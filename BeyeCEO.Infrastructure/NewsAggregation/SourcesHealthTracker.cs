using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    public class SourceHealthEntry
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? LastRunAt { get; set; }
        public DateTime? LastSuccessAt { get; set; }
        public string? LastError { get; set; }
        public int Fetched { get; set; }
        public int Stored { get; set; }
        public DateTime? NewestItemDate { get; set; }
        public bool SourceStale { get; set; }
    }

    // In-memory only (per the Step 7 spec) — resets on app restart. Updated
    // by both the Hangfire jobs and the admin run-on-demand endpoint, read by
    // GET /api/admin/news/sources-health.
    public class SourcesHealthTracker
    {
        private readonly ConcurrentDictionary<string, SourceHealthEntry> _entries = new();

        public void RecordSuccess(string feedName, int fetched, int stored, DateTime? newestItemDate, bool stale)
        {
            var entry = _entries.GetOrAdd(feedName, n => new SourceHealthEntry { Name = n });
            entry.LastRunAt = DateTime.UtcNow;
            entry.LastSuccessAt = DateTime.UtcNow;
            entry.LastError = null;
            entry.Fetched = fetched;
            entry.Stored = stored;
            entry.NewestItemDate = newestItemDate;
            entry.SourceStale = stale;
        }

        public void RecordError(string feedName, string error)
        {
            var entry = _entries.GetOrAdd(feedName, n => new SourceHealthEntry { Name = n });
            entry.LastRunAt = DateTime.UtcNow;
            entry.LastError = error;
        }

        public IReadOnlyList<SourceHealthEntry> Snapshot() =>
            _entries.Values.OrderBy(e => e.Name).ToList();
    }
}
