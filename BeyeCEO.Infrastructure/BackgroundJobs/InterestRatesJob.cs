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
    public class InterestRatesJob
    {
        private readonly IMarketDataRepository _repo;
        private readonly FredClient _fred;
        private readonly ILogger<InterestRatesJob> _logger;

        // FRED Series — قابل للتوسعة
        private static readonly Dictionary<string, List<(string SeriesId, string RateType)>>
            CountrySeriesMap = new()
            {
                ["US"] =
                [
                    ("FEDFUNDS", "Fed Funds Rate"),
                ("DFEDTARU", "Fed Target Upper"),
                ("DFEDTARL", "Fed Target Lower"),
                ],
                ["GB"] =
                [
                    ("BOEBPOLICYR", "BOE Policy Rate"),
                ]
            };

        public InterestRatesJob(
            IMarketDataRepository repo,
            FredClient fred,
            ILogger<InterestRatesJob> logger)
        {
            _repo = repo;
            _fred = fred;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== InterestRatesJob STARTED === {Time}",
                DateTime.UtcNow);

            // 1. جيب كل الدول النشطة من DB
            var countries = await _repo.GetActiveCountriesAsync();
            int success = 0, failed = 0, skipped = 0;

            foreach (var country in countries)
            {
                // 2. تحقق لو عندنا Series لهاد البلد
                if (!CountrySeriesMap.TryGetValue(
                    country.CountryCode, out var seriesList))
                {
                    _logger.LogDebug(
                        "No FRED series for {CountryCode} — skipping",
                        country.CountryCode);
                    skipped++;
                    continue;
                }

                _logger.LogInformation(
                    "Fetching rates for {CountryCode} ({CentralBank})",
                    country.CountryCode, country.CentralBank);

                // 3. جيب كل الـ Series للبلد
                foreach (var (seriesId, rateType) in seriesList)
                {
                    try
                    {
                        var obs = await _fred.GetLatestAsync(seriesId);
                        if (obs == null)
                        {
                            _logger.LogWarning(
                                "No data for {SeriesId}", seriesId);
                            failed++;
                            continue;
                        }

                        var rate = InterestRate.Create(
                            country.CentralBank,
                            country.CountryCode,
                            rateType,
                            obs.Value,
                            obs.Date);

                        await _repo.SaveInterestRateAsync(rate);
                        success++;

                        _logger.LogInformation(
                            "✅ {CountryCode} {RateType} = {Value}%",
                            country.CountryCode, rateType, obs.Value);

                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger.LogError(ex,
                            "❌ Failed {SeriesId}: {Message}",
                            seriesId, ex.Message);
                    }
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "=== InterestRatesJob COMPLETED === " +
                "{Success} success, {Failed} failed, " +
                "{Skipped} skipped, {Duration}ms",
                success, failed, skipped,
                stopwatch.ElapsedMilliseconds);
        }

        // Method لإضافة دولة جديدة بسهولة لاحقاً
        public static void AddCountrySeries(
            string countryCode,
            List<(string SeriesId, string RateType)> series)
        {
            CountrySeriesMap[countryCode] = series;
        }
    }
}
