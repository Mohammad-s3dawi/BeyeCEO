using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.News.Entities
{
    public class NewsArticle:BaseEntity
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
        public NewsScope Scope { get; private set; }
        public int ReadTimeMinutes { get; private set; }
        public DateTime PublishedAt { get; private set; }

        private NewsArticle() { }

        public static NewsArticle Create(
            string titleEN, string titleAR,
            string contentEN, string contentAR,
            string summary, string sourceName,
            string sourceLogoUrl, string sourceUrl,
            string imageUrl, string category,
            NewsScope scope, DateTime publishedAt)
        {
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
                ReadTimeMinutes = CalculateReadTime(contentEN),
                PublishedAt = publishedAt
            };
        }

        private static int CalculateReadTime(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return 1;
            var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return Math.Max(1, wordCount / 200); // متوسط 200 كلمة/دقيقة
        }
    }
}
