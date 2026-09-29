using BeyeCEO.Domain.MarketData.Entities;
using BeyeCEO.Domain.MarketData.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class BankAdsScraper
    {
        private readonly IMarketDataRepository _repo;
        private readonly ILogger<BankAdsScraper> _logger;

        private static readonly string[] Keywords =
            ["banner", "offer", "promo", "campaign"];

        public BankAdsScraper(IMarketDataRepository repo, ILogger<BankAdsScraper> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<List<BankAd>> FetchAllAsync(string countryCode)
        {
            var allAds = new List<BankAd>();
            var websites = await _repo.GetActiveBankWebsitesAsync(countryCode);

            foreach (var bank in websites)
            {
                try
                {
                    var ads = await FetchBankAsync(
                        bank.BankName, bank.BankNameAR, bank.WebsiteUrl, bank.ScrapingStrategy);
                    allAds.AddRange(ads);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "BankAdsScraper: ❌ {Bank}: {Message}",
                        bank.BankName, ex.Message);
                }
            }

            return allAds;
        }

        private async Task<List<BankAd>> FetchBankAsync(
            string name, string nameAR, string url, string strategy)
        {
            var ads = new List<BankAd>();

            var web = new HtmlWeb { UserAgent = "BeyeCEO/1.0" };
            var doc = await Task.Run(() => web.Load(url));

            var baseUri = new Uri(url);
            var candidates = SelectImages(doc, strategy);

            foreach (var img in candidates)
            {
                var src = img.GetAttributeValue("src", string.Empty);
                if (string.IsNullOrWhiteSpace(src)) continue;

                if (src.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    continue;

                var width = ParseIntAttribute(img, "width");
                if (width.HasValue && width.Value < 200) continue;

                var fullUrl = ResolveUrl(baseUri, src);

                // Arab Bank أحياناً بيرجع روابط مطلقة عالدومين arabbank.com.jo
                // (الـ CDN) بدل arabbank.jo لنفس الصور — وحّدها عالدومين الأساسي
                // عشان ما تنكرر نفس الصورة مرتين بدومينين مختلفين
                if (name == "Arab Bank")
                    fullUrl = NormalizeArabBankUrl(fullUrl);

                var alt = img.GetAttributeValue("alt", string.Empty);

                ads.Add(BankAd.Create(
                    bankName: name,
                    bankNameAR: nameAR,
                    countryCode: "JO",
                    imageUrl: fullUrl,
                    altText: alt,
                    sourceUrl: url));

                if (ads.Count >= 5) break;
            }

            _logger.LogInformation(
                "BankAdsScraper: ✅ {Bank}: {Count} images found",
                name, ads.Count);

            return ads;
        }

        private static IEnumerable<HtmlNode> SelectImages(
            HtmlDocument doc, string strategy)
        {
            var allImgs = doc.DocumentNode.SelectNodes("//img");
            if (allImgs == null) return [];

            if (strategy == "keyword")
            {
                return allImgs.Where(img =>
                {
                    var src = img.GetAttributeValue("src", string.Empty)
                        .ToLowerInvariant();
                    return Keywords.Any(src.Contains);
                });
            }

            if (strategy.StartsWith("class:"))
            {
                var className = strategy["class:".Length..];
                return allImgs.Where(img =>
                    img.GetAttributeValue("class", string.Empty)
                        .Contains(className, StringComparison.OrdinalIgnoreCase));
            }

            if (strategy == "wordpress")
            {
                return allImgs.Where(img =>
                    img.GetAttributeValue("src", string.Empty)
                        .Contains("wp-content/uploads", StringComparison.OrdinalIgnoreCase));
            }

            if (strategy == "hero")
            {
                var scoped = doc.DocumentNode
                    .SelectNodes("//main//img | //section//img");
                var pool = scoped ?? allImgs;

                return pool.Where(img =>
                {
                    var width = ParseIntAttribute(img, "width");
                    return !width.HasValue || width.Value > 500;
                });
            }

            return [];
        }

        private static int? ParseIntAttribute(HtmlNode node, string attr)
        {
            var value = node.GetAttributeValue(attr, string.Empty);
            return int.TryParse(value, out var result) ? result : null;
        }

        private static string ResolveUrl(Uri baseUri, string src) =>
            Uri.TryCreate(baseUri, src, out var resolved)
                ? resolved.ToString()
                : src;

        private static string NormalizeArabBankUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return url;

            if (!uri.Host.Contains("arabbank.com.jo", StringComparison.OrdinalIgnoreCase))
                return url;

            var builder = new UriBuilder(uri) { Host = "www.arabbank.jo" };
            return builder.Uri.ToString();
        }
    }
}
