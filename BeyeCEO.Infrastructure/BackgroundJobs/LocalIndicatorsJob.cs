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

    public class LocalIndicatorsJob
    {
        private readonly IMarketDataRepository _repo;
        private readonly CBJScraper _cbjScraper;
        private readonly ILogger<LocalIndicatorsJob> _logger;

        public LocalIndicatorsJob(
            IMarketDataRepository repo,
            CBJScraper cbjScraper,
            ILogger<LocalIndicatorsJob> logger)
        {
            _repo = repo;
            _cbjScraper = cbjScraper;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== LocalIndicatorsJob STARTED === {Time}",
                DateTime.UtcNow);

            var countries = await _repo.GetActiveCountriesAsync();
            var localCountries = countries
                .Where(c => c.HasLocalData &&
                            c.CentralBankScraperType != "NONE")
                .ToList();

            _logger.LogInformation(
                "Found {Count} countries with CB scrapers",
                localCountries.Count);

            foreach (var country in localCountries)
            {
                try
                {
                    switch (country.CentralBankScraperType)
                    {
                        case "CBJ":
                            await ProcessCBJAsync();
                            break;
                        default:
                            _logger.LogWarning(
                                "No scraper for {Type}",
                                country.CentralBankScraperType);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "❌ Failed for {CountryCode}: {Message}",
                        country.CountryCode, ex.Message);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "=== LocalIndicatorsJob COMPLETED === {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }

        private async Task ProcessCBJAsync()
        {
            var indicators = await _cbjScraper.FetchAsync();

            foreach (var indicator in indicators)
            {
                await _repo.SaveLocalIndicatorAsync(indicator);
            }

            _logger.LogInformation(
                "✅ CBJ: {Count} indicators saved",
                indicators.Count);
        }
    }
}
