using BeyeCEO.Infrastructure.NewsAggregation.ImageFallback;
using System.Diagnostics;

namespace BeyeCEO.Tests
{
    public class DomainRateLimiterTests
    {
        [Fact]
        public async Task WaitForSlotAsync_SecondCallWithinCrawlDelay_WaitsTheRemainder()
        {
            var limiter = new DomainRateLimiter();
            const double crawlDelaySeconds = 0.3;

            await limiter.WaitForSlotAsync("example.com", crawlDelaySeconds);

            var sw = Stopwatch.StartNew();
            await limiter.WaitForSlotAsync("example.com", crawlDelaySeconds);
            sw.Stop();

            Assert.True(sw.Elapsed.TotalSeconds >= crawlDelaySeconds - 0.05,
                $"expected a wait of ~{crawlDelaySeconds}s, got {sw.Elapsed.TotalSeconds}s");
        }

        [Fact]
        public async Task WaitForSlotAsync_DifferentDomains_DoNotWaitOnEachOther()
        {
            var limiter = new DomainRateLimiter();

            await limiter.WaitForSlotAsync("example.com", 5.0);

            var sw = Stopwatch.StartNew();
            await limiter.WaitForSlotAsync("other.com", 5.0);
            sw.Stop();

            Assert.True(sw.Elapsed.TotalSeconds < 1.0,
                $"a different domain should not be throttled by example.com's crawl-delay, took {sw.Elapsed.TotalSeconds}s");
        }

        [Fact]
        public async Task WaitForSlotAsync_NoDeclaredCrawlDelay_StillAppliesPolitenessDefault()
        {
            var limiter = new DomainRateLimiter();

            await limiter.WaitForSlotAsync("example.com", crawlDelaySeconds: null);

            var sw = Stopwatch.StartNew();
            await limiter.WaitForSlotAsync("example.com", crawlDelaySeconds: null);
            sw.Stop();

            // Default politeness spacing is 1 second when no Crawl-delay is declared.
            Assert.True(sw.Elapsed.TotalSeconds >= 0.9,
                $"expected the ~1s politeness default, got {sw.Elapsed.TotalSeconds}s");
        }
    }
}
