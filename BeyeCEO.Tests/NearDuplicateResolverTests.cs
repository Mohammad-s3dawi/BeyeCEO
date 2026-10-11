using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Infrastructure.NewsAggregation;
using System;
using System.Collections.Generic;

namespace BeyeCEO.Tests
{
    public class NearDuplicateResolverTests
    {
        private static NewsArticle MakeArticle(
            string titleEn, string sourceUrl, DateTime publishedAt, string feedName, bool isOfficial = false) =>
            NewsArticle.Create(
                titleEN: titleEn,
                titleAR: "",
                contentEN: "",
                contentAR: "",
                summary: "summary",
                sourceName: feedName,
                sourceLogoUrl: "",
                sourceUrl: sourceUrl,
                imageUrl: "",
                category: "Banking",
                scope: NewsScope.Local,
                publishedAt: publishedAt,
                countryCode: "JO",
                feedName: feedName,
                isOfficialSource: isOfficial);

        [Fact]
        public void GetAuthorityTier_Official_IsMostAuthoritative()
        {
            Assert.True(
                NearDuplicateResolver.GetAuthorityTier("Bank of England", isOfficial: true) <
                NearDuplicateResolver.GetAuthorityTier("Finextra", isOfficial: false));
        }

        [Fact]
        public void GetAuthorityTier_TradePress_BeatsGeneralNews()
        {
            Assert.True(
                NearDuplicateResolver.GetAuthorityTier("Finextra", isOfficial: false) <
                NearDuplicateResolver.GetAuthorityTier("Al Wakeel News", isOfficial: false));
            Assert.True(
                NearDuplicateResolver.GetAuthorityTier("Banking Dive", isOfficial: false) <
                NearDuplicateResolver.GetAuthorityTier("The Guardian", isOfficial: false));
        }

        [Fact]
        public void FindDuplicateIndices_SameTierCandidates_KeepsEarliestDropsLater()
        {
            var now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
            var candidates = new List<(string Title, DateTime PublishedAt, int Tier)>
            {
                ("الاحتياطيات الأجنبية للبنك المركزي ترتفع إلى 28.5 مليار دولار في نهاية أيلول", now, 3),
                ("الاحتياطيات الأجنبية للبنك المركزي ترتفع إلى 28.5 مليار دولار في نهاية أيلول", now.AddMinutes(-30), 3),
            };

            var dropped = NearDuplicateResolver.FindDuplicateIndices(candidates);

            Assert.Single(dropped);
            Assert.Contains(0, dropped); // the later one (index 0) is dropped; earlier (index 1) survives
        }

        [Fact]
        public void FindDuplicateIndices_UnrelatedTitles_NeitherDropped()
        {
            var now = DateTime.UtcNow;
            var candidates = new List<(string Title, DateTime PublishedAt, int Tier)>
            {
                ("Central bank raises interest rates amid inflation concerns", now, 3),
                ("Local football team wins championship after dramatic final", now, 3),
            };

            var dropped = NearDuplicateResolver.FindDuplicateIndices(candidates);

            Assert.Empty(dropped);
        }

        [Fact]
        public void FindDuplicateIndices_ReorderedEnglishWords_Collapses()
        {
            var now = DateTime.UtcNow;
            var candidates = new List<(string Title, DateTime PublishedAt, int Tier)>
            {
                ("Jordan central bank raises interest rates amid inflation", now, 3),
                ("Interest rates raised amid inflation by Jordan central bank", now.AddMinutes(5), 3),
            };

            var dropped = NearDuplicateResolver.FindDuplicateIndices(candidates);

            Assert.Single(dropped);
        }

        [Fact]
        public void FindDuplicateIndices_MatchingExistingDbTitle_IsDropped()
        {
            var candidates = new List<(string Title, DateTime PublishedAt, int Tier)>
            {
                ("Bank rate maintained at 3.75 percent following committee vote", DateTime.UtcNow, 3),
            };
            var existingTitles = new[] { "Bank rate maintained at 3.75 percent following committee vote" };

            var dropped = NearDuplicateResolver.FindDuplicateIndices(candidates, existingTitles);

            Assert.Single(dropped);
        }

        [Fact]
        public void ApplyToArticles_HigherTierKept_LowerTierDropped()
        {
            var now = DateTime.UtcNow;
            var official = MakeArticle(
                "Central bank raises interest rates amid inflation",
                "https://example.com/official", now, "Bank of England", isOfficial: true);
            var tradePress = MakeArticle(
                "Central bank raises interest rates amid inflation",
                "https://example.com/tradepress", now.AddMinutes(-10), "Finextra");

            var survivors = NearDuplicateResolver.ApplyToArticles(
                new[] { tradePress, official }, recentExistingTitles: null, out var dropped);

            Assert.Equal(1, dropped);
            Assert.Single(survivors);
            Assert.Equal("Bank of England", survivors[0].FeedName);
        }

        [Fact]
        public void ApplyToFeedResults_CrossFeedDuplicateWithinBatch_IsMovedToNearDuplicateItems()
        {
            var now = DateTime.UtcNow;
            var sharedTitle = "الاحتياطيات الأجنبية للبنك المركزي ترتفع إلى 28.5 مليار دولار في نهاية أيلول";

            var alWakeel = new FeedFetchResult { Name = "Al Wakeel News" };
            alWakeel.PassedRaw.Add((
                new ParsedFeedItem { Title = sharedTitle, Link = "https://alwakeelnews.com/a", PublishedAtUtc = now },
                new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong }));
            alWakeel.PassedStrong = 1;

            var alMamlaka = new FeedFetchResult { Name = "Al Mamlaka" };
            alMamlaka.PassedRaw.Add((
                new ParsedFeedItem { Title = sharedTitle, Link = "https://almamlakatv.com/b", PublishedAtUtc = now.AddMinutes(-15) },
                new KeywordCheckResult { Verdict = KeywordVerdict.PassedBankName }));
            alMamlaka.PassedBankName = 1;

            NearDuplicateResolver.ApplyToFeedResults(new[] { alWakeel, alMamlaka });

            var totalSurvivingPassedRaw = alWakeel.PassedRaw.Count + alMamlaka.PassedRaw.Count;
            var totalNearDup = alWakeel.NearDuplicate + alMamlaka.NearDuplicate;

            Assert.Equal(1, totalSurvivingPassedRaw);
            Assert.Equal(1, totalNearDup);
        }
    }
}
