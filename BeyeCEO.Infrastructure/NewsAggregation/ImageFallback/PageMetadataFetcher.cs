using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.NewsAggregation.ImageFallback
{
    public enum PageImageFetchStatus
    {
        Found,
        NotFound,
        RobotsDisallowed,
        DomainStoppedAfterError,
        MaxFetchesPerDomainExceeded,
        RecentFailureCached,
        FetchError,
        InvalidUrl
    }

    public class PageImageResult
    {
        public PageImageFetchStatus Status { get; init; }
        public string? ImageUrl { get; init; }
    }

    // Scoped (one instance per pipeline run) — the per-run guards
    // (domain-stop-after-403/429, max-fetches-per-domain) are instance
    // state so they reset every run, while robots.txt caching, rate
    // limiting and failure caching are shared singletons injected in.
    public class PageMetadataFetcher : IPageMetadataFetcher
    {
        private const string OurUserAgentToken = "BeyeCEO-NewsReader";

        private readonly IHttpClientFactory _httpFactory;
        private readonly IRobotsTxtCache _robots;
        private readonly IDomainRateLimiter _rateLimiter;
        private readonly IImageFetchFailureCache _failureCache;
        private readonly IConfiguration _config;
        private readonly ILogger<PageMetadataFetcher> _logger;

        private readonly HashSet<string> _stoppedDomainsThisRun = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _fetchCountThisRun = new(StringComparer.OrdinalIgnoreCase);

        public PageMetadataFetcher(
            IHttpClientFactory httpFactory,
            IRobotsTxtCache robots,
            IDomainRateLimiter rateLimiter,
            IImageFetchFailureCache failureCache,
            IConfiguration config,
            ILogger<PageMetadataFetcher> logger)
        {
            _httpFactory = httpFactory;
            _robots = robots;
            _rateLimiter = rateLimiter;
            _failureCache = failureCache;
            _config = config;
            _logger = logger;
        }

        public async Task<PageImageResult> TryGetImageUrlAsync(string pageUrl, int maxBytes, CancellationToken ct = default)
        {
            if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri) ||
                !string.Equals(pageUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return new PageImageResult { Status = PageImageFetchStatus.InvalidUrl };
            }

            var domain = pageUri.Host;

            if (_stoppedDomainsThisRun.Contains(domain))
                return new PageImageResult { Status = PageImageFetchStatus.DomainStoppedAfterError };

            if (_failureCache.HasRecentFailure(pageUrl))
                return new PageImageResult { Status = PageImageFetchStatus.RecentFailureCached };

            var maxPerDomain = _config.GetValue("News:ImageFallback:MaxFetchesPerDomainPerRun", 5);
            _fetchCountThisRun.TryGetValue(domain, out var countSoFar);
            if (countSoFar >= maxPerDomain)
                return new PageImageResult { Status = PageImageFetchStatus.MaxFetchesPerDomainExceeded };

            var origin = $"{pageUri.Scheme}://{pageUri.Host}";
            var rules = await _robots.GetRulesAsync(origin, ct);
            var path = pageUri.PathAndQuery;

            if (!rules.IsAllowed(path, OurUserAgentToken) || !rules.IsAllowed(path, "*"))
                return new PageImageResult { Status = PageImageFetchStatus.RobotsDisallowed };

            var crawlDelay = rules.GetCrawlDelaySeconds(OurUserAgentToken) ?? rules.GetCrawlDelaySeconds("*");
            await _rateLimiter.WaitForSlotAsync(domain, crawlDelay, ct);

            _fetchCountThisRun[domain] = countSoFar + 1;

            try
            {
                var client = _httpFactory.CreateClient("NewsImageFallback");
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

                using var response = await client.GetAsync(pageUri, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

                if (response.StatusCode == HttpStatusCode.Forbidden || (int)response.StatusCode == 429)
                {
                    _stoppedDomainsThisRun.Add(domain);
                    _failureCache.RecordFailure(pageUrl);
                    _logger.LogInformation(
                        "PageMetadataFetcher: {Domain} returned {Status} — stopping this domain for the rest of the run",
                        domain, (int)response.StatusCode);
                    return new PageImageResult { Status = PageImageFetchStatus.DomainStoppedAfterError };
                }

                if (!response.IsSuccessStatusCode)
                {
                    _failureCache.RecordFailure(pageUrl);
                    return new PageImageResult { Status = PageImageFetchStatus.FetchError };
                }

                var html = await ReadUpToAsync(response, maxBytes, timeoutCts.Token);

                var raw = ImageUrlExtractor.ExtractCandidate(html);
                var normalized = ImageUrlExtractor.Normalize(raw, pageUri);

                if (normalized == null || ImageUrlExtractor.LooksLikeNonPhoto(normalized))
                {
                    _failureCache.RecordFailure(pageUrl);
                    return new PageImageResult { Status = PageImageFetchStatus.NotFound };
                }

                if (!string.Equals(normalized.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    var upgraded = await TryUpgradeToHttpsAsync(normalized, client, ct);
                    if (upgraded == null)
                    {
                        _failureCache.RecordFailure(pageUrl);
                        return new PageImageResult { Status = PageImageFetchStatus.NotFound };
                    }
                    normalized = upgraded;
                }

                return new PageImageResult { Status = PageImageFetchStatus.Found, ImageUrl = normalized.ToString() };
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "PageMetadataFetcher: fetch failed for {Url}", pageUrl);
                _failureCache.RecordFailure(pageUrl);
                return new PageImageResult { Status = PageImageFetchStatus.FetchError };
            }
        }

        // Reads the body incrementally, stopping as soon as an image meta
        // tag is recognizable or the byte budget is exhausted — whichever
        // comes first.
        private static async Task<string> ReadUpToAsync(HttpResponseMessage response, int maxBytes, CancellationToken ct)
        {
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var buffer = new byte[8192];
            using var ms = new MemoryStream();
            int read;

            while (ms.Length < maxBytes && (read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                ms.Write(buffer, 0, read);

                var textSoFar = Encoding.UTF8.GetString(ms.ToArray());
                if (ImageUrlExtractor.ExtractCandidate(textSoFar) != null) break;
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static async Task<Uri?> TryUpgradeToHttpsAsync(Uri httpUri, HttpClient client, CancellationToken ct)
        {
            try
            {
                var httpsUri = new UriBuilder(httpUri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri;
                using var request = new HttpRequestMessage(HttpMethod.Head, httpsUri);
                using var response = await client.SendAsync(request, ct);
                return response.IsSuccessStatusCode ? httpsUri : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
