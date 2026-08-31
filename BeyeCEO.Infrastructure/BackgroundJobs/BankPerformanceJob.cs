using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.BackgroundJobs
{
    public class BankPerformanceJob
    {
        private readonly IMarketDataRepository _repo;
        private readonly BeyePerformanceClient _beye;
        private readonly ILogger<BankPerformanceJob> _logger;

        public BankPerformanceJob(
            IMarketDataRepository repo,
            BeyePerformanceClient beye,
            ILogger<BankPerformanceJob> logger)
        {
            _repo = repo;
            _beye = beye;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== BankPerformanceJob STARTED === {Time}", DateTime.UtcNow);

            var banks = await _repo.GetBeyeEnabledBanksAsync();
            int success = 0, failed = 0;

            foreach (var bank in banks)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(bank.BeyeApiUrl))
                    {
                        _logger.LogWarning(
                            "⚠️ {Bank}: HasBeyeSystem=true but BeyeApiUrl is empty — skipping",
                            bank.NameEN);
                        failed++;
                        continue;
                    }

                    var results = await _beye.FetchBSAsync(
                        bank.Id, bank.BeyeApiUrl, bank.BeyeApiKey ?? string.Empty);

                    if (results.Count > 0)
                    {
                        await _repo.SaveBankPerformanceAsync(results, bank.Id, "BS");
                        success++;

                        _logger.LogInformation(
                            "✅ {Bank}: {Count} B/S metrics saved",
                            bank.NameEN, results.Count);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ {Bank}: 0 B/S metrics returned", bank.NameEN);
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "❌ {Bank}: {Message}", bank.NameEN, ex.Message);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "=== BankPerformanceJob COMPLETED === " +
                "{Success} success, {Failed} failed, {Duration}ms",
                success, failed, stopwatch.ElapsedMilliseconds);
        }
    }
}
