using BeyeCEO.Infrastructure.NewsAggregation;
using System;

namespace BeyeCEO.Tests
{
    public class FeedFetcherServiceTests
    {
        [Fact]
        public void IsTooOld_ItemOlderThanLimit_ReturnsTrue()
        {
            var now = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);
            var publishedAt = now.AddDays(-45); // 45 days old on a 30-day feed

            Assert.True(FeedFetcherService.IsTooOld(publishedAt, now, maxAgeDays: 30));
        }

        [Fact]
        public void IsTooOld_ItemWithinLimit_ReturnsFalse()
        {
            var now = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);
            var publishedAt = now.AddDays(-10);

            Assert.False(FeedFetcherService.IsTooOld(publishedAt, now, maxAgeDays: 30));
        }

        [Fact]
        public void IsTooOld_ExactlyAtLimit_ReturnsFalse()
        {
            var now = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);
            var publishedAt = now.AddDays(-30);

            Assert.False(FeedFetcherService.IsTooOld(publishedAt, now, maxAgeDays: 30));
        }

        [Fact]
        public void IsTooOld_RespectsHigherPerFeedOverride()
        {
            var now = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);
            var publishedAt = now.AddDays(-45); // would be too old at 30d, not at 60d

            Assert.True(FeedFetcherService.IsTooOld(publishedAt, now, maxAgeDays: 30));
            Assert.False(FeedFetcherService.IsTooOld(publishedAt, now, maxAgeDays: 60));
        }
    }
}
