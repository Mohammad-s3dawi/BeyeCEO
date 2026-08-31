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

    public class GlobalMarketsJob
    {
        private readonly IMarketDataRepository _repo;
        private readonly AlphaVantageClient _alphaVantage;
        private readonly ILogger<GlobalMarketsJob> _logger;

        // Indices — عالمية ثابتة
        private static readonly List<(string Symbol, string Name, string Region)> Indices =
        [
            ("SPY",  "S&P 500",        "US"),
            ("QQQ",  "Nasdaq 100",     "US"),
            ("DIA",  "Dow Jones",      "US"),
            ("IWM",  "Russell 2000",   "US"),
            ("EWG",  "DAX Germany",    "EU"),
            ("EWU",  "FTSE 100",       "EU"),
            ("EWQ",  "CAC 40 France",  "EU"),
            ("EWI",  "FTSE MIB Italy", "EU"),
            ("EWP",  "IBEX 35 Spain",  "EU"),
            ("EWJ",  "Nikkei Japan",   "ASIA"),
            ("FXI",  "Hang Seng",      "ASIA"),
            ("EWY",  "KOSPI Korea",    "ASIA"),
            ("EWA",  "ASX Australia",  "ASIA"),
            ("MCHI", "CSI 300 China",  "CHINA"),
            ("KWEB", "China Tech",     "CHINA"),
            ("KSA",  "Saudi Tadawul",  "MENA"),
        ];

        public GlobalMarketsJob(
            IMarketDataRepository repo,
            AlphaVantageClient alphaVantage,
            ILogger<GlobalMarketsJob> logger)
        {
            _repo = repo;
            _alphaVantage = alphaVantage;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== GlobalMarketsJob STARTED === {Time}",
                DateTime.UtcNow);

            try
            {
                await FetchIndicesAsync();

                stopwatch.Stop();
                _logger.LogInformation(
                    "=== GlobalMarketsJob COMPLETED === {Duration}ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "=== GlobalMarketsJob FAILED === {Message}",
                    ex.Message);
                throw;
            }
        }

        private async Task FetchIndicesAsync()
        {
            _logger.LogInformation(
                "--- Fetching {Count} indices ---", Indices.Count);

            int success = 0, failed = 0;

            foreach (var (symbol, name, region) in Indices)
            {
                try
                {
                    var quote = await _alphaVantage
                        .GetGlobalQuoteAsync(symbol);

                    if (quote == null)
                    {
                        _logger.LogWarning(
                            "No data for {Symbol}", symbol);
                        failed++;
                        continue;
                    }

                    var index = GlobalIndex.Create(
                        symbol, name, quote.Price,
                        quote.Change, quote.ChangePct,
                        region, "AlphaVantage", quote.Volume);

                    await _repo.SaveGlobalIndexAsync(index);
                    success++;

                    _logger.LogInformation(
                        "✅ {Symbol} = {Price} ({ChangePct}%)",
                        symbol, quote.Price, quote.ChangePct);

                    await Task.Delay(TimeSpan.FromSeconds(12));
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "❌ {Symbol}: {Message}", symbol, ex.Message);
                }
            }

            _logger.LogInformation(
                "--- Indices: {Success} ✅ {Failed} ❌ ---",
                success, failed);
        }

    }
}
