using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.NewsAggregation.ImageFallback
{
    // Polite og:image lookup for a single article page — robots.txt
    // aware, rate-limited, bounded by timeout/redirects/byte budget. Never
    // throws; failures are reported via PageImageResult.Status.
    public interface IPageMetadataFetcher
    {
        Task<PageImageResult> TryGetImageUrlAsync(string pageUrl, int maxBytes, CancellationToken ct = default);
    }
}
