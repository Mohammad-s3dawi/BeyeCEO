using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class RssNewsClient
    {
        private readonly HttpClient _http;
        private readonly TranslationService _translator;
        private readonly ILogger<RssNewsClient> _logger;

        // بنكية/اقتصادية — لفلترة عناصر الـ RSS
        private static readonly string[] BankingKeywords =
        [
            "bank", "banking", "finance", "economy",
            "interest", "inflation", "investment",
            "بنك", "مصرف", "مصرفي", "مالية", "اقتصاد",
            "فائدة", "تضخم", "استثمار", "دينار"
        ];

        // Map البلد → روابط RSS المتاحة والشغالة فعلاً
        private static readonly Dictionary<string, string[]> CountryRssFeeds = new()
        {
            ["JO"] =
            [
                "https://en.ammonnews.net/rss.php?type=news&id=2", // English
                "https://jo24.net/feed/rss.php"                     // Arabic
            ],
        };

        public RssNewsClient(
            HttpClient http, TranslationService translator, ILogger<RssNewsClient> logger)
        {
            _http = http;
            _translator = translator;
            _logger = logger;
        }

        public bool HasFeeds(string countryCode) =>
            CountryRssFeeds.ContainsKey(countryCode.ToUpper());

        public async Task<List<NewsArticle>> FetchLocalNewsAsync(string countryCode)
        {
            var articles = new List<NewsArticle>();

            if (!CountryRssFeeds.TryGetValue(
                countryCode.ToUpper(), out var feeds))
            {
                _logger.LogWarning(
                    "Rss: No feeds for {CountryCode}", countryCode);
                return articles;
            }

            foreach (var feedUrl in feeds)
            {
                try
                {
                    var xml = await _http.GetStringAsync(feedUrl);
                    var doc = XDocument.Parse(xml);
                    var sourceName = new Uri(feedUrl).Host
                        .Replace("en.", string.Empty)
                        .Replace("www.", string.Empty);
                    var isEnglish = feedUrl.Contains("en.");

                    foreach (var item in doc.Descendants("item"))
                    {
                        try
                        {
                            var title = item.Element("title")?.Value ?? string.Empty;
                            var description = item.Element("description")?.Value ?? string.Empty;

                            if (!IsBankingRelated(title, description)) continue;

                            var link = item.Element("link")?.Value ?? string.Empty;

                            var pubDateStr = item.Element("pubDate")?.Value;
                            var publishedAt = DateTime.TryParse(
                                pubDateStr, out var dt) ? dt : DateTime.UtcNow;

                            var imageUrl = item.Element("enclosure")
                                ?.Attribute("url")?.Value ?? string.Empty;

                            var summary = CleanHtml(description);
                            var cleanTitle = CleanHtml(title);

                            var titleEN = isEnglish
                                ? cleanTitle
                                : await _translator.TranslateAsync(cleanTitle, "ar", "en");
                            var titleAR = isEnglish
                                ? await _translator.TranslateAsync(cleanTitle, "en", "ar")
                                : cleanTitle;

                            var article = NewsArticle.Create(
                                titleEN: titleEN,
                                titleAR: titleAR,
                                contentEN: isEnglish ? summary : string.Empty,
                                contentAR: isEnglish ? string.Empty : summary,
                                summary: summary[..Math.Min(500, summary.Length)],
                                sourceName: sourceName,
                                sourceLogoUrl: string.Empty,
                                sourceUrl: link,
                                imageUrl: imageUrl,
                                category: "Banking",
                                scope: NewsScope.Local,
                                publishedAt: publishedAt,
                                countryCode: countryCode.ToUpper());

                            articles.Add(article);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                "Rss: Failed to parse item from {Feed}: {Message}",
                                feedUrl, ex.Message);
                        }
                    }

                    _logger.LogInformation(
                        "Rss: ✅ {Count} matching articles from {Feed}",
                        articles.Count, feedUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Rss: ❌ {Feed}: {Message}",
                        feedUrl, ex.Message);
                }
            }

            return articles;
        }

        private static bool IsBankingRelated(string title, string description)
        {
            var text = $"{title} {description}".ToLowerInvariant();
            return BankingKeywords.Any(text.Contains);
        }

        private static string CleanHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            return Regex.Replace(html, "<.*?>", string.Empty)
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Trim();
        }
    }
}
