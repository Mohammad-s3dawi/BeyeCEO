using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    // Cross-source near-duplicate detection: normalize a title down to a
    // token set, then compare sets with Jaccard similarity. Punctuation
    // stripping also absorbs digit-formatting differences (e.g. "28.5" vs
    // "28,5" both become separate "28"/"5" tokens either way).
    public static class TitleSimilarity
    {
        private static readonly Regex PunctuationRegex = new(@"[^\p{L}\p{N}\s]", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        private static readonly HashSet<string> StopwordsEn = new(StringComparer.Ordinal)
        {
            "the", "a", "an", "of", "in", "on", "at", "to", "for", "and", "or", "is", "are",
            "was", "were", "by", "with", "from", "as", "it", "its", "be", "has", "have", "had"
        };

        private static readonly HashSet<string> StopwordsAr = new(
            new[]
            {
                "في", "من", "إلى", "على", "عن", "ما", "هذا", "هذه", "و", "أن", "إن",
                "التي", "الذي", "مع", "بعد", "قبل", "كان", "كانت", "ذلك", "تلك"
            }.Select(s => BankingKeywordFilter.NormalizeArabic(s)),
            StringComparer.Ordinal);

        public static HashSet<string> Tokenize(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return new HashSet<string>();

            var normalized = BankingKeywordFilter.NormalizeArabic(title).ToLowerInvariant();
            normalized = PunctuationRegex.Replace(normalized, " ");
            normalized = WhitespaceRegex.Replace(normalized, " ").Trim();

            return normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !StopwordsEn.Contains(t) && !StopwordsAr.Contains(t))
                .ToHashSet();
        }

        public static double Jaccard(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 || b.Count == 0) return 0;

            var intersection = 0;
            foreach (var t in a) if (b.Contains(t)) intersection++;

            var unionCount = a.Count + b.Count - intersection;
            return unionCount == 0 ? 0 : (double)intersection / unionCount;
        }

        public static double Jaccard(string? titleA, string? titleB) =>
            Jaccard(Tokenize(titleA), Tokenize(titleB));
    }
}
