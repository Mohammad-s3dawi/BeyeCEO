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

            try
            {
                var ads = await _scraper.FetchAllAsync();

                foreach (var ad in ads)
                {
                    await _repo.SaveBankAdAsync(ad);
                }

                _logger.LogInformation(
                    "✅ BankAds: {Count} processed", ads.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ BankAdsJob: {Message}", ex.Message);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "=== BankAdsJob COMPLETED === {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
