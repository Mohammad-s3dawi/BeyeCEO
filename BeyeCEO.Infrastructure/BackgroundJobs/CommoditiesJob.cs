using BeyeCEO.Domain.MarketData.Entities;
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
    public class CommoditiesJob
    {
        private readonly IMarketDataRepository _repo;
        private readonly EiaClient _eia;
        private readonly AlphaVantageClient _alphaVantage;
        private readonly ILogger<CommoditiesJob> _logger;

        public CommoditiesJob(
            IMarketDataRepository repo,
            EiaClient eia,
            AlphaVantageClient alphaVantage,
            ILogger<CommoditiesJob> logger)
        {
            _repo = repo;
            _eia = eia;
            _alphaVantage = alphaVantage;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== CommoditiesJob STARTED === {Time}",
                DateTime.UtcNow);

            try
            {
                await FetchOilAsync();
                await FetchGoldAsync();

                stopwatch.Stop();
                _logger.LogInformation(
                    "=== CommoditiesJob COMPLETED === Duration: {Duration}ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "=== CommoditiesJob FAILED === Duration: {Duration}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task FetchOilAsync()
        {
            _logger.LogInformation("--- Fetching Oil prices ---");

            try
            {
                var brent = await _eia.GetBrentCrudeAsync();
                if (brent != null)
                {
                    await _repo.SaveCommodityAsync(
                        Commodity.Create(
                            "BRENT", brent.Name, brent.Price,
                            "USD", brent.ChangePct, "EIA"));

                    _logger.LogInformation(
                        "✅ Brent = ${Price} | Change: {ChangePct}%",
                        brent.Price, brent.ChangePct);
                }
                else
                {
                    _logger.LogWarning("❌ No Brent data returned");
                }

                var wti = await _eia.GetWtiCrudeAsync();
                if (wti != null)
                {
                    await _repo.SaveCommodityAsync(
                        Commodity.Create(
                            "WTI", wti.Name, wti.Price,
                            "USD", wti.ChangePct, "EIA"));

                    _logger.LogInformation(
                        "✅ WTI = ${Price} | Change: {ChangePct}%",
                        wti.Price, wti.ChangePct);
                }
                else
                {
                    _logger.LogWarning("❌ No WTI data returned");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Failed to fetch oil: {Message}", ex.Message);
            }
        }

        private async Task FetchGoldAsync()
        {
            _logger.LogInformation("--- Fetching Gold price ---");

            try
            {
                var gold = await _alphaVantage
                    .GetGlobalQuoteAsync("GLD");

                if (gold != null)
                {
                    await _repo.SaveCommodityAsync(
                        Commodity.Create(
                            "GOLD", "Gold", gold.Price,
                            "USD", gold.ChangePct, "AlphaVantage"));

                    _logger.LogInformation(
                        "✅ Gold = ${Price} | Change: {ChangePct}%",
                        gold.Price, gold.ChangePct);
                }
                else
                {
                    _logger.LogWarning("❌ No Gold data returned");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Failed to fetch gold: {Message}", ex.Message);
            }
        }
    }
}
