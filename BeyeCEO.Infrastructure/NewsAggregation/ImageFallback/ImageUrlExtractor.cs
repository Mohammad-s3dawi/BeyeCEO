using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeyeCEO.Infrastructure.NewsAggregation.ImageFallback
{
    // Pure HTML-meta image extraction — no network calls. Priority order
    // (Step 11b item 5): og:image, og:image:secure_url, twitter:image,
    // <link rel="image_src">, JSON-LD "image".
    public static class ImageUrlExtractor
    {
        private static readonly Regex OgImageRegex = BuildMetaRegex("og:image");
        private static readonly Regex OgImageSecureRegex = BuildMetaRegex("og:image:secure_url");
        private static readonly Regex TwitterImageRegex = BuildMetaRegex("twitter:image");

        private static readonly Regex LinkImageSrcRegex = new(
            "<link[^>]+rel=[\"']image_src[\"'][^>]+href=[\"']([^\"']+)[\"']" +
            "|<link[^>]+href=[\"']([^\"']+)[\"'][^>]+rel=[\"']image_src[\"']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex JsonLdImageRegex = new(
            "\"image\"\\s*:\\s*\"([^\"]+)\"",
            RegexOptions.Compiled);

        private static Regex BuildMetaRegex(string key) => new(
            $"<meta[^>]+(?:property|name)=[\"']{Regex.Escape(key)}[\"'][^>]+content=[\"']([^\"']+)[\"']" +
            $"|<meta[^>]+content=[\"']([^\"']+)[\"'][^>]+(?:property|name)=[\"']{Regex.Escape(key)}[\"']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly string[] NonPhotoHints =
            ["logo", "icon", "favicon", "default", "placeholder", "sprite", "blank", "spacer"];

        private static readonly Regex TinyDimensionRegex = new(
            @"\b(1x1|16x16|32x32|48x48|64x64)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string? FirstGroup(Match m) =>
            m.Success ? (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value) : null;

        // Returns the first raw (un-normalized) candidate found, in priority order.
        public static string? ExtractCandidate(string html) =>
            FirstGroup(OgImageRegex.Match(html))
            ?? FirstGroup(OgImageSecureRegex.Match(html))
            ?? FirstGroup(TwitterImageRegex.Match(html))
            ?? FirstGroup(LinkImageSrcRegex.Match(html))
            ?? (JsonLdImageRegex.Match(html) is { Success: true } jm ? jm.Groups[1].Value : null);

        // Resolves protocol-relative ("//host/path"), root-relative
        // ("/path") and plain relative URLs against the page's own URL.
        // Returns null if the candidate can't be resolved to an absolute URL.
        public static Uri? Normalize(string? rawCandidate, Uri pageUri)
        {
            if (string.IsNullOrWhiteSpace(rawCandidate)) return null;

            var candidate = System.Net.WebUtility.HtmlDecode(rawCandidate.Trim());
            if (candidate.Length == 0) return null;

            if (candidate.StartsWith("//", StringComparison.Ordinal))
                candidate = "https:" + candidate;

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var absolute))
                return absolute;

            return Uri.TryCreate(pageUri, candidate, out var resolved) ? resolved : null;
        }

        // Obvious non-article-photo patterns: site logos/icons/favicons,
        // generic "default"/"placeholder" fallback images, sprite sheets,
        // or tiny fixed dimensions baked into the URL.
        public static bool LooksLikeNonPhoto(Uri imageUri)
        {
            var lower = imageUri.ToString().ToLowerInvariant();
            return NonPhotoHints.Any(h => lower.Contains(h, StringComparison.Ordinal))
                || TinyDimensionRegex.IsMatch(lower);
        }
    }
}
