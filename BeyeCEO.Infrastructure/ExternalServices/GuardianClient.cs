using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class GuardianClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ILogger<GuardianClient> _logger;

        // أقسام اقتصادية وبنكية
        private static readonly string[] Sections =
        [
            "business", "money", "banking",
        "economics", "financial-sector"
        ];

        public GuardianClient(
            HttpClient http,
            IConfiguration config,
            ILogger<GuardianClient> logger)
        {
            _http = http;
            _apiKey = config["ExternalApis:Guardian:ApiKey"]!;
            _baseUrl = config["ExternalApis:Guardian:BaseUrl"]!;
            _logger = logger;
        }

        public async Task<List<NewsArticle>> FetchInternationalNewsAsync(
            int pageSize = 10)
        {
            var articles = new List<NewsArticle>();

            try
            {
                foreach (var section in Sections)
                {
                    var url = $"{_baseUrl}/search" +
                              $"?section={section}" +
                              $"&show-fields=headline,bodyText,thumbnail,trailText" +
                              $"&page-size={pageSize}" +
                              $"&order-by=newest" +
                              $"&api-key={_apiKey}";

                    var response = await _http.GetStringAsync(url);
                    var json = JsonDocument.Parse(response);

                    var results = json.RootElement
                        .GetProperty("response")
                        .GetProperty("results");

                    foreach (var item in results.EnumerateArray())
                    {
                        try
                        {
                            var fields = item.TryGetProperty(
                                "fields", out var f) ? f : default;

                            var title = fields.ValueKind != JsonValueKind.Undefined
                                ? fields.GetProperty("headline")
                                    .GetString() ?? string.Empty
                                : item.GetProperty("webTitle")
                                    .GetString() ?? string.Empty;

                            var body = fields.ValueKind != JsonValueKind.Undefined &&
                                fields.TryGetProperty("bodyText", out var bt)
                                ? bt.GetString() ?? string.Empty
                                : string.Empty;

                            var summary = fields.ValueKind != JsonValueKind.Undefined &&
                                fields.TryGetProperty("trailText", out var tt)
                                ? tt.GetString() ?? string.Empty
                                : string.Empty;

                            var imageUrl = fields.ValueKind != JsonValueKind.Undefined &&
                                fields.TryGetProperty("thumbnail", out var thumb)
                                ? thumb.GetString() ?? string.Empty
                                : string.Empty;

                            var sourceUrl = item.GetProperty("webUrl")
                                .GetString() ?? string.Empty;

                            var publishedStr = item.GetProperty("webPublicationDate")
                                .GetString() ?? DateTime.UtcNow.ToString("o");

                            var publishedAt = DateTime.TryParse(
                                publishedStr, out var dt) ? dt : DateTime.UtcNow;

                            // تنظيف الـ HTML من الـ summary
                            summary = CleanHtml(summary);
                            body = CleanHtml(body);

                            var article = NewsArticle.Create(
                                titleEN: title,
                                titleAR: string.Empty,
                                contentEN: body,
                                contentAR: string.Empty,
                                summary: summary[..Math.Min(500, summary.Length)],
                                sourceName: "The Guardian",
                                sourceLogoUrl: string.Empty,
                                sourceUrl: sourceUrl,
                                imageUrl: imageUrl,
                                category: MapCategory(section),
                                scope: NewsScope.International,
                                publishedAt: publishedAt,
                                countryCode: null);

                            articles.Add(article);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                "Guardian: Failed to parse article: {Message}",
                                ex.Message);
                        }
                    }

                    _logger.LogInformation(
                        "Guardian: Fetched {Count} articles from {Section}",
                        results.GetArrayLength(), section);

                    await Task.Delay(500); // rate limiting
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Guardian: ❌ {Message}", ex.Message);
            }

            return articles;
        }

        private static string MapCategory(string section) => section switch
        {
            "business" => "Business",
            "money" => "Finance",
            "banking" => "Banking",
            "economics" => "Economy",
            "financial-sector" => "Banking",
            _ => "General"
        };

        private static string CleanHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            // ازل HTML tags بسيط
            return System.Text.RegularExpressions.Regex
                .Replace(html, "<.*?>", string.Empty)
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Trim();
        }
        // Map البلد → Keywords للبحث
        private static readonly Dictionary<string, string> CountryKeywords = new()
        {
            ["JO"] = "Jordan bank economy finance",
            ["EG"] = "Egypt bank economy finance",
            ["SA"] = "Saudi Arabia bank economy finance",
            ["AE"] = "UAE bank economy finance",
            ["KW"] = "Kuwait bank economy finance",
            ["BH"] = "Bahrain bank economy finance",
            ["QA"] = "Qatar bank economy finance",
            ["GB"] = "UK bank economy finance",
            ["US"] = "US bank economy finance",
        };

        public async Task<List<NewsArticle>> FetchLocalNewsAsync(
            string countryCode, int pageSize = 5)
        {
            var articles = new List<NewsArticle>();

            if (!CountryKeywords.TryGetValue(
                countryCode.ToUpper(), out var keywords))
            {
                _logger.LogWarning(
                    "Guardian: No keywords for {CountryCode}", countryCode);
                return articles;
            }

            try
            {
                var url = $"{_baseUrl}/search" +
                          $"?q={Uri.EscapeDataString(keywords)}" +
                          $"&section=business|money|economics" +
                          $"&show-fields=headline,bodyText,thumbnail,trailText" +
                          $"&page-size={pageSize}" +
                          $"&order-by=newest" +
                          $"&api-key={_apiKey}";

                var response = await _http.GetStringAsync(url);
                var json = JsonDocument.Parse(response);

                var results = json.RootElement
                    .GetProperty("response")
                    .GetProperty("results");

                foreach (var item in results.EnumerateArray())
                {
                    try
                    {
                        var fields = item.TryGetProperty(
                            "fields", out var f) ? f : default;

                        var title = fields.ValueKind != JsonValueKind.Undefined
                            ? fields.GetProperty("headline")
                                .GetString() ?? string.Empty
                            : item.GetProperty("webTitle")
                                .GetString() ?? string.Empty;

                        var body = fields.ValueKind != JsonValueKind.Undefined &&
                            fields.TryGetProperty("bodyText", out var bt)
                            ? bt.GetString() ?? string.Empty
                            : string.Empty;

                        var summary = fields.ValueKind != JsonValueKind.Undefined &&
                            fields.TryGetProperty("trailText", out var tt)
                            ? tt.GetString() ?? string.Empty
                            : string.Empty;

                        var imageUrl = fields.ValueKind != JsonValueKind.Undefined &&
                            fields.TryGetProperty("thumbnail", out var thumb)
                            ? thumb.GetString() ?? string.Empty
                            : string.Empty;

                        var sourceUrl = item.GetProperty("webUrl")
                            .GetString() ?? string.Empty;

                        var publishedStr = item.GetProperty("webPublicationDate")
                            .GetString() ?? DateTime.UtcNow.ToString("o");

                        var publishedAt = DateTime.TryParse(
                            publishedStr, out var dt) ? dt : DateTime.UtcNow;

                        summary = CleanHtml(summary);
                        body = CleanHtml(body);

                        var article = NewsArticle.Create(
                            titleEN: title,
                            titleAR: string.Empty,
                            contentEN: body,
                            contentAR: string.Empty,
                            summary: summary[..Math.Min(500, summary.Length)],
                            sourceName: "The Guardian",
                            sourceLogoUrl: string.Empty,
                            sourceUrl: sourceUrl,
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
                            "Guardian Local: Parse error: {Message}",
                            ex.Message);
                    }
                }

                _logger.LogInformation(
                    "Guardian: ✅ {Count} local articles for {CountryCode}",
                    articles.Count, countryCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Guardian Local: ❌ {CountryCode}: {Message}",
                    countryCode, ex.Message);
            }

            return articles;
        }
    }
}
