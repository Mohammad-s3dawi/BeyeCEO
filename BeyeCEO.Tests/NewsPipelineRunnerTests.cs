using BeyeCEO.Infrastructure.NewsAggregation;
using BeyeCEO.Infrastructure.NewsAggregation.ImageFallback;
using BeyeCEO.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;

namespace BeyeCEO.Tests
{
    public class NewsPipelineRunnerTests
    {
        private static NewsPipelineRunner MakeRunner(FakeNewsRepository repo, IPageMetadataFetcher? pageFetcher = null)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            // FeedFetcherService/GuardianClient aren't touched by
            // ProcessFeedResultAsync (it takes an already-built
            // FeedFetchResult) — null-forgiving is safe here, this is the
            // one method under test that never dereferences them. pageFetcher
            // is only dereferenced when a feed's ImageFallback.Enabled=true,
            // which existing tests' `Feed` fixture leaves unset.
            return new NewsPipelineRunner(
                fetcher: null!,
                guardian: null!,
                filter: new BankingKeywordFilter(),
                newsRepo: repo,
                config: config,
                health: new SourcesHealthTracker(),
                pageFetcher: pageFetcher!,
                logger: NullLogger<NewsPipelineRunner>.Instance);
        }

        private static FeedOptions Feed => new()
        {
            Name = "Test Feed",
            Url = "https://example.com/rss",
            Format = "rss",
            Language = "en",
            Scope = "Local",
            CountryCode = "JO",
            PollGroup = "local-rss"
        };

        private static FeedFetchResult MakeFetchResultWithOnePassedItem(string link = "https://example.com/a")
        {
            var item = new ParsedFeedItem
            {
                Title = "Central bank raises rates",
                Link = link,
                RawLink = link,
                Summary = "summary",
                PublishedAtUtc = DateTime.UtcNow,
                DateGuessed = false
            };
            var check = new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong, MatchedOn = "bank" };

