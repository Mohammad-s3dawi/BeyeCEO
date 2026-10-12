using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.NewsAggregation.ImageFallback
{
    public interface IDomainRateLimiter
    {
        Task WaitForSlotAsync(string domain, double? crawlDelaySeconds, CancellationToken ct = default);
    }

    // Singleton — enforces a minimum spacing between requests to the same
    // domain, honoring robots.txt Crawl-delay when declared (falls back to
    // a 1-second politeness default otherwise). Shared across runs so two
    // jobs firing close together still respect the same domain's pacing.
    public class DomainRateLimiter : IDomainRateLimiter
    {
        private const double DefaultMinSpacingSeconds = 1.0;

        private readonly ConcurrentDictionary<string, DateTime> _lastFetchUtc = new(StringComparer.OrdinalIgnoreCase);

        public async Task WaitForSlotAsync(string domain, double? crawlDelaySeconds, CancellationToken ct = default)
        {
            var minSpacing = TimeSpan.FromSeconds(Math.Max(crawlDelaySeconds ?? DefaultMinSpacingSeconds, DefaultMinSpacingSeconds));

            if (_lastFetchUtc.TryGetValue(domain, out var last))
            {
                var elapsed = DateTime.UtcNow - last;
                if (elapsed < minSpacing)
                    await Task.Delay(minSpacing - elapsed, ct);
            }

            _lastFetchUtc[domain] = DateTime.UtcNow;
        }
    }
}
