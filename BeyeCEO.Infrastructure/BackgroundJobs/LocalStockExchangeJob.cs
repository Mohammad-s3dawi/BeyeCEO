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
        private readonly ASEScraper _aseScraper;
        private readonly ILogger<LocalStockExchangeJob> _logger;

        public LocalStockExchangeJob(
            IMarketDataRepository repo,
            ASEScraper aseScraper,
            ILogger<LocalStockExchangeJob> logger)
        {
            _repo = repo;
            _aseScraper = aseScraper;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== LocalStockExchangeJob STARTED === {Time}",
                DateTime.UtcNow);

            // جيب الدول النشطة اللي عندها Stock Scraper
            var countries = await _repo.GetActiveCountriesAsync();
            var stockCountries = countries
                .Where(c => c.HasLocalData &&
                            c.StockScraperType != "NONE")
                .ToList();

            _logger.LogInformation(
                "Found {Count} countries with stock scrapers",
                stockCountries.Count);

            foreach (var country in stockCountries)
            {
                try
                {
                    _logger.LogInformation(
                        "Processing {CountryCode} — {Exchange}",
                        country.CountryCode,
                        country.StockScraperType);

                    switch (country.StockScraperType)
                    {
                        case "ASE":
                            await ProcessASEAsync(country.CountryCode);
                            break;
                        default:
                            _logger.LogWarning(
                                "No scraper for {Type}",
                                country.StockScraperType);
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
                "=== LocalStockExchangeJob COMPLETED === {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }

        private async Task ProcessASEAsync(string countryCode)
        {
            // جيب بيانات البورصة
            var data = await _aseScraper.FetchAsync();
            if (data != null)
            {
                await _repo.SaveStockExchangeDataAsync(data);
                _logger.LogInformation(
                    "✅ ASE data saved for {Date}",
                    data.TradeDate);
            }

            // جيب Top Movers
            var movers = await _aseScraper.FetchTopMoversAsync();
            if (movers.Any())
            {
                await _repo.SaveTopMoversAsync(movers);
                _logger.LogInformation(
                    "✅ {Count} top movers saved", movers.Count);
            }
        }
    }
}
