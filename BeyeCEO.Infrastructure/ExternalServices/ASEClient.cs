using BeyeCEO.Domain.MarketData.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{

    public class ASEClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ASEClient> _logger;

        private const string MarketIndexUrl =
            "https://www.ase.com.jo/en/api/v1/market-index?_format=json";

        private const string TickerFeedsUrl =
            "https://www.ase.com.jo/en/ticker_feeds";

        public ASEClient(HttpClient http, ILogger<ASEClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<StockExchangeData?> FetchMarketDataAsync()
        {
            try
            {
                _logger.LogInformation(
                    "ASEClient: Fetching market index");

                var response = await _http.GetStringAsync(MarketIndexUrl);
                var items = System.Text.Json.JsonSerializer
                    .Deserialize<List<ASEMarketIndex>>(response);
                var item = items?.FirstOrDefault();
                if (item == null)
                {
                    _logger.LogWarning("ASEClient: No market index data");
                    return null;
                }

                var index = decimal.Parse(item.IndexValue);
                var changePct = decimal.Parse(item.PercentageOfVariation);

                // جيب بيانات الأسهم للـ Gainers/Losers
                var tickerData = await FetchTickerAsync();
                var listedStocks = tickerData
                    .Where(x => x.GroupCode == "11" ||
                                x.GroupCode == "21")
                    .ToList();

                var gainers = listedStocks.Count(x =>
                    decimal.Parse(x.CurrentVariationPercent) > 0);
                var losers = listedStocks.Count(x =>
                    decimal.Parse(x.CurrentVariationPercent) < 0);
                var unchanged = listedStocks.Count(x =>
                    decimal.Parse(x.CurrentVariationPercent) == 0);

                // Parse التاريخ
                var tradeDate = DateOnly.FromDateTime(DateTime.Now);
                if (DateTime.TryParse(item.Date, out var parsedDate))
                    tradeDate = DateOnly.FromDateTime(parsedDate);

                var data = StockExchangeData.Create(
                    countryCode: "JO",
                    exchange: "ASE",
                    tradingAmount: 0,       // محتاج API ثانية
                    tradingVolume: 0,       // محتاج API ثانية
                    transactions: 0,        // محتاج API ثانية
                    bankingIndex: index,
                    generalIndex: index,
                    tradeDate: tradeDate,
                    gainers: gainers,
                    losers: losers,
                    unchanged: unchanged,
                    changePct: changePct,
                    previousIndex: index / (1 + changePct / 100));

                _logger.LogInformation(
                    "ASEClient: ✅ Index={Index} ChangePct={ChangePct}% " +
                    "Gainers={Gainers} Losers={Losers}",
                    index, changePct, gainers, losers);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ASEClient: ❌ {Message}", ex.Message);
                return null;
            }
        }

        public async Task<List<StockExchangeTopMover>>
            FetchTopMoversAsync()
        {
            var movers = new List<StockExchangeTopMover>();

            try
            {
                var tickerData = await FetchTickerAsync();
                var tradeDate = DateOnly.FromDateTime(DateTime.Now);

                // فرز حسب التغيير
                var stocks = tickerData
                    .Where(x => x.GroupCode != "20") // ازل الـ Indices
                    .Select(x => new
                    {
                        x.ObjectId,
                        x.NameShort,
                        Price = decimal.TryParse(
                            x.CurrentPrice, out var p) ? p : 0,
                        ChangePct = decimal.TryParse(
                            x.CurrentVariationPercent, out var c) ? c : 0
                    })
                    .ToList();

                // Top 5 Gainers
                var gainers = stocks
                    .Where(x => x.ChangePct > 0)
                    .OrderByDescending(x => x.ChangePct)
                    .Take(5)
                    .ToList();

                // Top 5 Losers
                var losers = stocks
                    .Where(x => x.ChangePct < 0)
                    .OrderBy(x => x.ChangePct)
                    .Take(5)
                    .ToList();

                byte rank = 1;
                foreach (var g in gainers)
                {
                    movers.Add(StockExchangeTopMover.Create(
                        "JO", "ASE", g.NameShort, g.ObjectId,
                        g.Price, g.ChangePct, "Gainer", rank++,
                        tradeDate));
                }

                rank = 1;
                foreach (var l in losers)
                {
                    movers.Add(StockExchangeTopMover.Create(
                        "JO", "ASE", l.NameShort, l.ObjectId,
                        l.Price, Math.Abs(l.ChangePct),
                        "Loser", rank++, tradeDate));
                }

                _logger.LogInformation(
                    "ASEClient: ✅ {G} Gainers, {L} Losers",
                    gainers.Count, losers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ASEClient TopMovers: ❌ {Message}", ex.Message);
            }

            return movers;
        }

        private async Task<List<ASETicker>> FetchTickerAsync()
        {
            var response = await _http.GetStringAsync(TickerFeedsUrl);
            return JsonSerializer.Deserialize<List<ASETicker>>(response)
                ?? new List<ASETicker>();
        }
    }

    // ── Response Models ───────────────────────────────────────
    public class ASEMarketIndex
    {
        [JsonPropertyName("index_value")]
        public string IndexValue { get; set; } = string.Empty;

        [JsonPropertyName("percentage_of_variation")]
        public string PercentageOfVariation { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;
    }

    public class ASETicker
    {
        [JsonPropertyName("object_id")]
        public string ObjectId { get; set; } = string.Empty;

        [JsonPropertyName("current_price")]
        public string CurrentPrice { get; set; } = string.Empty;

        [JsonPropertyName("current_variation_percent")]
        public string CurrentVariationPercent { get; set; } = string.Empty;

        [JsonPropertyName("group_code")]
        public string GroupCode { get; set; } = string.Empty;

        [JsonPropertyName("name_long")]
        public string NameLong { get; set; } = string.Empty;

        [JsonPropertyName("name_short")]
        public string NameShort { get; set; } = string.Empty;
    }
}
