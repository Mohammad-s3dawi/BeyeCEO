using BeyeCEO.Infrastructure.NewsAggregation.ImageFallback;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Tests.Fakes
{
    // Hand-rolled fake — returns a canned image URL for every page (or a
    // per-URL override), so NewsPipelineRunner tests can exercise the
    // integration (site-default safeguard, counting, never-blocks-on-
    // failure) without any real HTTP.
    public class FakePageMetadataFetcher : IPageMetadataFetcher
    {
        public string? DefaultImageUrl { get; set; }
        public Dictionary<string, string?> ImageUrlByPage { get; } = new();
        public int CallCount { get; private set; }

        public Task<PageImageResult> TryGetImageUrlAsync(string pageUrl, int maxBytes, CancellationToken ct = default)
        {
            CallCount++;

            var url = ImageUrlByPage.TryGetValue(pageUrl, out var specific) ? specific : DefaultImageUrl;

            return Task.FromResult(url == null
                ? new PageImageResult { Status = PageImageFetchStatus.NotFound }
                : new PageImageResult { Status = PageImageFetchStatus.Found, ImageUrl = url });
        }
    }
}
