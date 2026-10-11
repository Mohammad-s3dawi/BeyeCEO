namespace BeyeCEO.Infrastructure.NewsAggregation
{
    public class FeedOptions
    {
        // Stored as NewsArticle.SourceName when this feed is ever wired to the save
        // path — must always be this exact name, never a generic one.
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Format { get; set; } = "rss"; // rss | atom | rdf
        public string Language { get; set; } = "en"; // ar | en
        public string Scope { get; set; } = "Local"; // Local | International
        public string? CountryCode { get; set; }
        public string PollGroup { get; set; } = "local-rss"; // local-rss | international
        public int MaxItemsPerRun { get; set; } = 15;
        public int ExpectedMaxAgeDays { get; set; } = 14;

        // Per-feed override for News:MaxItemAgeDays (global default). Items
        // older than this are skipped entirely (counted as "tooOld") so a
        // first run doesn't backfill months of old items. Null = use the
        // global default.
        public int? MaxItemAgeDays { get; set; }

        public bool Enabled { get; set; } = true;
    }
}
