using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Domain.News.Interfaces;
using BeyeCEO.Infrastructure.ExternalServices;
using BeyeCEO.Infrastructure.NewsAggregation.ImageFallback;
using BeyeCEO.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    public class FeedRunResult
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Fetched { get; set; }
        public int TooOld { get; set; }
        public int Rejected { get; set; }
        public int Passed { get; set; }
        public int NearDuplicate { get; set; }
        public int AlreadyInDb { get; set; }
        public int SkippedLongUrl { get; set; }
        public int WouldStoreOrStored { get; set; }
        public DateTime? NewestItemDate { get; set; }
        public bool Stale { get; set; }

        // Step 11b — og:image fallback stats for this feed's run.
        public int ImageFromFeed { get; set; }
        public int ImageFromFallback { get; set; }
        public int ImageFallbackFetchesMade { get; set; }
        public int ImageFallbackFetchesSkipped { get; set; }

        // Breakdown of ImageFallbackFetchesSkipped by PageImageFetchStatus
        // name (RobotsDisallowed, MaxFetchesPerDomainExceeded, etc.) — lets
        // the dry-run report distinguish "robots said no" from "hit the
        // per-domain cap" from "page had nothing usable."
        public Dictionary<string, int> ImageFallbackSkipReasons { get; set; } = new();
    }

    public class GroupRunResult
    {
        public string Group { get; set; } = string.Empty;
        public List<FeedRunResult> Feeds { get; set; } = new();
        public List<string> SampleWouldStoreTitles { get; set; } = new();

        public int TotalFetched => Feeds.Sum(f => f.Fetched);
        public int TotalTooOld => Feeds.Sum(f => f.TooOld);
        public int TotalRejected => Feeds.Sum(f => f.Rejected);
        public int TotalPassed => Feeds.Sum(f => f.Passed);
        public int TotalNearDuplicate => Feeds.Sum(f => f.NearDuplicate);
        public int TotalAlreadyInDb => Feeds.Sum(f => f.AlreadyInDb);
        public int TotalSkippedLongUrl => Feeds.Sum(f => f.SkippedLongUrl);
        public int TotalWouldStoreOrStored => Feeds.Sum(f => f.WouldStoreOrStored);
    }

    // Shared by the Hangfire recurring jobs, the admin on-demand run
    // endpoint, and the dry-run endpoint, so all three exercise exactly the
    // same fetch -> age-skip -> filter -> near-dup -> dedupe -> (maybe)
    // store logic.
    public class NewsPipelineRunner
    {
        private readonly FeedFetcherService _fetcher;
        private readonly GuardianClient _guardian;
        private readonly BankingKeywordFilter _filter;
        private readonly INewsRepository _newsRepo;
        private readonly IConfiguration _config;
        private readonly SourcesHealthTracker _health;
        private readonly IPageMetadataFetcher _pageFetcher;
        private readonly ILogger<NewsPipelineRunner> _logger;

        public NewsPipelineRunner(
            FeedFetcherService fetcher,
            GuardianClient guardian,
            BankingKeywordFilter filter,
            INewsRepository newsRepo,
            IConfiguration config,
            SourcesHealthTracker health,
            IPageMetadataFetcher pageFetcher,
            ILogger<NewsPipelineRunner> logger)
        {
            _fetcher = fetcher;
            _guardian = guardian;
            _filter = filter;
            _newsRepo = newsRepo;
            _config = config;
            _health = health;
            _pageFetcher = pageFetcher;
            _logger = logger;
        }

        private bool StorageEnabled => _config.GetValue("News:Storage:Enabled", false);

        private static NewsScope ScopeOf(FeedOptions feed) =>
            string.Equals(feed.Scope, "International", StringComparison.OrdinalIgnoreCase)
                ? NewsScope.International
                : NewsScope.Local;

        private async Task<List<string>> GetRecentTitlesAsync(NewsScope scope, string? countryCode)
        {
            var recent = await _newsRepo.GetRecentArticlesAsync(scope, countryCode, DateTime.UtcNow.AddHours(-72));
            return recent
                .Select(a => string.IsNullOrEmpty(a.TitleEN) ? a.TitleAR : a.TitleEN)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }

        public async Task<List<GroupRunResult>> RunAsync(string group, bool store)
        {
            var results = new List<GroupRunResult>();

            if (string.Equals(group, "all", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(group, "local-rss", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(await RunLocalRssAsync(store));
            }

            if (string.Equals(group, "all", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(group, "international", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(await RunInternationalAsync(store));
            }

            return results;
        }

        public Task<GroupRunResult> RunLocalRssAsync(bool store) =>
            RunConfigFeedsAsync("local-rss", store);

        public async Task<GroupRunResult> RunInternationalAsync(bool store)
        {
            var result = await RunConfigFeedsAsync("international", store);

            var (guardianResult, guardianTitles) = await RunGuardianAsync(store);
            result.Feeds.Add(guardianResult);
            result.SampleWouldStoreTitles = result.SampleWouldStoreTitles
                .Concat(guardianTitles)
                .Take(10)
                .ToList();

            return result;
        }

        // Fetches + filters + applies near-duplicate detection across every
        // enabled feed, grouped by Scope/CountryCode, without mapping to
        // NewsArticle or touching the save path — used by the admin
        // dry-run endpoint. Guardian is intentionally excluded (it isn't a
        // News:Feeds entry and the dry-run endpoint never included it).
        public async Task<List<FeedFetchResult>> DryRunAllFeedsAsync()
        {
            var feeds = (_config.GetSection("News:Feeds").Get<List<FeedOptions>>() ?? [])
                .Where(f => f.Enabled)
                .ToList();

            // Sequential, not Task.WhenAll: FeedFetcherService.FetchAsync
            // uses the scoped IMarketDataRepository (one shared DbContext
            // per request) for the local-bank-name lookup, and EF Core's
            // DbContext is not safe for concurrent use from multiple
            // in-flight calls.
            var results = new List<FeedFetchResult>();
            foreach (var feed in feeds)
                results.Add(await _fetcher.FetchAsync(feed));

            var pairs = feeds.Zip(results, (f, r) => (Feed: f, Result: r)).ToList();

            foreach (var group in pairs.GroupBy(p => (Scope: ScopeOf(p.Feed), p.Feed.CountryCode)))
            {
                var recentTitles = await GetRecentTitlesAsync(group.Key.Scope, group.Key.CountryCode);
                NearDuplicateResolver.ApplyToFeedResults(group.Select(p => p.Result).ToList(), recentTitles);
            }

            return results.ToList();
        }

        private async Task<GroupRunResult> RunConfigFeedsAsync(string pollGroup, bool store)
        {
            var feeds = _config.GetSection("News:Feeds").Get<List<FeedOptions>>() ?? [];
            var groupFeeds = feeds
                .Where(f => f.Enabled && string.Equals(f.PollGroup, pollGroup, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var groupResult = new GroupRunResult { Group = pollGroup };
            var storageEnabled = StorageEnabled;

            // Fetch every feed first (each isolated by try/catch) so
            // near-duplicate detection can run once across the whole group
            // before any feed's items are mapped/stored.
            var fetchPairs = new List<(FeedOptions Feed, FeedFetchResult? Result, Exception? Error)>();
            foreach (var feed in groupFeeds)
            {
                try
                {
                    fetchPairs.Add((feed, await _fetcher.FetchAsync(feed), null));
                }
                catch (Exception ex)
                {
                    fetchPairs.Add((feed, null, ex));
                    _health.RecordError(feed.Name, ex.Message);
                    _logger.LogError(ex, "Feed {Name}: fetch failed", feed.Name);
                }
            }

            var successfulResults = fetchPairs.Where(p => p.Result != null).Select(p => p.Result!).ToList();
            if (successfulResults.Count > 0 && groupFeeds.Count > 0)
            {
                var scope = ScopeOf(groupFeeds[0]);
                var countryCode = scope == NewsScope.Local ? groupFeeds[0].CountryCode : null;
                var recentTitles = await GetRecentTitlesAsync(scope, countryCode);
                NearDuplicateResolver.ApplyToFeedResults(successfulResults, recentTitles);
            }

            foreach (var (feed, fetchResult, error) in fetchPairs)
            {
                FeedRunResult feedResult;
                List<string> titles;

                if (error != null)
                {
                    feedResult = new FeedRunResult { Name = feed.Name, Status = $"error: {error.Message}" };
                    titles = [];
                }
                else
                {
                    (feedResult, titles) = await ProcessFeedResultAsync(feed, fetchResult!, store, storageEnabled);
                    _health.RecordSuccess(
                        feed.Name, feedResult.Fetched, feedResult.WouldStoreOrStored,
                        feedResult.NewestItemDate, feedResult.Stale);
                }

                groupResult.Feeds.Add(feedResult);
                groupResult.SampleWouldStoreTitles.AddRange(titles);
            }

            groupResult.SampleWouldStoreTitles = groupResult.SampleWouldStoreTitles.Take(10).ToList();
            return groupResult;
        }

        // Pure data-flow, no HTTP: maps a feed's already-fetched/filtered/
        // near-dup-pruned items, dedupes, checks the DB, and (maybe) stores.
        // Separated out from RunConfigFeedsAsync so it can be unit tested
        // with a hand-built FeedFetchResult and a fake INewsRepository — no
        // network or real database required.
        public async Task<(FeedRunResult Result, List<string> Titles)> ProcessFeedResultAsync(
            FeedOptions feed, FeedFetchResult fetchResult, bool store, bool storageEnabled)
        {
            var feedResult = new FeedRunResult
            {
                Name = feed.Name,
                Status = fetchResult.Status,
                Fetched = fetchResult.ItemsFetched,
                TooOld = fetchResult.TooOld,
                Rejected = fetchResult.Rejected,
                // PassedRaw may already have near-duplicates removed by the
                // caller — add NearDuplicate back to report the original
                // (pre-dedup) keyword-filter-passed count.
                Passed = fetchResult.PassedKeywordFilter + fetchResult.NearDuplicate,
                NearDuplicate = fetchResult.NearDuplicate,
                NewestItemDate = fetchResult.NewestItemDate,
                Stale = fetchResult.SourceStale
            };

            var mapped = new List<(NewsArticle Article, string RawLink)>();
            foreach (var (item, check) in fetchResult.PassedRaw)
            {
                var article = NewsArticleMapper.MapToArticle(item, check, feed, _logger);
                if (article == null) { feedResult.SkippedLongUrl++; continue; }
                mapped.Add((article, item.RawLink));
            }

            var deduped = NewsRepository.DedupeIncomingBatch(mapped.Select(m => m.Article))
                .Select(a => (Article: a, RawLink: mapped.First(m => m.Article == a).RawLink))
                .ToList();

            var checkUrls = deduped
                .SelectMany(m => new[] { m.Article.SourceUrl, m.RawLink })
                .Where(u => !string.IsNullOrWhiteSpace(u));

            var existing = await _newsRepo.FindExistingSourceUrlsAsync(checkUrls);

            var toStore = new List<NewsArticle>();
            foreach (var (article, rawLink) in deduped)
            {
                if (NewsArticleMapper.MatchesExisting(article.SourceUrl, rawLink, existing))
                {
                    feedResult.AlreadyInDb++;
                    continue;
                }
                toStore.Add(article);
            }

            feedResult.ImageFromFeed = toStore.Count(a => !string.IsNullOrWhiteSpace(a.ImageUrl));
            await ApplyImageFallbackAsync(feed, toStore, feedResult);

            feedResult.WouldStoreOrStored = toStore.Count;

            if (store && storageEnabled && toStore.Count > 0)
                feedResult.WouldStoreOrStored = await _newsRepo.SaveRangeAsync(toStore);

            var titles = toStore
                .Select(a => string.IsNullOrEmpty(a.TitleEN) ? a.TitleAR : a.TitleEN)
                .ToList();

            return (feedResult, titles);
        }

        // Step 11b — og:image fallback. Only for feeds explicitly
        // allow-listed via ImageFallback.Enabled, only for items that will
        // be stored with no image from the feed itself. Never blocks the
        // run: any fetch exception is swallowed and counted as skipped.
        private async Task ApplyImageFallbackAsync(FeedOptions feed, List<NewsArticle> toStore, FeedRunResult feedResult)
        {
            if (feed.ImageFallback is not { Enabled: true }) return;

            var candidates = toStore.Where(a => string.IsNullOrWhiteSpace(a.ImageUrl)).ToList();
            if (candidates.Count == 0) return;

            var existingCounts = await _newsRepo.GetImageUrlCountsAsync(feed.Name);
            var runCounts = new Dictionary<string, int>();

            foreach (var article in candidates)
            {
                if (string.IsNullOrWhiteSpace(article.SourceUrl)) continue;

                try
                {
                    var result = await _pageFetcher.TryGetImageUrlAsync(article.SourceUrl, feed.ImageFallback.MaxBytes);

                    if (result.Status != PageImageFetchStatus.Found || result.ImageUrl == null)
                    {
                        feedResult.ImageFallbackFetchesSkipped++;
                        IncrementSkipReason(feedResult, result.Status.ToString());
                        continue;
                    }

                    feedResult.ImageFallbackFetchesMade++;

                    existingCounts.TryGetValue(result.ImageUrl, out var existingCount);
                    runCounts.TryGetValue(result.ImageUrl, out var runCount);

                    if (SiteDefaultImageGuard.IsSiteDefault(existingCount, runCount))
                    {
                        _logger.LogInformation(
                            "ApplyImageFallbackAsync: {Feed} — rejecting {Url} as a site-default image (seen {Count} times)",
                            feed.Name, result.ImageUrl, existingCount + runCount);
                        IncrementSkipReason(feedResult, "RejectedAsSiteDefault");
                        continue;
                    }

                    article.SetImageUrl(result.ImageUrl);
                    feedResult.ImageFromFallback++;
                    runCounts[result.ImageUrl] = runCount + 1;
                }
                catch (Exception ex)
                {
                    feedResult.ImageFallbackFetchesSkipped++;
                    IncrementSkipReason(feedResult, "Exception");
                    _logger.LogInformation(ex, "ApplyImageFallbackAsync: {Feed} — fallback failed for {Url}", feed.Name, article.SourceUrl);
                }
            }
        }

        private static void IncrementSkipReason(FeedRunResult feedResult, string reason)
        {
            feedResult.ImageFallbackSkipReasons.TryGetValue(reason, out var count);
            feedResult.ImageFallbackSkipReasons[reason] = count + 1;
        }

        private async Task<(FeedRunResult Result, List<string> Titles)> RunGuardianAsync(bool store)
        {
            var feedResult = new FeedRunResult { Name = "The Guardian" };
            var titles = new List<string>();
            var storageEnabled = StorageEnabled;
            var maxAgeDays = _config.GetValue("News:MaxItemAgeDays", 30);

            try
            {
                var articles = (await _guardian.FetchInternationalNewsAsync(pageSize: 5)).ToList();
                feedResult.Fetched = articles.Count;
                feedResult.NewestItemDate = articles.Count > 0 ? articles.Max(a => a.PublishedAt) : null;
                feedResult.Stale = feedResult.NewestItemDate.HasValue &&
                    (DateTime.UtcNow - feedResult.NewestItemDate.Value).TotalDays > maxAgeDays;

                var now = DateTime.UtcNow;
                var mapped = new List<(NewsArticle Article, string RawLink)>();

                foreach (var article in articles)
                {
                    if (FeedFetcherService.IsTooOld(article.PublishedAt, now, maxAgeDays))
                    {
                        feedResult.TooOld++;
                        continue;
                    }

                    var check = _filter.Check(article.TitleEN, article.Summary, isOfficialFeed: false);
                    if (check.Verdict == KeywordVerdict.Rejected)
                    {
                        feedResult.Rejected++;
                        continue;
                    }

                    feedResult.Passed++;

                    var remapped = NewsArticleMapper.RemapGuardianArticle(article, _logger);
                    if (remapped == null) { feedResult.SkippedLongUrl++; continue; }

                    mapped.Add((remapped, article.SourceUrl));
                }

                // Near-duplicate detection within Guardian's own batch +
                // against recent (72h) existing International rows.
                var recentTitles = await GetRecentTitlesAsync(NewsScope.International, null);
                var survivors = NearDuplicateResolver.ApplyToArticles(
                    mapped.Select(m => m.Article).ToList(), recentTitles, out var droppedCount);
                feedResult.NearDuplicate = droppedCount;
                mapped = mapped.Where(m => survivors.Contains(m.Article)).ToList();

                var deduped = NewsRepository.DedupeIncomingBatch(mapped.Select(m => m.Article))
                    .Select(a => (Article: a, RawLink: mapped.First(m => m.Article == a).RawLink))
                    .ToList();

                var checkUrls = deduped
                    .SelectMany(m => new[] { m.Article.SourceUrl, m.RawLink })
                    .Where(u => !string.IsNullOrWhiteSpace(u));

                var existing = await _newsRepo.FindExistingSourceUrlsAsync(checkUrls);

                var toStore = new List<NewsArticle>();
                foreach (var (article, rawLink) in deduped)
                {
                    if (NewsArticleMapper.MatchesExisting(article.SourceUrl, rawLink, existing))
                    {
                        feedResult.AlreadyInDb++;
                        continue;
                    }
                    toStore.Add(article);
                }

                feedResult.WouldStoreOrStored = toStore.Count;

                if (store && storageEnabled && toStore.Count > 0)
                    feedResult.WouldStoreOrStored = await _newsRepo.SaveRangeAsync(toStore);

                titles.AddRange(toStore.Select(a => a.TitleEN).Take(10));

                feedResult.Status = "ok";
                _health.RecordSuccess(
                    "The Guardian", feedResult.Fetched, feedResult.WouldStoreOrStored,
                    feedResult.NewestItemDate, feedResult.Stale);
            }
            catch (Exception ex)
            {
                feedResult.Status = $"error: {ex.Message}";
                _health.RecordError("The Guardian", ex.Message);
                _logger.LogError(ex, "Guardian: pipeline run failed");
            }

            return (feedResult, titles);
        }
    }
}
