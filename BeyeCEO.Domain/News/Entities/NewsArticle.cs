using BeyeCEO.Domain.MarketData.Entities;
using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.News.Entities
{
    public class NewsArticle : BaseEntity
    {
        public string TitleEN { get; private set; } = string.Empty;
        public string TitleAR { get; private set; } = string.Empty;
        public string ContentEN { get; private set; } = string.Empty;
        public string ContentAR { get; private set; } = string.Empty;
        public string Summary { get; private set; } = string.Empty;
        public string SourceName { get; private set; } = string.Empty;
        public string SourceLogoUrl { get; private set; } = string.Empty;
        public string SourceUrl { get; private set; } = string.Empty;
        public string ImageUrl { get; private set; } = string.Empty;
        public string Category { get; private set; } = string.Empty;
        public string? CountryCode { get; private set; }        // ← null = دولية
        public NewsScope Scope { get; private set; }
        public int ReadTimeMinutes { get; private set; }
        public DateTime PublishedAt { get; private set; }

        // News pipeline (feed-config aggregation) — all optional/nullable so
        // existing callers (Guardian/NewsApi) keep compiling unchanged.
        public string? SummaryEN { get; private set; }
        public string? SummaryAR { get; private set; }
        public string? OriginalLanguage { get; private set; }   // "ar" | "en"
        public string TranslationStatus { get; private set; } = "NotRequested"; // Done|Pending|NotRequested
        public bool IsBankingRelated { get; private set; } = true;
        public bool IsOfficialSource { get; private set; }
        public string? FeedName { get; private set; }

        // Navigation ← جديد
        public Country? Country { get; private set; }           // ← nullable لأن الدولية ما عندها

        private NewsArticle() { }

        public static NewsArticle Create(
            string titleEN, string titleAR,
            string contentEN, string contentAR,
            string summary, string sourceName,
            string sourceLogoUrl, string sourceUrl,
            string imageUrl, string category,
            NewsScope scope, DateTime publishedAt,
            string? countryCode = null,              // ← null للأخبار الدولية
            string? summaryEN = null,
            string? summaryAR = null,
            string? originalLanguage = null,
            string translationStatus = "NotRequested",
            bool isBankingRelated = true,
            bool isOfficialSource = false,
            string? feedName = null)
        {
            // Business Rule — المحلي لازم عنده CountryCode
            if (scope == NewsScope.Local && string.IsNullOrWhiteSpace(countryCode))
                throw new ArgumentException("Local news must have a CountryCode");

            // Business Rule — الدولي ما عنده CountryCode
            if (scope == NewsScope.International && countryCode != null)
                throw new ArgumentException("International news must not have a CountryCode");

            return new NewsArticle
            {
                TitleEN = titleEN,
                TitleAR = titleAR,
                ContentEN = contentEN,
                ContentAR = contentAR,
                Summary = summary,
                SourceName = sourceName,
                SourceLogoUrl = sourceLogoUrl,
                SourceUrl = sourceUrl,
                ImageUrl = imageUrl,
                Category = category,
                Scope = scope,
                CountryCode = countryCode?.ToUpper(),
                PublishedAt = publishedAt,
                ReadTimeMinutes = CalculateReadTime(contentEN),
                SummaryEN = summaryEN,
                SummaryAR = summaryAR,
                OriginalLanguage = originalLanguage,
                TranslationStatus = translationStatus,
                IsBankingRelated = isBankingRelated,
                IsOfficialSource = isOfficialSource,
                FeedName = feedName
            };
        }

        // Step 11b — og:image fallback sets this after the article is
        // mapped, before it's saved, only when ImageUrl was empty.
        public void SetImageUrl(string imageUrl) => ImageUrl = imageUrl;

        private static int CalculateReadTime(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return 1;
            var wordCount = content.Split(' ',
                StringSplitOptions.RemoveEmptyEntries).Length;
            return Math.Max(1, wordCount / 200);
        }
    }
}
