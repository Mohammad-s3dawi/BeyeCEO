using System;
using System.Collections.Concurrent;

namespace BeyeCEO.Infrastructure.NewsAggregation.ImageFallback
{
    public interface IImageFetchFailureCache
    {
        bool HasRecentFailure(string url);
        void RecordFailure(string url);
    }

    // Singleton — remembers URLs that failed (timeout, error status, no
    // usable image found) for 24h so repeated runs don't keep re-hitting a
    // page that's known to fail.
    public class ImageFetchFailureCache : IImageFetchFailureCache
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

        private readonly ConcurrentDictionary<string, DateTime> _failures = new();

        public bool HasRecentFailure(string url) =>
            _failures.TryGetValue(url, out var at) && DateTime.UtcNow - at < Ttl;

        public void RecordFailure(string url) => _failures[url] = DateTime.UtcNow;
    }
}
