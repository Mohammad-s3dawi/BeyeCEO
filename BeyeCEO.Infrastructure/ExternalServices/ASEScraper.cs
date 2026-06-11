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
    public class ASEScraper
    {
        private readonly HttpClient _http;
        private readonly ILogger<ASEScraper> _logger;

        private const string Url =
            "https://www.ase.com.jo/en/daily_summary";

        public ASEScraper(
            HttpClient http,
            ILogger<ASEScraper> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<StockExchangeData?> FetchAsync()
        {
            try
            {
                _logger.LogInformation(
                    "ASEScraper: Fetching {Url}", Url);

                var html = await _http.GetStringAsync(Url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // جيب كل الـ paragraphs
                var paragraphs = doc.DocumentNode
                    .SelectNodes("//p")
                    ?.Select(p => p.InnerText.Trim())
                    .ToList() ?? new List<string>();

                var fullText = string.Join(" ", paragraphs);

                _logger.LogDebug(
                    "ASEScraper: Raw text = {Text}",
                    fullText[..Math.Min(200, fullText.Length)]);

                // استخرج البيانات
                var tradingAmount = ExtractDecimal(
                    fullText,
                    @"reached JD\((\d+\.?\d*)\) million");

                var volume = ExtractDecimal(
                    fullText,
                    @"\((\d+\.?\d*)\) million shares");

                var transactions = ExtractInt(
                    fullText,
                    @"through \((\d+[,\d]*)\)");

                var index = ExtractDecimal(
                    fullText,
                    @"closed at \((\d+\.?\d*)\)");

                var gainers = ExtractInt(
                    fullText,
                    @"prices of \((\d+)\) companies rose");

                var losers = ExtractInt(
                    fullText,
                    @"prices of \((\d+)\) declined");

                // تحديد الـ ChangePct
                var isIncrease = fullText.Contains("an increase");
                var changePct = ExtractDecimal(
                    fullText,
                    @"(?:increase|decrease) of \((\d+\.?\d*)%\)");

                if (!isIncrease) changePct = -changePct;

                // تحقق إن البيانات موجودة
                if (index == 0 || tradingAmount == 0)
                {
                    _logger.LogWarning(
                        "ASEScraper: Could not extract data");
                    return null;
                }

                var tradeDate = DateOnly.FromDateTime(DateTime.Now);
                var unchanged = 106 - gainers - losers; // تقريبي

                var data = StockExchangeData.Create(
                    countryCode: "JO",
                    exchange: "ASE",
                    tradingAmount: tradingAmount * 1_000_000,
                    tradingVolume: (long)(volume * 1_000_000),
                    transactions: transactions,
                    bankingIndex: index,
                    generalIndex: index,
                    tradeDate: tradeDate,
                    gainers: gainers,
                    losers: losers,
                    unchanged: Math.Max(0, unchanged),
                    changePct: changePct,
                    previousIndex: index / (1 + changePct / 100));

                _logger.LogInformation(
                    "ASEScraper: ✅ Index={Index} " +
                    "Amount={Amount}M ChangePct={ChangePct}%",
                    index, tradingAmount, changePct);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ASEScraper: ❌ Failed: {Message}", ex.Message);
                return null;
            }
        }

        // ── Top Movers ────────────────────────────────────────
        public async Task<List<StockExchangeTopMover>>
            FetchTopMoversAsync()
        {
            var movers = new List<StockExchangeTopMover>();

            try
            {
                var html = await _http.GetStringAsync(Url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var paragraphs = doc.DocumentNode
                    .SelectNodes("//p")
                    ?.Select(p => p.InnerText.Trim())
                    .ToList() ?? new List<string>();

                var fullText = string.Join(" ", paragraphs);
                var tradeDate = DateOnly.FromDateTime(DateTime.Now);

                // استخرج Top 5 Gainers
                movers.AddRange(
                    ExtractTopMovers(fullText, "gainers", tradeDate));

                // استخرج Top 5 Losers
                movers.AddRange(
                    ExtractTopMovers(fullText, "losers", tradeDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ASEScraper TopMovers: ❌ {Message}", ex.Message);
            }

            return movers;
        }

        private List<StockExchangeTopMover> ExtractTopMovers(
            string text, string type, DateOnly tradeDate)
        {
            var movers = new List<StockExchangeTopMover>();
            var moverType = type == "gainers" ? "Gainer" : "Loser";

            // Pattern: "Company Name by (X.XX%)"
            var keyword = type == "gainers"
                ? "top five gainers were"
                : "top five losers were";

            var startIdx = text.IndexOf(
                keyword, StringComparison.OrdinalIgnoreCase);

            if (startIdx < 0) return movers;

            var section = text[startIdx..Math.Min(startIdx + 500, text.Length)];

            // استخرج الأسماء والنسب
            var pattern = @"([\w\s&\.]+)\. by \((\d+\.?\d*)%\)|" +
                          @"([\w\s&\.]+) by \((\d+\.?\d*)%\)";

            var matches = Regex.Matches(section, pattern);
            byte rank = 1;

            foreach (Match match in matches.Take(5))
            {
                var companyName = (match.Groups[1].Value +
                    match.Groups[3].Value).Trim();
                var changePct = decimal.Parse(
                    match.Groups[2].Value +
                    match.Groups[4].Value);

                if (string.IsNullOrEmpty(companyName)) continue;

                movers.Add(StockExchangeTopMover.Create(
                    countryCode: "JO",
                    exchange: "ASE",
                    companyName: companyName,
                    symbol: companyName[..Math.Min(10,
                        companyName.Length)].ToUpper(),
                    price: 0,
                    changePct: type == "losers"
                        ? -changePct : changePct,
                    moverType: moverType,
                    rank: rank++,
                    tradeDate: tradeDate));
            }

            return movers;
        }

        // ── Helpers ───────────────────────────────────────────
        private static decimal ExtractDecimal(
            string text, string pattern)
        {
            var match = Regex.Match(text, pattern);
            if (!match.Success) return 0;
            var value = match.Groups[1].Value.Replace(",", "");
            return decimal.TryParse(value, out var result)
                ? result : 0;
        }

        private static int ExtractInt(
            string text, string pattern)
        {
            var match = Regex.Match(text, pattern);
            if (!match.Success) return 0;
            var value = match.Groups[1].Value.Replace(",", "");
            return int.TryParse(value, out var result) ? result : 0;
        }
    }
}
