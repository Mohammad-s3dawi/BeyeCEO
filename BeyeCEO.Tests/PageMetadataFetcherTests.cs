using BeyeCEO.Infrastructure.NewsAggregation.ImageFallback;
using BeyeCEO.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http;
using System.Text;

namespace BeyeCEO.Tests
{
    public class PageMetadataFetcherTests
    {
        private static PageMetadataFetcher MakeFetcher(
            FakeHttpMessageHandler handler,
            IImageFetchFailureCache? failureCache = null,
            IDomainRateLimiter? rateLimiter = null)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            return new PageMetadataFetcher(
                new FakeHttpClientFactory(handler),
                new RobotsTxtCache(new FakeHttpClientFactory(handler), NullLogger<RobotsTxtCache>.Instance),
                rateLimiter ?? new DomainRateLimiter(),
                failureCache ?? new ImageFetchFailureCache(),
                config,
                NullLogger<PageMetadataFetcher>.Instance);
        }

        private static HttpResponseMessage Html(string body, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "text/html") };

        [Fact]
        public async Task TryGetImageUrlAsync_FindsOgImage_OnAllowedPage()
        {
            var handler = new FakeHttpMessageHandler
            {
                Responder = req => req.RequestUri!.AbsolutePath == "/robots.txt"
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : Html("""<meta property="og:image" content="https://example.com/photo.jpg">""")
            };
            var fetcher = MakeFetcher(handler);

            var result = await fetcher.TryGetImageUrlAsync("https://example.com/news/a1", 131072);

            Assert.Equal(PageImageFetchStatus.Found, result.Status);
            Assert.Equal("https://example.com/photo.jpg", result.ImageUrl);
        }

        [Fact]
        public async Task TryGetImageUrlAsync_RobotsDisallow_NeverFetchesThePage()
        {
            var handler = new FakeHttpMessageHandler
            {
                Responder = req => req.RequestUri!.AbsolutePath == "/robots.txt"
                    ? Html("User-agent: *\nDisallow: /news/", HttpStatusCode.OK)
                    : Html("""<meta property="og:image" content="https://example.com/should-not-be-fetched.jpg">""")
            };
            var fetcher = MakeFetcher(handler);

            var result = await fetcher.TryGetImageUrlAsync("https://example.com/news/a1", 131072);

            Assert.Equal(PageImageFetchStatus.RobotsDisallowed, result.Status);
            Assert.Null(result.ImageUrl);
            // Only robots.txt was fetched — the disallowed page itself never was.
            Assert.Single(handler.Requests);
            Assert.Equal("/robots.txt", handler.Requests[0].RequestUri!.AbsolutePath);
        }

        [Fact]
        public async Task TryGetImageUrlAsync_403_StopsDomainForRestOfRun()
        {
            var handler = new FakeHttpMessageHandler
            {
                Responder = req => req.RequestUri!.AbsolutePath == "/robots.txt"
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : new HttpResponseMessage(HttpStatusCode.Forbidden)
            };
            var fetcher = MakeFetcher(handler);

            var first = await fetcher.TryGetImageUrlAsync("https://example.com/news/a1", 131072);
            Assert.Equal(PageImageFetchStatus.DomainStoppedAfterError, first.Status);

            var requestCountAfterFirst = handler.Requests.Count;

            // A different page, same domain, same run — must short-circuit
            // without making a new HTTP request.
            var second = await fetcher.TryGetImageUrlAsync("https://example.com/news/a2", 131072);

            Assert.Equal(PageImageFetchStatus.DomainStoppedAfterError, second.Status);
            Assert.Equal(requestCountAfterFirst, handler.Requests.Count);
        }

        [Fact]
        public async Task TryGetImageUrlAsync_RecentFailure_IsCachedAndSkipsRefetch()
        {
            var handler = new FakeHttpMessageHandler
            {
                Responder = req => req.RequestUri!.AbsolutePath == "/robots.txt"
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : new HttpResponseMessage(HttpStatusCode.InternalServerError)
            };
            var sharedFailureCache = new ImageFetchFailureCache();

            var fetcher1 = MakeFetcher(handler, failureCache: sharedFailureCache);
            var first = await fetcher1.TryGetImageUrlAsync("https://example.com/news/a1", 131072);
            Assert.Equal(PageImageFetchStatus.FetchError, first.Status);

            var requestCountAfterFirst = handler.Requests.Count;

            // A fresh PageMetadataFetcher instance (simulating a later run)
            // sharing the same singleton failure cache must skip re-fetching.
            var fetcher2 = MakeFetcher(handler, failureCache: sharedFailureCache);
            var second = await fetcher2.TryGetImageUrlAsync("https://example.com/news/a1", 131072);

            Assert.Equal(PageImageFetchStatus.RecentFailureCached, second.Status);
            Assert.Equal(requestCountAfterFirst, handler.Requests.Count);
        }

        [Fact]
        public async Task TryGetImageUrlAsync_ByteBudget_StopsReadingWithoutConsumingWholeBody()
        {
            // No image anywhere in a body much larger than the budget.
            var filler = new string('x', 2_000_000);
            var bodyBytes = Encoding.UTF8.GetBytes($"<html><head></head><body>{filler}</body></html>");

            CountingStreamContent? content = null;
            var handler = new FakeHttpMessageHandler
            {
                Responder = req =>
                {
                    if (req.RequestUri!.AbsolutePath == "/robots.txt")
                        return new HttpResponseMessage(HttpStatusCode.NotFound);

                    content = new CountingStreamContent(bodyBytes);
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
                }
            };
            var fetcher = MakeFetcher(handler);

            const int maxBytes = 2048;
            var result = await fetcher.TryGetImageUrlAsync("https://example.com/news/a1", maxBytes);

            Assert.Equal(PageImageFetchStatus.NotFound, result.Status);
            Assert.NotNull(content);

            // The full 2MB body must never have been fully read — only
            // modestly more than the byte budget (one buffer's worth of
            // slack from the read loop), proving the stream was abandoned
            // early rather than consumed to completion.
            Assert.True(content!.Stream.TotalBytesRead < maxBytes + 16384,
                $"expected an early stop near {maxBytes} bytes, but {content.Stream.TotalBytesRead} were read");
        }

        [Fact]
        public async Task TryGetImageUrlAsync_FindsImageEarly_StopsReadingRestOfBody()
        {
            var filler = new string('x', 2_000_000);
            var body = $"""<meta property="og:image" content="https://example.com/early.jpg">""" + filler;
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            CountingStreamContent? content = null;
            var handler = new FakeHttpMessageHandler
            {
                Responder = req =>
                {
                    if (req.RequestUri!.AbsolutePath == "/robots.txt")
                        return new HttpResponseMessage(HttpStatusCode.NotFound);

                    content = new CountingStreamContent(bodyBytes);
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
                }
            };
            var fetcher = MakeFetcher(handler);

            var result = await fetcher.TryGetImageUrlAsync("https://example.com/news/a1", 131072);

            Assert.Equal(PageImageFetchStatus.Found, result.Status);
            Assert.Equal("https://example.com/early.jpg", result.ImageUrl);
            Assert.NotNull(content);
            // Found within the first chunk — nowhere near the full 2MB body.
            Assert.True(content!.Stream.TotalBytesRead < 50_000,
                $"expected an early stop, but {content.Stream.TotalBytesRead} bytes were read");
        }
    }
}
