using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Domain.News.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    public class OldRowClassification
    {
        public Guid Id { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string? CountryCode { get; set; }
        public NewsScope Scope { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TitleEN { get; set; } = string.Empty;
        public string TitleAR { get; set; } = string.Empty;
        public bool WouldKeep { get; set; }
        public string Rule { get; set; } = string.Empty;
        public string? MatchedTerm { get; set; }
        public bool LooksBankingLoose { get; set; }
        public bool IsDeleted { get; set; }
    }

    // Step 9a — read-only. Applies the CURRENT BankingKeywordFilter
    // retroactively to rows already in the database, so a human can decide
    // which old non-banking rows to later soft-delete. Never calls
    // SaveChanges / never touches IsDeleted — pure read + in-memory compute.
    public class OldRowsClassificationService
    {
        // "Looks banking-related" by a looser, naive substring check — used
        // only to measure the strict filter's recall (how many of the rows
        // it rejects still mention an obvious banking term). Independent of
        // BankingKeywordFilter's own (much stricter) keyword lists.
        private static readonly string[] LooseEnTerms = ["bank", "loan", "interest", "central bank"];
        private static readonly string[] LooseArTermsRaw = ["بنك", "مصرف", "قرض", "فائدة"];
        private static readonly string[] LooseArTerms =
            LooseArTermsRaw.Select(BankingKeywordFilter.NormalizeArabic).ToArray();

        private readonly INewsRepository _newsRepo;
        private readonly IMarketDataRepository _marketRepo;
        private readonly BankingKeywordFilter _filter;

        public OldRowsClassificationService(
            INewsRepository newsRepo,
            IMarketDataRepository marketRepo,
            BankingKeywordFilter filter)
        {
            _newsRepo = newsRepo;
            _marketRepo = marketRepo;
            _filter = filter;
        }

        public async Task<List<OldRowClassification>> ClassifyAsync(DateTime beforeUtc, bool includeDeleted = false)
        {
            var rows = includeDeleted
                ? await _newsRepo.GetAllBeforeIncludingDeletedAsync(beforeUtc)
                : await _newsRepo.GetAllActiveBeforeAsync(beforeUtc);

            // Cache local bank-name lookups per country — many rows share
            // the same CountryCode and the lookup hits the database.
            var bankNamesByCountry = new Dictionary<string, List<(string NameEN, string NameAR)>>(
                StringComparer.OrdinalIgnoreCase);

            var results = new List<OldRowClassification>(rows.Count);

            foreach (var row in rows)
            {
                var isInternational = row.Scope == NewsScope.International;

                IEnumerable<(string NameEN, string NameAR)> localBanks = [];
                if (!isInternational && !string.IsNullOrWhiteSpace(row.CountryCode))
                {
                    if (!bankNamesByCountry.TryGetValue(row.CountryCode, out var cached))
                    {
                        cached = (await _marketRepo.GetBankNamesByCountryAsync(row.CountryCode)).ToList();
                        bankNamesByCountry[row.CountryCode] = cached;
                    }
                    localBanks = cached;
                }

                // title := TitleEN + TitleAR ; summary := Summary (+ SummaryEN/SummaryAR if present)
                var title = string.Join(" ", new[] { row.TitleEN, row.TitleAR }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var summary = string.Join(" ", new[] { row.Summary, row.SummaryEN, row.SummaryAR }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                var check = _filter.Check(
                    title, summary,
                    isOfficialFeed: false,
                    localBankNames: localBanks,
                    isInternational: isInternational,
                    countryCode: row.CountryCode,
                    ownNameExclusions: null);

                var fullText = $"{title} {summary}";
                var lowerEn = fullText.ToLowerInvariant();
                var normalizedAr = BankingKeywordFilter.NormalizeArabic(fullText);
                var looksLoose =
                    LooseEnTerms.Any(t => lowerEn.Contains(t, StringComparison.Ordinal)) ||
                    LooseArTerms.Any(t => normalizedAr.Contains(t, StringComparison.Ordinal));

                results.Add(new OldRowClassification
                {
                    Id = row.Id,
                    SourceName = row.SourceName,
                    CountryCode = row.CountryCode,
                    Scope = row.Scope,
                    PublishedAt = row.PublishedAt,
                    CreatedAt = row.CreatedAt,
                    TitleEN = row.TitleEN,
                    TitleAR = row.TitleAR,
                    WouldKeep = check.Verdict != KeywordVerdict.Rejected,
                    Rule = check.RuleLabel,
                    MatchedTerm = check.MatchedOn,
                    LooksBankingLoose = looksLoose,
                    IsDeleted = row.IsDeleted
                });
            }

            return results;
        }

        // Deterministic sample: fix the candidate order first (by Id), then
        // assign each item a seeded-random weight and take the lowest
        // `count` weights. Reproducible regardless of DB fetch order.
        public static List<OldRowClassification> SeededSample(
            IEnumerable<OldRowClassification> pool, int count, int seed)
        {
            var ordered = pool.OrderBy(x => x.Id).ToList();
            var rng = new Random(seed);
            return ordered
                .Select(x => (Item: x, Weight: rng.Next()))
                .OrderBy(x => x.Weight)
                .Take(count)
                .Select(x => x.Item)
                .ToList();
        }
    }
}
