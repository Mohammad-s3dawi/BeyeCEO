using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.BackgroundJobs
{


    public class LocalStockExchangeJob
    {
        private readonly IMarketDataRepository _repo;
        private readonly ASEClient _aseClient;
        private readonly ILogger<LocalStockExchangeJob> _logger;

        public LocalStockExchangeJob(
            IMarketDataRepository repo,
            ASEClient aseClient,
            ILogger<LocalStockExchangeJob> logger)
        {
            _repo = repo;
            _aseClient = aseClient;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== LocalStockExchangeJob STARTED === {Time}",
                DateTime.UtcNow);

            var countries = await _repo.GetActiveCountriesAsync();
            var stockCountries = countries
                .Where(c => c.HasLocalData &&
                            c.StockScraperType != "NONE")
                .ToList();

            foreach (var country in stockCountries)
            {
                try
                {
                    switch (country.StockScraperType)
                    {
                        case "ASE":
                            await ProcessASEAsync();
                            break;
                        default:
                            _logger.LogWarning(
                                "No client for {Type}",
                                country.StockScraperType);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "❌ {CountryCode}: {Message}",
                        country.CountryCode, ex.Message);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "=== LocalStockExchangeJob COMPLETED === {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }

        private async Task ProcessASEAsync()
        {
            var data = await _aseClient.FetchMarketDataAsync();
            if (data != null)
            {
                await _repo.SaveStockExchangeDataAsync(data);
                _logger.LogInformation(
                    "✅ ASE data saved");
            }

            var movers = await _aseClient.FetchTopMoversAsync();
            if (movers.Any())
            {
                await _repo.SaveTopMoversAsync(movers);
                _logger.LogInformation(
                    "✅ {Count} movers saved", movers.Count);
            }
        }
    }
}
