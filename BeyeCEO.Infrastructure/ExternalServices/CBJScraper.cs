using BeyeCEO.Domain.MarketData.Entities;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class CBJScraper
    {
        private readonly HttpClient _http;
        private readonly ILogger<CBJScraper> _logger;

        private const string Url =
            "https://www.cbj.gov.jo/Default/Ar";

        public CBJScraper(
            HttpClient http,
            ILogger<CBJScraper> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<LocalIndicator>> FetchAsync()
        {
            var indicators = new List<LocalIndicator>();

            try
            {
                _logger.LogInformation(
                    "CBJScraper: Fetching {Url}", Url);

                var web = new HtmlWeb();
                var doc = await Task.Run(() => web.Load(Url));

                // جيب كل النصوص من الصفحة
                var allText = doc.DocumentNode.InnerText;

                var periodDate = DateOnly.FromDateTime(DateTime.Now);

                // استخرج أسعار الفائدة
                var rates = new Dictionary<string, string>
                {
                    ["MAIN_RATE"] = ExtractRate(
                        allText, "سعر الفائدة الرئيسي"),
                    ["REDISCOUNT_RATE"] = ExtractRate(
                        allText, "سعر إعادة الخصم"),
                    ["REPO_RATE"] = ExtractRate(
                        allText, "سعر فائدة اتفاقيات إعادة الشراء"),
                    ["DEPOSIT_RATE"] = ExtractRate(
                        allText, "سعر نافذة الإيداع")
                };

                var rateNames = new Dictionary<string, (string EN, string AR)>
                {
                    ["MAIN_RATE"] = ("Main Interest Rate", "سعر الفائدة الرئيسي"),
                    ["REDISCOUNT_RATE"] = ("Re-Discount Rate", "سعر إعادة الخصم"),
                    ["REPO_RATE"] = ("Overnight Repo Rate", "سعر إعادة الشراء"),
                    ["DEPOSIT_RATE"] = ("Deposit Window Rate", "سعر نافذة الإيداع")
                };

                foreach (var (code, valueStr) in rates)
                {
                    if (string.IsNullOrEmpty(valueStr)) continue;
                    if (!decimal.TryParse(valueStr, out var value)) continue;

                    indicators.Add(LocalIndicator.Create(
                        countryCode: "JO",
                        indicatorCode: code,
                        nameEN: rateNames[code].EN,
                        nameAR: rateNames[code].AR,
                        value: value,
                        unit: "%",
                        periodDate: periodDate));

                    _logger.LogInformation(
                        "CBJScraper: ✅ {Code} = {Value}%",
                        code, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CBJScraper: ❌ {Message}", ex.Message);
            }

            return indicators;
        }

        private static string ExtractRate(
            string text, string label)
        {
            var idx = text.IndexOf(
                label, StringComparison.OrdinalIgnoreCase);

            if (idx < 0) return string.Empty;

            // خذ النص بعد الـ label
            var after = text[(idx + label.Length)..Math.Min(idx + label.Length + 20, text.Length)];

            // استخرج الرقم
            var match = Regex.Match(after, @"(\d+\.?\d*)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
