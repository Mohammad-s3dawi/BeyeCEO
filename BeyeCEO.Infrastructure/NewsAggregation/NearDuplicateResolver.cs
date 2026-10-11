using BeyeCEO.Domain.News.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    // Decides which of several same-story candidates (same Scope/CountryCode)
    // to keep when they're the same news from different outlets. Pure and
    // generic over whatever candidate type the caller has (ParsedFeedItem
    // for config feeds, NewsArticle for Guardian) via (title, publishedAt,
    // tier) projections, so it's directly unit-testable without any feed or
    // DB plumbing.
    public static class NearDuplicateResolver
    {
        public const double JaccardThreshold = 0.7;

        // Lower = more authoritative. Official central-bank/regulator feeds
        // beat financial trade press, which beats general news.
        private static readonly HashSet<string> TradePressFeedNames =
            new(StringComparer.OrdinalIgnoreCase) { "Finextra", "Banking Dive" };

        public static int GetAuthorityTier(string feedName, bool isOfficial) =>
            isOfficial ? 1 : TradePressFeedNames.Contains(feedName) ? 2 : 3;

        // Given a batch of (Title, PublishedAt, Tier) candidates plus titles
        // of already-existing (recent, same scope/country) DB rows, returns
        // the indices into `candidates` that should be DROPPED as near-
        // duplicates. Within the batch, the most-authoritative (lowest tier)
        // candidate is kept; ties break on earliest PublishedAt. Any
        // candidate matching an existing DB title is always dropped (the
        // already-stored row is treated as authoritative by definition —
        // nothing gets un-stored here).
        public static HashSet<int> FindDuplicateIndices(
            IReadOnlyList<(string Title, DateTime PublishedAt, int Tier)> candidates,
            IEnumerable<string>? recentExistingTitles = null)
        {
            var existingTokenSets = (recentExistingTitles ?? [])
                .Select(TitleSimilarity.Tokenize)
                .Where(t => t.Count > 0)
                .ToList();

            var order = Enumerable.Range(0, candidates.Count)
                .OrderBy(i => candidates[i].Tier)
                .ThenBy(i => candidates[i].PublishedAt)
                .ToList();

            var toDrop = new HashSet<int>();
            var keptTokenSets = new List<HashSet<string>>();

            foreach (var i in order)
            {
                var tokens = TitleSimilarity.Tokenize(candidates[i].Title);

                var dupOfExisting = existingTokenSets.Any(e => TitleSimilarity.Jaccard(tokens, e) >= JaccardThreshold);
                var dupOfKept = keptTokenSets.Any(k => TitleSimilarity.Jaccard(tokens, k) >= JaccardThreshold);

                if (dupOfExisting || dupOfKept)
                    toDrop.Add(i);
                else
                    keptTokenSets.Add(tokens);
            }

            return toDrop;
        }

        // Applies near-duplicate detection across every feed's PassedRaw in
        // one group (same Scope/CountryCode), mutating each FeedFetchResult
        // in place: dropped items move from PassedRaw/PassedItems/the
        // matching passed-verdict counter into NearDuplicateItems/NearDuplicate.
        // Shared by the dry-run endpoint and the real pipeline so both report
        // (and, for the pipeline, actually drop) the same duplicates.
        public static void ApplyToFeedResults(
            IReadOnlyList<FeedFetchResult> results, IEnumerable<string>? recentExistingTitles = null)
        {
            var flatOwners = new List<(FeedFetchResult Owner, int Index)>();
            var candidates = new List<(string Title, DateTime PublishedAt, int Tier)>();

            foreach (var r in results)
            {
                for (var i = 0; i < r.PassedRaw.Count; i++)
                {
                    var (item, check) = r.PassedRaw[i];
                    flatOwners.Add((r, i));
                    candidates.Add((item.Title, item.PublishedAtUtc, GetAuthorityTier(r.Name, check.IsOfficial)));
                }
            }

            if (candidates.Count == 0) return;

            var dropIndices = FindDuplicateIndices(candidates, recentExistingTitles);
            if (dropIndices.Count == 0) return;

            foreach (var ownerGroup in dropIndices
                .Select(gi => flatOwners[gi])
                .GroupBy(x => x.Owner))
            {
                var owner = ownerGroup.Key;
                foreach (var idx in ownerGroup.Select(x => x.Index).OrderByDescending(i => i))
                {
                    var (item, check) = owner.PassedRaw[idx];

                    owner.NearDuplicateItems.Add(new DryRunItemResult
                    {
                        Title = item.Title,
                        PublishedAtUtc = item.PublishedAtUtc,
                        Rule = "near duplicate",
                        MatchedTerm = check.RuleLabel
                    });
                    owner.NearDuplicate++;
                    owner.PassedRaw.RemoveAt(idx);

                    var displayIdx = owner.PassedItems.FindIndex(
                        p => p.Title == item.Title && p.PublishedAtUtc == item.PublishedAtUtc);
                    if (displayIdx >= 0) owner.PassedItems.RemoveAt(displayIdx);

                    switch (check.Verdict)
                    {
                        case KeywordVerdict.PassedStrong: owner.PassedStrong--; break;
                        case KeywordVerdict.PassedWeak: owner.PassedWeak2--; break;
                        case KeywordVerdict.PassedBankName: owner.PassedBankName--; break;
                        case KeywordVerdict.PassedOfficialTopic: owner.PassedOfficialTopic--; break;
                    }
                }
            }
        }

        // Same idea for Guardian, which builds NewsArticle objects directly
        // instead of going through FeedFetchResult/PassedRaw.
        public static List<NewsArticle> ApplyToArticles(
            IReadOnlyList<NewsArticle> candidates,
            IEnumerable<string>? recentExistingTitles,
            out int droppedCount)
        {
            var tuples = candidates
                .Select(a => (
                    Title: string.IsNullOrEmpty(a.TitleEN) ? a.TitleAR : a.TitleEN,
                    a.PublishedAt,
                    GetAuthorityTier(a.FeedName ?? string.Empty, a.IsOfficialSource)))
                .ToList();

            var dropIndices = FindDuplicateIndices(tuples, recentExistingTitles);
            droppedCount = dropIndices.Count;

            return candidates.Where((_, i) => !dropIndices.Contains(i)).ToList();
        }
    }
}
