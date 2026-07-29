using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.BackgroundJobs
{
    public class CbjCircularsJob
    {
        private readonly IMarketDataRepository _repo;
        private readonly CBJCircularsScraper _scraper;
        private readonly ILogger<CbjCircularsJob> _logger;

        public CbjCircularsJob(
            IMarketDataRepository repo,
            CBJCircularsScraper scraper,
            ILogger<CbjCircularsJob> logger)
        {
            _repo = repo;
            _scraper = scraper;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== CbjCircularsJob STARTED === {Time}", DateTime.UtcNow);

            try
            {
                var circulars = await _scraper.FetchAsync();

                foreach (var circular in circulars)
                {
                    await _repo.SaveCircularAsync(circular);
                }

                _logger.LogInformation(
                    "✅ CBJ Circulars: {Count} saved", circulars.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ CbjCircularsJob: {Message}", ex.Message);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "=== CbjCircularsJob COMPLETED === {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
