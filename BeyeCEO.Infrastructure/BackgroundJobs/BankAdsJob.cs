using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.BackgroundJobs
{
    public class BankAdsJob
    {
        private readonly IMarketDataRepository _repo;
        private readonly BankAdsScraper _scraper;
        private readonly ILogger<BankAdsJob> _logger;

        public BankAdsJob(
            IMarketDataRepository repo,
            BankAdsScraper scraper,
            ILogger<BankAdsJob> logger)
        {
            _repo = repo;
            _scraper = scraper;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== BankAdsJob STARTED === {Time}", DateTime.UtcNow);

            var countries = await _repo.GetActiveCountriesAsync();
            int total = 0;

            foreach (var country in countries)
            {
                try
                {
                    var ads = await _scraper.FetchAllAsync(country.CountryCode);

                    foreach (var ad in ads)
                    {
                        await _repo.SaveBankAdAsync(ad);
                    }

                    total += ads.Count;

                    _logger.LogInformation(
                        "✅ BankAds/{Country}: {Count} processed",
                        country.CountryCode, ads.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "❌ BankAdsJob/{Country}: {Message}",
                        country.CountryCode, ex.Message);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "=== BankAdsJob COMPLETED === {Total} total, {Duration}ms",
                total, stopwatch.ElapsedMilliseconds);
        }
    }
}
