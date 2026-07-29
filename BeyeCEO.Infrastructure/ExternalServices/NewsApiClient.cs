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
    public class NewsApiClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ILogger<NewsApiClient> _logger;

        // Keywords للأخبار البنكية والاقتصادية
        private const string BankingKeywords =
            "central bank OR interest rate OR inflation OR GDP OR " +
            "banking sector OR financial results OR merger OR acquisition OR " +
            "regulatory OR Basel OR capital adequacy";

        // Map البلد → اللغة + الـ Query
        private static readonly Dictionary<string,
            (string Language, string Country)> CountryMap = new()
            {
                ["JO"] = ("ar", "jo"),
                ["EG"] = ("ar", "eg"),
                ["SA"] = ("ar", "sa"),
                ["AE"] = ("ar", "ae"),
                ["KW"] = ("ar", "kw"),
                ["BH"] = ("ar", "bh"),
                ["QA"] = ("ar", "qa"),
                ["GB"] = ("en", "gb"),
                ["US"] = ("en", "us"),
            };

        // Map البلد → مصادر بنكية/مالية موثوقة (للـ /everything fallback)
        private static readonly Dictionary<string, string> CountryDomains = new()
        {
            ["JO"] = "reuters.com,ft.com,arabnews.com",
            ["SA"] = "arabnews.com,reuters.com",
            ["AE"] = "thenationalnews.com,gulfnews.com,reuters.com",
            ["EG"] = "egyptindependent.com,reuters.com",
            ["KW"] = "arabtimesonline.com,reuters.com",
            ["BH"] = "reuters.com,arabnews.com",
            ["QA"] = "gulf-times.com,reuters.com",
            ["GB"] = "ft.com,reuters.com,bbc.co.uk",
            ["US"] = "reuters.com,bloomberg.com",
        };

        public NewsApiClient(
            HttpClient http,
            IConfiguration config,
            ILogger<NewsApiClient> logger)
        {
            _http = http;
            _apiKey = config["ExternalApis:NewsApi:ApiKey"]!;
            _baseUrl = config["ExternalApis:NewsApi:BaseUrl"]!;
            _logger = logger;
        }

        public async Task<List<NewsArticle>> FetchLocalNewsAsync(
         string countryCode, int pageSize = 10)
        {
            if (!CountryMap.TryGetValue(
                countryCode.ToUpper(), out var countryInfo))
            {
                _logger.LogWarning(
                    "NewsApi: No mapping for {CountryCode}", countryCode);
                return new List<NewsArticle>();
            }

            // ← top-headlines بدل everything
            var url = $"{_baseUrl}/top-headlines" +
                      $"?country={countryInfo.Country}" +
                      $"&category=business" +
                      $"&pageSize={pageSize}" +
                      $"&apiKey={_apiKey}";

            var articles = await FetchAndParseAsync(url, countryCode);

            if (articles.Count == 0)
            {
                _logger.LogInformation(
                    "NewsApi: 0 top-headlines for {Country}, falling back to /everything",
                    countryCode);

                var domains = CountryDomains[countryCode.ToUpper()];
                var fallbackUrl = $"{_baseUrl}/everything" +
                                  $"?domains={Uri.EscapeDataString(domains)}" +
                                  $"&sortBy=publishedAt" +
                                  $"&pageSize={pageSize}" +
                                  $"&apiKey={_apiKey}";

                articles = await FetchAndParseAsync(fallbackUrl, countryCode);
            }

            return articles;
        }

        private async Task<List<NewsArticle>> FetchAndParseAsync(
            string url, string countryCode)
        {
            var articles = new List<NewsArticle>();

            try
            {
                var httpResponse = await _http.GetAsync(url);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    _logger.LogError("NewsApi: HTTP {Status} for {Country}. Body: {Body}",
                        (int)httpResponse.StatusCode, countryCode, errorBody);
                    return articles;
                }
                var response = await httpResponse.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(response);

                var status = json.RootElement
                    .GetProperty("status").GetString();

                if (status != "ok")
                {
                    _logger.LogWarning(
                        "NewsApi: Status={Status} for {Country}",
                        status, countryCode);
                    return articles;
                }

                var newsArticles = json.RootElement
                    .GetProperty("articles");

                foreach (var item in newsArticles.EnumerateArray())
                {
                    try
                    {
                        var title = item.GetProperty("title")
                            .GetString() ?? string.Empty;

                        if (title == "[Removed]") continue;

                        var description = item.TryGetProperty(
                            "description", out var desc)
                            ? desc.GetString() ?? string.Empty
                            : string.Empty;

                        var content = item.TryGetProperty(
                            "content", out var cont)
                            ? cont.GetString() ?? string.Empty
                            : string.Empty;

                        var sourceName = item.GetProperty("source")
                            .GetProperty("name")
                            .GetString() ?? string.Empty;

                        var sourceUrl = item.TryGetProperty(
                            "url", out var sUrl)
                            ? sUrl.GetString() ?? string.Empty
                            : string.Empty;

                        var imageUrl = item.TryGetProperty(
                            "urlToImage", out var img)
                            ? img.GetString() ?? string.Empty
                            : string.Empty;

                        var publishedStr = item.TryGetProperty(
                            "publishedAt", out var pub)
                            ? pub.GetString() ?? DateTime.UtcNow.ToString("o")
                            : DateTime.UtcNow.ToString("o");

                        var publishedAt = DateTime.TryParse(
                            publishedStr, out var dt) ? dt : DateTime.UtcNow;

                        var article = NewsArticle.Create(
                            titleEN: title,
                            titleAR: string.Empty,
                            contentEN: content,
                            contentAR: string.Empty,
                            summary: description[..Math.Min(500,
                                description.Length)],
                            sourceName: sourceName,
                            sourceLogoUrl: string.Empty,
                            sourceUrl: sourceUrl,
                            imageUrl: imageUrl ?? string.Empty,
                            category: "Banking",
                            scope: NewsScope.Local,
                            publishedAt: publishedAt,
                            countryCode: countryCode.ToUpper());

                        articles.Add(article);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            "NewsApi: Failed to parse: {Message}",
                            ex.Message);
                    }
                }

                _logger.LogInformation(
                    "NewsApi: ✅ {Count} articles for {Country}",
                    articles.Count, countryCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "NewsApi: ❌ {Country}: {Message}",
                    countryCode, ex.Message);
            }

            return articles;
        }
    }
}
