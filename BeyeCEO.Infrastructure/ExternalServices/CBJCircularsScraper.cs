using BeyeCEO.Domain.MarketData.Entities;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class CBJCircularsScraper
    {
        private readonly ILogger<CBJCircularsScraper> _logger;

        private const string Url =
            "https://www.cbj.gov.jo/AR/List/%D8%A7%D9%84%D8%AA%D8%B9%D8%A7%D9%85%D9%8A%D9%85";

        public CBJCircularsScraper(ILogger<CBJCircularsScraper> logger)
        {
            _logger = logger;
        }

        public async Task<List<CentralBankCircular>> FetchAsync()
        {
            var circulars = new List<CentralBankCircular>();

            try
            {
                _logger.LogInformation(
                    "CBJCircularsScraper: Fetching {Url}", Url);

                var web = new HtmlWeb
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                };
                var doc = await Task.Run(() => web.Load(Url));

                var labels = doc.DocumentNode.SelectNodes("//label");
                var fileSizes = doc.DocumentNode
                    .SelectNodes("//span[contains(@class,'file-size')]");
                var links = doc.DocumentNode
                    .SelectNodes("//a[contains(@class,'DownloadLink')]");

                if (labels == null || links == null)
                {
                    _logger.LogWarning(
                        "CBJCircularsScraper: No rows found on page");
                    return circulars;
                }

                var count = Math.Min(labels.Count, links.Count);

                for (var i = 0; i < count; i++)
                {
                    try
                    {
                        var title = labels[i].InnerText.Trim();
                        var pdfUrl = links[i].GetAttributeValue("href", string.Empty);

                        if (string.IsNullOrWhiteSpace(pdfUrl)) continue;

                        if (!pdfUrl.StartsWith("http"))
                            pdfUrl = "https://www.cbj.gov.jo" + pdfUrl;

                        var fileSize = fileSizes != null && i < fileSizes.Count
                            ? fileSizes[i].InnerText.Trim()
                            : string.Empty;

                        circulars.Add(CentralBankCircular.Create(
                            countryCode: "JO",
                            titleAR: title,
                            titleEN: null,
                            circularNumber: ExtractNumber(title),
                            circularDate: ExtractDate(title),
                            pdfUrl: pdfUrl,
                            fileSize: fileSize));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            "CBJCircularsScraper: Failed to parse row {Index}: {Message}",
                            i, ex.Message);
                    }
                }

                _logger.LogInformation(
                    "CBJCircularsScraper: ✅ Parsed {Count} circulars",
                    circulars.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CBJCircularsScraper: ❌ {Message}", ex.Message);
            }

            // الأحدث فقط
            return circulars
                .OrderByDescending(c => c.CircularDate ?? DateOnly.MinValue)
                .Take(50)
                .ToList();
        }

        private static string? ExtractNumber(string title)
        {
            var match = Regex.Match(title, @"رقم\s*\(?([\d/\-]+)\)?");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static DateOnly? ExtractDate(string title)
        {
            // ← رابط بكلمة "تاريخ" عشان ما نلخبط رقم التعميم مع التاريخ الحقيقي
            var match = Regex.Match(
                title, @"تاريخ\s*(\d{1,2}[/\-]\d{1,2}[/\-]\d{4})");

            if (!match.Success) return null;

            var parts = match.Groups[1].Value.Replace('-', '/').Split('/');
            if (parts.Length != 3) return null;

            if (int.TryParse(parts[0], out var day) &&
                int.TryParse(parts[1], out var month) &&
                int.TryParse(parts[2], out var year))
            {
                try
                {
                    return new DateOnly(year, month, day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
            }

            return null;
        }
    }
}
