using BeyeCEO.Domain.MarketData.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    // Orchestrates one fetch+parse+filter pass over a single configured feed.
    // Read-only: never writes to the database. Used today only by the
    // admin dry-run endpoint.
    public class FeedFetcherService
    {
        private const int MaxReportedItems = 25;

        // Feeds judged "official" (central banks / regulators) get the
        // relaxed official-topic path in BankingKeywordFilter instead of the
        // unconditional auto-pass this used to be. Trade press like
        // Finextra / Banking Dive is NOT in this set and goes through the
        // normal keyword filter only.
        private static readonly HashSet<string> OfficialFeedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Bank of England",
            "BIS",
            "Federal Reserve Banking Regulation",
            "Federal Reserve Monetary Policy",
            "ECB"
        };

        // An official feed's own institution name must not count as a bank
        // signal — otherwise every item trivially passes just by mentioning
        // itself. "Federal Open Market Committee"/"FOMC" are deliberately
        // NOT excluded for the Fed feeds — those still need to pass FOMC
        // minutes/statements/projections.
        private static readonly Dictionary<string, string[]> OfficialOwnNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Bank of England"] = ["Bank of England"],
            ["Federal Reserve Banking Regulation"] = ["Federal Reserve Board", "Federal Reserve", "the Board"],
            ["Federal Reserve Monetary Policy"] = ["Federal Reserve Board", "Federal Reserve", "the Board"],
            ["ECB"] = ["European Central Bank", "ECB"],
            ["BIS"] = ["Bank for International Settlements", "BIS"]
        };

        private readonly HttpClient _http;
        private readonly IMarketDataRepository _marketRepo;
        private readonly BankingKeywordFilter _filter;
        private readonly IConfiguration _config;
        private readonly ILogger<FeedFetcherService> _logger;

        public FeedFetcherService(
            HttpClient http,
            IMarketDataRepository marketRepo,
            BankingKeywordFilter filter,
            IConfiguration config,
            ILogger<FeedFetcherService> logger)
        {
            _http = http;
            _marketRepo = marketRepo;
            _filter = filter;
            _config = config;
            _logger = logger;
        }

        // Pure, unit-testable: is this item older than the feed's (or the
        // global default's) max-age cutoff?
        public static bool IsTooOld(DateTime publishedAtUtc, DateTime nowUtc, int maxAgeDays) =>
            (nowUtc - publishedAtUtc).TotalDays > maxAgeDays;

        public async Task<FeedFetchResult> FetchAsync(FeedOptions feed)
        {
            var result = new FeedFetchResult { Name = feed.Name };

            string body;
            try
            {
                using var response = await _http.GetAsync(feed.Url);
                if (!response.IsSuccessStatusCode)
                {
                    result.Status = $"http {(int)response.StatusCode}";
                    _logger.LogWarning("Feed {Name}: HTTP {Status}", feed.Name, (int)response.StatusCode);
                    return result;
                }

                body = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                result.Status = $"error: {ex.Message}";
                _logger.LogWarning(ex, "Feed {Name}: fetch failed", feed.Name);
                return result;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                result.Status = "empty body (soft-404?)";
                _logger.LogWarning("Feed {Name}: empty body, treating as soft-404", feed.Name);
                return result;
            }

            var items = FeedParser.Parse(body, feed.Format);
            if (items.Count == 0)
            {
                result.Status = "parsed but 0 items";
                _logger.LogWarning("Feed {Name}: parser found 0 items", feed.Name);
                return result;
            }

            items = items.Take(feed.MaxItemsPerRun).ToList();

            // In-batch dedupe by normalized link (links are already normalized by the parser).
            items = items
                .GroupBy(i => i.Link)
                .Select(g => g.First())
                .ToList();

            result.Status = "ok";
            result.ItemsFetched = items.Count;
            result.ItemsWithSummary = items.Count(i => !string.IsNullOrWhiteSpace(i.Summary));
            result.ItemsWithImage = items.Count(i => !string.IsNullOrWhiteSpace(i.ImageUrl));
            result.NewestItemDate = items.Max(i => i.PublishedAtUtc);

            result.SourceStale = (DateTime.UtcNow - result.NewestItemDate.Value).TotalDays > feed.ExpectedMaxAgeDays;
            if (result.SourceStale)
            {
                _logger.LogWarning(
                    "SOURCE_STALE: {Name} newest item is {Newest:u}, older than {Days}d threshold",
                    feed.Name, result.NewestItemDate, feed.ExpectedMaxAgeDays);
            }

            var isOfficial = OfficialFeedNames.Contains(feed.Name);
            var ownNameExclusions = isOfficial && OfficialOwnNames.TryGetValue(feed.Name, out var names) ? names : null;
            var isInternational = string.Equals(feed.Scope, "International", StringComparison.OrdinalIgnoreCase);
            var maxItemAgeDays = feed.MaxItemAgeDays ?? _config.GetValue("News:MaxItemAgeDays", 30);
            var now = DateTime.UtcNow;

            IEnumerable<(string NameEN, string NameAR)> localBanks = [];
            if (!string.IsNullOrWhiteSpace(feed.CountryCode))
                localBanks = await _marketRepo.GetBankNamesByCountryAsync(feed.CountryCode);

            foreach (var item in items)
            {
                if (IsTooOld(item.PublishedAtUtc, now, maxItemAgeDays))
                {
                    result.TooOld++;
                    continue;
                }

                var check = _filter.Check(
                    item.Title, item.Summary, isOfficial, localBanks, isInternational, feed.CountryCode,
                    ownNameExclusions);

                switch (check.Verdict)
                {
                    case KeywordVerdict.PassedStrong: result.PassedStrong++; break;
                    case KeywordVerdict.PassedWeak: result.PassedWeak2++; break;
                    case KeywordVerdict.PassedBankName: result.PassedBankName++; break;
                    case KeywordVerdict.PassedOfficialTopic: result.PassedOfficialTopic++; break;
                    default: result.Rejected++; break;
                }

                var entry = new DryRunItemResult
                {
                    Title = item.Title,
                    PublishedAtUtc = item.PublishedAtUtc,
                    Rule = check.RuleLabel,
                    MatchedTerm = check.MatchedOn
                };

                var target = check.Verdict == KeywordVerdict.Rejected ? result.RejectedItems : result.PassedItems;
                if (target.Count < MaxReportedItems)
                    target.Add(entry);

                if (check.Verdict != KeywordVerdict.Rejected)
                    result.PassedRaw.Add((item, check));
            }

            return result;
        }
    }
}
