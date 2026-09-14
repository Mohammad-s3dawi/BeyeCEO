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
                if (string.IsNullOrWhiteSpace(bank.BeyeApiUrl))
                {
                    _logger.LogWarning(
                        "⚠️ {Bank}: HasBeyeSystem=true but BeyeApiUrl is empty — skipping",
                        bank.NameEN);
                    failed++;
                    continue;
                }

                var views = await _repo.GetBeyeViewsForBankAsync(bank.Id);

                if (views.Count == 0)
                {
                    _logger.LogWarning(
                        "⚠️ {Bank}: no active BeyeViews configured — skipping", bank.NameEN);
                    failed++;
                    continue;
                }

                foreach (var view in views)
                {
                    try
                    {
                        var raw = await _beye.FetchViewDataAsync(
                            bank.BeyeApiUrl, view.ViewId, bank.BeyeApiKey ?? string.Empty);

                        if (raw.Count == 0)
                        {
                            _logger.LogWarning(
                                "⚠️ {Bank} / {Section} (viewId {ViewId}): 0 KPIs returned",
                                bank.NameEN, view.Section, view.ViewId);
                            failed++;
                            continue;
                        }

                        var metrics = _beye.MapToMetrics(raw, bank.Id, view.Section);

                        if (metrics.Count == 0)
                        {
                            _logger.LogWarning(
                                "⚠️ {Bank} / {Section} (viewId {ViewId}): 0 KPIs mapped from {Raw} raw items",
                                bank.NameEN, view.Section, view.ViewId, raw.Count);
                            failed++;
                            continue;
                        }

                        await _repo.SaveBankPerformanceAsync(metrics, bank.Id, view.Section);
                        success++;

                        _logger.LogInformation(
                            "✅ {Bank} / {Section} (viewId {ViewId}): {Count} metrics saved",
                            bank.NameEN, view.Section, view.ViewId, metrics.Count);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger.LogError(ex,
                            "❌ {Bank} / {Section} (viewId {ViewId}): {Message}",
                            bank.NameEN, view.Section, view.ViewId, ex.Message);
                    }
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
