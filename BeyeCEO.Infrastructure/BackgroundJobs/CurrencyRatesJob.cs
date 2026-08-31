using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.BackgroundJobs
{
    public class CurrencyRatesJob
    {
        private readonly IMarketDataRepository _repo;
        private readonly ExchangeRateClient _exchangeRate;
        private readonly ILogger<CurrencyRatesJob> _logger;

        public CurrencyRatesJob(
            IMarketDataRepository repo,
            ExchangeRateClient exchangeRate,
            ILogger<CurrencyRatesJob> logger)
        {
            _repo = repo;
            _exchangeRate = exchangeRate;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== CurrencyRatesJob STARTED === {Time}", DateTime.UtcNow);

            try
            {
                var rates = await _exchangeRate.FetchRatesAsync();

                if (rates.Count > 0)
                {
                    await _repo.SaveCurrencyRatesRangeAsync(rates);
                    _logger.LogInformation(
                        "✅ CurrencyRates: {Count} saved", rates.Count);
                }
                else
                {
                    _logger.LogWarning("CurrencyRatesJob: No rates fetched");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ CurrencyRatesJob: {Message}", ex.Message);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "=== CurrencyRatesJob COMPLETED === {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
