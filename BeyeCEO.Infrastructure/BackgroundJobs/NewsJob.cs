using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Domain.News.Interfaces;
using BeyeCEO.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.BackgroundJobs
{
    public class NewsJob
    {
        private readonly INewsRepository _newsRepo;
        private readonly IMarketDataRepository _marketRepo;
        private readonly GuardianClient _guardian;
        private readonly NewsApiClient _newsApi;
        private readonly RssNewsClient _rssNews;
        private readonly ILogger<NewsJob> _logger;

        public NewsJob(
            INewsRepository newsRepo,
            IMarketDataRepository marketRepo,
            GuardianClient guardian,
            NewsApiClient newsApi,
            RssNewsClient rssNews,
            ILogger<NewsJob> logger)
        {
            _newsRepo = newsRepo;
            _marketRepo = marketRepo;
            _guardian = guardian;
            _newsApi = newsApi;
            _rssNews = rssNews;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "=== NewsJob STARTED === {Time}", DateTime.UtcNow);

            await FetchInternationalNewsAsync();
            await FetchLocalNewsAsync();

            stopwatch.Stop();
            _logger.LogInformation(
                "=== NewsJob COMPLETED === {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }

        private async Task FetchInternationalNewsAsync()
        {
            _logger.LogInformation(
                "--- Fetching International News ---");

            try
            {
                var articles = await _guardian
                    .FetchInternationalNewsAsync(pageSize: 5);

                if (articles.Any())
                {
                    await _newsRepo.SaveRangeAsync(articles);
                    _logger.LogInformation(
                        "✅ International: {Count} articles saved",
                        articles.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ International News: {Message}", ex.Message);
            }
        }

        private async Task FetchLocalNewsAsync()
        {
            _logger.LogInformation(
                "--- Fetching Local News ---");

            var countries = await _marketRepo.GetActiveCountriesAsync();

            foreach (var country in countries)
            {
                try
                {
                    var articles = await _newsApi
                        .FetchLocalNewsAsync(country.CountryCode, pageSize: 5);

                    if (articles.Count == 0 && _rssNews.HasFeeds(country.CountryCode))
                    {
                        _logger.LogInformation(
                            "NewsApi: 0 results for {CountryCode}, trying RSS",
                            country.CountryCode);

                        articles = await _rssNews
                            .FetchLocalNewsAsync(country.CountryCode);
                    }

                    if (articles.Any())
                    {
                        await _newsRepo.SaveRangeAsync(articles);
                        _logger.LogInformation(
                            "✅ {CountryCode}: {Count} articles saved",
                            country.CountryCode, articles.Count);
                    }

                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "❌ {CountryCode}: {Message}",
                        country.CountryCode, ex.Message);
                }
            }
        }
    }
}
