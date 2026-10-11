using System;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    public class ParsedFeedItem
    {
        public string Title { get; set; } = string.Empty;

        // Normalized (host lowercased, utm_* and fragment stripped) — see LinkNormalizer.
        public string Link { get; set; } = string.Empty;

        // Pre-normalization value straight from the feed XML. Kept so a DB
        // existence check can also match legacy rows that were stored with
        // raw, unnormalized URLs before LinkNormalizer existed.
        public string RawLink { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime PublishedAtUtc { get; set; }
        public bool DateGuessed { get; set; }
        public string? ImageUrl { get; set; }
    }
}
