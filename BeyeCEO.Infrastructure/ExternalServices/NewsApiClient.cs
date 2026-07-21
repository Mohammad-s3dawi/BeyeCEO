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
            "bank OR banking OR finance OR economy OR " +
            "interest rate OR inflation OR GDP OR investment";

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
            var articles = new List<NewsArticle>();

            if (!CountryMap.TryGetValue(
                countryCode.ToUpper(), out var countryInfo))
            {
                _logger.LogWarning(
                    "NewsApi: No mapping for {CountryCode}", countryCode);
                return articles;
            }

            try
            {
                // ← top-headlines بدل everything
                var url = $"{_baseUrl}/top-headlines" +
                          $"?country={countryInfo.Country}" +
                          $"&category=business" +
                          $"&pageSize={pageSize}" +
                          $"&apiKey={_apiKey}";

                var response = await _http.GetStringAsync(url);
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
