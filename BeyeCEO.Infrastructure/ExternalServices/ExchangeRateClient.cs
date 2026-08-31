using BeyeCEO.Domain.MarketData.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class ExchangeRateClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExchangeRateClient> _logger;

        private const string Url = "https://open.er-api.com/v6/latest/USD";

        private static readonly string[] TargetCurrencies =
        [
            "EUR", "GBP", "JPY", "CHF",
            "AED", "JOD", "SAR", "KWD", "EGP", "BHD", "QAR"
        ];

        public ExchangeRateClient(HttpClient http, ILogger<ExchangeRateClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<CurrencyRate>> FetchRatesAsync()
        {
            var rates = new List<CurrencyRate>();

            try
            {
                _logger.LogInformation(
                    "ExchangeRateClient: Fetching {Url}", Url);

                var response = await _http.GetStringAsync(Url);
                var json = JsonDocument.Parse(response);

                var result = json.RootElement.GetProperty("result").GetString();
                if (result != "success")
                {
                    _logger.LogWarning(
                        "ExchangeRateClient: result={Result}", result);
                    return rates;
                }

                var ratesElement = json.RootElement.GetProperty("rates");

                foreach (var currency in TargetCurrencies)
                {
                    if (!ratesElement.TryGetProperty(currency, out var rateValue))
                    {
                        _logger.LogWarning(
                            "ExchangeRateClient: No rate for {Currency}", currency);
                        continue;
                    }

                    var rate = rateValue.GetDecimal();

                    rates.Add(CurrencyRate.Create(
                        baseCurrency: "USD",
                        quoteCurrency: currency,
                        rate: rate,
                        source: "ExchangeRate-API"));

                    _logger.LogInformation(
                        "ExchangeRateClient: ✅ USD/{Currency} = {Rate}",
                        currency, rate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ExchangeRateClient: ❌ {Message}", ex.Message);
            }

            return rates;
        }
    }
}
