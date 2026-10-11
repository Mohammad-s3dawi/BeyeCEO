using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Infrastructure.NewsAggregation;
using System;
using System.Collections.Generic;

namespace BeyeCEO.Tests
{
    public class NewsArticleMapperTests
    {
        private static FeedOptions MakeFeed(
            string language, string scope = "Local", string? countryCode = "JO") => new()
        {
            Name = "Test Feed",
            Url = "https://example.com/rss",
            Format = "rss",
            Language = language,
            Scope = scope,
            CountryCode = countryCode,
            PollGroup = scope == "International" ? "international" : "local-rss"
        };

        private static ParsedFeedItem MakeItem(
            string title = "Central bank raises rates",
            string link = "https://example.com/article",
            bool dateGuessed = false) => new()
        {
            Title = title,
            Link = link,
            RawLink = link,
            Summary = "A short summary of the article.",
            PublishedAtUtc = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc),
            DateGuessed = dateGuessed,
            ImageUrl = "https://example.com/image.jpg"
        };

        [Fact]
        public void MapToArticle_EnglishFeed_FillsOnlyEnglishColumns()
        {
            var feed = MakeFeed("en");
            var item = MakeItem();
            var check = new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong, IsOfficial = false };

            var article = NewsArticleMapper.MapToArticle(item, check, feed);

            Assert.NotNull(article);
            Assert.Equal(item.Title, article!.TitleEN);
            Assert.Equal(string.Empty, article.TitleAR);
            Assert.Equal(item.Summary, article.SummaryEN);
            Assert.Null(article.SummaryAR);
            Assert.Equal(item.Summary, article.Summary);
            Assert.Equal("en", article.OriginalLanguage);
        }

        [Fact]
        public void MapToArticle_ArabicFeed_FillsOnlyArabicColumns()
        {
            var feed = MakeFeed("ar");
            var item = MakeItem(title: "البنك المركزي يرفع الفائدة");
            var check = new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong, IsOfficial = false };

            var article = NewsArticleMapper.MapToArticle(item, check, feed);

            Assert.NotNull(article);
            Assert.Equal(item.Title, article!.TitleAR);
            Assert.Equal(string.Empty, article.TitleEN);
            Assert.Equal(item.Summary, article.SummaryAR);
            Assert.Null(article.SummaryEN);
            Assert.Equal("ar", article.OriginalLanguage);
        }

        [Fact]
        public void MapToArticle_LocalFeed_SetsScopeLocalAndCountryCode()
        {
            var feed = MakeFeed("en", scope: "Local", countryCode: "JO");
            var article = NewsArticleMapper.MapToArticle(
                MakeItem(), new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong }, feed);

            Assert.NotNull(article);
            Assert.Equal(NewsScope.Local, article!.Scope);
            Assert.Equal("JO", article.CountryCode);
        }

        [Fact]
        public void MapToArticle_InternationalFeed_SetsScopeInternationalAndNullCountryCode()
        {
            var feed = MakeFeed("en", scope: "International", countryCode: null);
            var article = NewsArticleMapper.MapToArticle(
                MakeItem(), new KeywordCheckResult { Verdict = KeywordVerdict.PassedOfficialTopic }, feed);

            Assert.NotNull(article);
            Assert.Equal(NewsScope.International, article!.Scope);
            Assert.Null(article.CountryCode);
        }

        [Fact]
        public void MapToArticle_IsOfficialSource_ReflectsCheckResult()
        {
            var feed = MakeFeed("en", scope: "International", countryCode: null);

            var officialArticle = NewsArticleMapper.MapToArticle(
                MakeItem(), new KeywordCheckResult { Verdict = KeywordVerdict.PassedOfficialTopic, IsOfficial = true }, feed);
            var nonOfficialArticle = NewsArticleMapper.MapToArticle(
                MakeItem(), new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong, IsOfficial = false }, feed);

            Assert.True(officialArticle!.IsOfficialSource);
            Assert.False(nonOfficialArticle!.IsOfficialSource);
        }

        [Fact]
        public void MapToArticle_LongSourceUrl_ReturnsNull()
        {
            var feed = MakeFeed("en");
            var longUrl = "https://example.com/" + new string('a', 800);
            var item = MakeItem(link: longUrl);

            var article = NewsArticleMapper.MapToArticle(
                item, new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong }, feed);

            Assert.Null(article);
        }

        [Fact]
        public void MapToArticle_UrlAtExactly800Chars_IsKept()
        {
            var feed = MakeFeed("en");
            // Build a normalized URL exactly at the 800-char boundary.
            var baseUrl = "https://example.com/";
            var padding = new string('a', 800 - baseUrl.Length);
            var url = baseUrl + padding;
            Assert.Equal(800, url.Length);

            var article = NewsArticleMapper.MapToArticle(
                MakeItem(link: url), new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong }, feed);

            Assert.NotNull(article);
        }

        [Fact]
        public void MapToArticle_DateGuessed_UsesNowInsteadOfOriginal()
        {
            var feed = MakeFeed("en");
            var item = MakeItem(dateGuessed: true); // PublishedAtUtc is 2026-10-07, but guessed

            var before = DateTime.UtcNow;
            var article = NewsArticleMapper.MapToArticle(
                item, new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong }, feed);
            var after = DateTime.UtcNow;

            Assert.NotNull(article);
            Assert.InRange(article!.PublishedAt, before.AddSeconds(-1), after.AddSeconds(1));
            Assert.NotEqual(item.PublishedAtUtc, article.PublishedAt);
        }

        [Fact]
        public void MapToArticle_TranslationStatus_IsAlwaysNotRequested()
        {
            var feed = MakeFeed("en");
            var article = NewsArticleMapper.MapToArticle(
                MakeItem(), new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong }, feed);

            Assert.Equal("NotRequested", article!.TranslationStatus);
            Assert.True(article.IsBankingRelated);
        }

        // ── MatchesExisting (raw vs normalized dedupe-against-DB) ────────

        [Fact]
        public void MatchesExisting_NormalizedUrlFound_ReturnsTrue()
        {
            var existing = new HashSet<string> { "https://example.com/article" };

            Assert.True(NewsArticleMapper.MatchesExisting(
                "https://example.com/article", "https://OLD.example.com/article?utm_source=x", existing));
        }

        [Fact]
        public void MatchesExisting_OnlyRawUrlFound_ReturnsTrue()
        {
            // Simulates a legacy row stored before LinkNormalizer existed —
            // its SourceUrl is the raw, un-normalized form.
            var existing = new HashSet<string> { "https://Example.com/article?utm_source=feed" };

            Assert.True(NewsArticleMapper.MatchesExisting(
                "https://example.com/article", "https://Example.com/article?utm_source=feed", existing));
        }

        [Fact]
        public void MatchesExisting_NeitherFormFound_ReturnsFalse()
        {
            var existing = new HashSet<string> { "https://example.com/some-other-article" };

            Assert.False(NewsArticleMapper.MatchesExisting(
                "https://example.com/article", "https://example.com/article-raw", existing));
        }

        [Fact]
        public void MatchesExisting_NullRawUrl_OnlyChecksNormalized()
        {
            var existing = new HashSet<string> { "https://example.com/article" };

            Assert.True(NewsArticleMapper.MatchesExisting("https://example.com/article", null, existing));
            Assert.False(NewsArticleMapper.MatchesExisting("https://example.com/other", null, existing));
        }
    }
}
