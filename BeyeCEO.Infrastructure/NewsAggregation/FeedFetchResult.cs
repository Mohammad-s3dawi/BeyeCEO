using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    public class DryRunItemResult
    {
        public string Title { get; set; } = string.Empty;
        public DateTime PublishedAtUtc { get; set; }
        public string Rule { get; set; } = string.Empty;
        public string? MatchedTerm { get; set; }
    }

    public class FeedFetchResult
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // ok | http <code> | empty body | error: ... | parse error: ...
        public int ItemsFetched { get; set; }
        public int ItemsWithSummary { get; set; }
        public int ItemsWithImage { get; set; }
        public DateTime? NewestItemDate { get; set; }
        public bool SourceStale { get; set; }

        public int TooOld { get; set; }
        public int PassedStrong { get; set; }
        public int PassedWeak2 { get; set; }
        public int PassedBankName { get; set; }
        public int PassedOfficialTopic { get; set; }
        public int Rejected { get; set; }

        // Items that passed the keyword filter but were then dropped because
        // a different source (same batch, or an existing recent DB row)
        // already covers the same story — see NearDuplicateResolver.
        public int NearDuplicate { get; set; }

        public int PassedKeywordFilter =>
            PassedStrong + PassedWeak2 + PassedBankName + PassedOfficialTopic;

        public List<DryRunItemResult> PassedItems { get; set; } = new();
        public List<DryRunItemResult> RejectedItems { get; set; } = new();
        public List<DryRunItemResult> NearDuplicateItems { get; set; } = new();

        // Full (uncapped) parsed item + verdict for every item that passed —
        // consumed by the storage pipeline to build NewsArticle rows. Not
        // part of the dry-run HTTP JSON contract.
        [JsonIgnore]
        public List<(ParsedFeedItem Item, KeywordCheckResult Check)> PassedRaw { get; set; } = new();
    }
}
