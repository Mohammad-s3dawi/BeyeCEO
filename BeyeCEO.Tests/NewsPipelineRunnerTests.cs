using BeyeCEO.Infrastructure.NewsAggregation;
using BeyeCEO.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;

namespace BeyeCEO.Tests
{
    public class NewsPipelineRunnerTests
    {
        private static NewsPipelineRunner MakeRunner(FakeNewsRepository repo)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            // FeedFetcherService/GuardianClient aren't touched by
            // ProcessFeedResultAsync (it takes an already-built
            // FeedFetchResult) — null-forgiving is safe here, this is the
            // one method under test that never dereferences them.
            return new NewsPipelineRunner(
                fetcher: null!,
                guardian: null!,
                filter: new BankingKeywordFilter(),
                newsRepo: repo,
                config: config,
                health: new SourcesHealthTracker(),
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
    }
}