            var result = new FeedFetchResult { Name = "Test Feed", Status = "ok", ItemsFetched = 1 };
            result.PassedRaw.Add((item, check));
            return result;
        }

        [Fact]
        public async Task ProcessFeedResultAsync_StoreFalse_DoesNotCallSaveRangeAsync()
        {
            var repo = new FakeNewsRepository();
            var runner = MakeRunner(repo);
            var fetchResult = MakeFetchResultWithOnePassedItem();

            var (result, titles) = await runner.ProcessFeedResultAsync(Feed, fetchResult, store: false, storageEnabled: true);

            Assert.Equal(0, repo.SaveRangeAsyncCallCount);
            Assert.Equal(1, result.WouldStoreOrStored);
            Assert.Single(titles);
        }

        [Fact]
        public async Task ProcessFeedResultAsync_StoreTrueButStorageDisabled_DoesNotCallSaveRangeAsync()
        {
            var repo = new FakeNewsRepository();
            var runner = MakeRunner(repo);
            var fetchResult = MakeFetchResultWithOnePassedItem();

            var (result, _) = await runner.ProcessFeedResultAsync(Feed, fetchResult, store: true, storageEnabled: false);

            Assert.Equal(0, repo.SaveRangeAsyncCallCount);
            Assert.Equal(1, result.WouldStoreOrStored); // still reports what WOULD have been stored
        }

        [Fact]
        public async Task ProcessFeedResultAsync_StoreTrueAndStorageEnabled_CallsSaveRangeAsync()
        {
            var repo = new FakeNewsRepository();
            var runner = MakeRunner(repo);
            var fetchResult = MakeFetchResultWithOnePassedItem();

            var (result, _) = await runner.ProcessFeedResultAsync(Feed, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(1, repo.SaveRangeAsyncCallCount);
            Assert.Single(repo.SavedArticles);
            Assert.Equal(1, result.WouldStoreOrStored);
        }

        [Fact]
        public async Task ProcessFeedResultAsync_ItemAlreadyInDb_IsSkippedAndCounted()
        {
            var repo = new FakeNewsRepository
            {
                ExistingUrls = new HashSet<string> { "https://example.com/a" }
            };
            var runner = MakeRunner(repo);
            var fetchResult = MakeFetchResultWithOnePassedItem("https://example.com/a");

            var (result, titles) = await runner.ProcessFeedResultAsync(Feed, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(1, result.AlreadyInDb);
            Assert.Equal(0, result.WouldStoreOrStored);
            Assert.Empty(titles);
            Assert.Equal(0, repo.SaveRangeAsyncCallCount); // nothing left to save
        }

        [Fact]
        public async Task ProcessFeedResultAsync_UrlExistsOnlyAsSoftDeleted_StillSkippedAndCounted()
        {
            // Step 9d hardening: FindExistingSourceUrlsAsync must report a
            // URL as existing even when the only matching row is soft-
            // deleted (e.g. removed by the Step 9c/9e cleanup), so it is
            // never re-ingested as if it were new. FakeNewsRepository
            // models this with a separate SoftDeletedUrls set — the URL
            // here is NOT in ExistingUrls at all, only in SoftDeletedUrls.
            var repo = new FakeNewsRepository
            {
                SoftDeletedUrls = new HashSet<string> { "https://example.com/a" }
            };
            var runner = MakeRunner(repo);
            var fetchResult = MakeFetchResultWithOnePassedItem("https://example.com/a");

            var (result, titles) = await runner.ProcessFeedResultAsync(Feed, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(1, result.AlreadyInDb);
            Assert.Equal(0, result.WouldStoreOrStored);
            Assert.Empty(titles);
            Assert.Equal(0, repo.SaveRangeAsyncCallCount);
        }

        [Fact]
        public async Task ProcessFeedResultAsync_LongUrl_IsSkippedAndCounted()
        {
            var repo = new FakeNewsRepository();
            var runner = MakeRunner(repo);
            var longUrl = "https://example.com/" + new string('a', 800);
            var fetchResult = MakeFetchResultWithOnePassedItem(longUrl);

            var (result, _) = await runner.ProcessFeedResultAsync(Feed, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(1, result.SkippedLongUrl);
            Assert.Equal(0, result.WouldStoreOrStored);
        }

        // ── Step 11b: og:image fallback integration ──────────────────────

        private static FeedOptions FeedWithImageFallback => new()
        {
            Name = "Test Feed",
            Url = "https://example.com/rss",
            Format = "rss",
            Language = "en",
            Scope = "Local",
            CountryCode = "JO",
            PollGroup = "local-rss",
            ImageFallback = new ImageFallbackOptions { Enabled = true, MaxBytes = 131072 }
        };

        [Fact]
        public async Task ProcessFeedResultAsync_ImageFallbackDisabled_NeverCallsFetcher()
        {
            var repo = new FakeNewsRepository();
            var pageFetcher = new FakePageMetadataFetcher { DefaultImageUrl = "https://example.com/found.jpg" };
            var runner = MakeRunner(repo, pageFetcher);
            var fetchResult = MakeFetchResultWithOnePassedItem();

            // Feed (the default fixture) has no ImageFallback configured.
            var (result, _) = await runner.ProcessFeedResultAsync(Feed, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(0, pageFetcher.CallCount);
            Assert.Equal(0, result.ImageFromFallback);
            Assert.Empty(repo.SavedArticles[0].ImageUrl);
        }

        [Fact]
        public async Task ProcessFeedResultAsync_ImageFallbackEnabled_SetsImageUrl_WhenFound()
        {
            var repo = new FakeNewsRepository();
            var pageFetcher = new FakePageMetadataFetcher { DefaultImageUrl = "https://example.com/found.jpg" };
            var runner = MakeRunner(repo, pageFetcher);
            var fetchResult = MakeFetchResultWithOnePassedItem();

            var (result, _) = await runner.ProcessFeedResultAsync(FeedWithImageFallback, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(1, pageFetcher.CallCount);
            Assert.Equal(1, result.ImageFromFallback);
            Assert.Equal(0, result.ImageFromFeed);
            Assert.Equal("https://example.com/found.jpg", repo.SavedArticles[0].ImageUrl);
        }

        [Fact]
        public async Task ProcessFeedResultAsync_ImageFallbackEnabled_NeverCalledWhenFeedAlreadyHasImage()
        {
            var repo = new FakeNewsRepository();
            var pageFetcher = new FakePageMetadataFetcher { DefaultImageUrl = "https://example.com/found.jpg" };
            var runner = MakeRunner(repo, pageFetcher);

            var item = new ParsedFeedItem
            {
                Title = "Central bank raises rates",
                Link = "https://example.com/a",
                RawLink = "https://example.com/a",
                Summary = "summary",
                ImageUrl = "https://example.com/from-feed.jpg",
                PublishedAtUtc = DateTime.UtcNow,
                DateGuessed = false
            };
            var check = new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong, MatchedOn = "bank" };
            var fetchResult = new FeedFetchResult { Name = "Test Feed", Status = "ok", ItemsFetched = 1 };
            fetchResult.PassedRaw.Add((item, check));

            var (result, _) = await runner.ProcessFeedResultAsync(FeedWithImageFallback, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(0, pageFetcher.CallCount);
            Assert.Equal(1, result.ImageFromFeed);
            Assert.Equal(0, result.ImageFromFallback);
            Assert.Equal("https://example.com/from-feed.jpg", repo.SavedArticles[0].ImageUrl);
        }

        [Fact]
        public async Task ProcessFeedResultAsync_ImageFallback_RejectsSiteDefaultAlreadySeenInDb()
        {
            var repo = new FakeNewsRepository
            {
                ImageUrlCounts = new Dictionary<string, int> { ["https://example.com/default-logo.jpg"] = 3 }
            };
            var pageFetcher = new FakePageMetadataFetcher { DefaultImageUrl = "https://example.com/default-logo.jpg" };
            var runner = MakeRunner(repo, pageFetcher);
            var fetchResult = MakeFetchResultWithOnePassedItem();

            var (result, _) = await runner.ProcessFeedResultAsync(FeedWithImageFallback, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(1, pageFetcher.CallCount);
            Assert.Equal(0, result.ImageFromFallback);
            Assert.Equal(1, result.ImageFallbackFetchesMade); // a candidate WAS found...
            Assert.Empty(repo.SavedArticles[0].ImageUrl); // ...but rejected as a site default
        }

        [Fact]
        public async Task ProcessFeedResultAsync_ImageFallback_RejectsSiteDefaultAcrossThisRun()
        {
            // Four items in the same run all resolve to the same candidate
            // image, with zero prior DB occurrences. The guard is checked
            // BEFORE each acceptance increments the running count, so the
            // 1st-3rd occurrences see counts 0/1/2 (all < threshold 3) and
            // are accepted; the 4th sees count 3 (>= threshold) and is
            // rejected — proving the in-run tally alone (no DB history
            // needed) can trigger the site-default safeguard.
            var repo = new FakeNewsRepository();
            var pageFetcher = new FakePageMetadataFetcher { DefaultImageUrl = "https://example.com/repeated.jpg" };
            var runner = MakeRunner(repo, pageFetcher);

            var fetchResult = new FeedFetchResult { Name = "Test Feed", Status = "ok", ItemsFetched = 4 };
            for (var i = 0; i < 4; i++)
            {
                var item = new ParsedFeedItem
                {
                    Title = $"Item {i}",
                    Link = $"https://example.com/item-{i}",
                    RawLink = $"https://example.com/item-{i}",
                    Summary = "summary",
                    PublishedAtUtc = DateTime.UtcNow,
                    DateGuessed = false
                };
                fetchResult.PassedRaw.Add((item, new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong, MatchedOn = "bank" }));
            }

            var (result, _) = await runner.ProcessFeedResultAsync(FeedWithImageFallback, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(4, pageFetcher.CallCount);
            var withImage = repo.SavedArticles.Count(a => !string.IsNullOrEmpty(a.ImageUrl));
            Assert.Equal(3, withImage); // the 4th occurrence is rejected as a site default
        }

        [Fact]
        public async Task ProcessFeedResultAsync_ImageFallback_FetcherThrows_NeverBlocksTheRun()
        {
            var repo = new FakeNewsRepository();
            var pageFetcher = new ThrowingPageMetadataFetcher();
            var runner = MakeRunner(repo, pageFetcher);
            var fetchResult = MakeFetchResultWithOnePassedItem();

            var (result, titles) = await runner.ProcessFeedResultAsync(FeedWithImageFallback, fetchResult, store: true, storageEnabled: true);

            Assert.Equal(1, result.WouldStoreOrStored);
            Assert.Single(titles);
            Assert.Equal(1, result.ImageFallbackFetchesSkipped);
            Assert.Empty(repo.SavedArticles[0].ImageUrl);
        }

        private class ThrowingPageMetadataFetcher : BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.IPageMetadataFetcher
        {
            public Task<BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.PageImageResult> TryGetImageUrlAsync(
                string pageUrl, int maxBytes, CancellationToken ct = default) =>
                throw new InvalidOperationException("simulated network failure");
        }
    }
}
