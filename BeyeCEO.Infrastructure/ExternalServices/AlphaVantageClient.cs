using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class AlphaVantageClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public AlphaVantageClient(
            HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["ExternalApis:AlphaVantage:ApiKey"]!;
            _baseUrl = config["ExternalApis:AlphaVantage:BaseUrl"]!;
        }

        // Global Quote — S&P500, Nasdaq, DAX
        public async Task<GlobalQuoteResult?> GetGlobalQuoteAsync(
            string symbol)
        {
            var url = $"{_baseUrl}?function=GLOBAL_QUOTE" +
                      $"&symbol={symbol}&apikey={_apiKey}";

            var response = await _http.GetStringAsync(url);
            var json = JsonDocument.Parse(response);

            if (!json.RootElement.TryGetProperty(
                "Global Quote", out var quote))
                return null;

            return new GlobalQuoteResult
            {
                Symbol = symbol,
                Price = decimal.Parse(
                    quote.GetProperty("05. price").GetString() ?? "0"),
                Change = decimal.Parse(
                    quote.GetProperty("09. change").GetString() ?? "0"),
                ChangePct = decimal.Parse(
                    (quote.GetProperty("10. change percent")
                        .GetString() ?? "0%").Replace("%", "")),
                Volume = long.Parse(
                    quote.GetProperty("06. volume").GetString() ?? "0")
            };
        }

        // Currency Exchange Rate
        public async Task<decimal> GetExchangeRateAsync(
            string fromCurrency, string toCurrency)
        {
            var url = $"{_baseUrl}?function=CURRENCY_EXCHANGE_RATE" +
                      $"&from_currency={fromCurrency}" +
                      $"&to_currency={toCurrency}" +
                      $"&apikey={_apiKey}";

            var response = await _http.GetStringAsync(url);
            var json = JsonDocument.Parse(response);

            if (!json.RootElement.TryGetProperty(
                "Realtime Currency Exchange Rate", out var rate))
                return 0;

            return decimal.Parse(
                rate.GetProperty("5. Exchange Rate").GetString() ?? "0");
        }
    }

    public class GlobalQuoteResult
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePct { get; set; }
        public long Volume { get; set; }
    }
}
