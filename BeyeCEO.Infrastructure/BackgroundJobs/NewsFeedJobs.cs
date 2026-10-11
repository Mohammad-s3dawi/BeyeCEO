using BeyeCEO.Infrastructure.NewsAggregation;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.BackgroundJobs
{
    // Replaces the old single "news" job (Guardian + NewsApi, NewsApi now
    // disabled). Local RSS feeds and international feeds poll on different
    // schedules since local JO outlets publish far more frequently than
    // central-bank/regulator feeds.
    public class NewsFeedJobs
    {
        private readonly NewsPipelineRunner _runner;
        private readonly ILogger<NewsFeedJobs> _logger;

        public NewsFeedJobs(NewsPipelineRunner runner, ILogger<NewsFeedJobs> logger)
        {
            _runner = runner;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        [AutomaticRetry(Attempts = 2)]
        public async Task RunLocalRssAsync()
        {
            var result = await _runner.RunLocalRssAsync(store: true);
            _logger.LogInformation(
                "news-local-rss: fetched={Fetched} tooOld={TooOld} rejected={Rejected} passed={Passed} " +
                "alreadyInDb={AlreadyInDb} stored={Stored}",
                result.TotalFetched, result.TotalTooOld, result.TotalRejected, result.TotalPassed,
                result.TotalAlreadyInDb, result.TotalWouldStoreOrStored);
        }

        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        [AutomaticRetry(Attempts = 2)]
        public async Task RunInternationalAsync()
        {
            var result = await _runner.RunInternationalAsync(store: true);
            _logger.LogInformation(
                "news-international: fetched={Fetched} tooOld={TooOld} rejected={Rejected} passed={Passed} " +
                "alreadyInDb={AlreadyInDb} stored={Stored}",
                result.TotalFetched, result.TotalTooOld, result.TotalRejected, result.TotalPassed,
                result.TotalAlreadyInDb, result.TotalWouldStoreOrStored);
        }
    }
}
