using System;
using System.Linq;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    public static class LinkNormalizer
    {
        public static string Normalize(string? rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) return string.Empty;

            var trimmed = rawUrl.Trim();

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                return trimmed;

            var keptParams = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(pair => !pair.StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var newQuery = keptParams.Count > 0 ? "?" + string.Join("&", keptParams) : string.Empty;

            var builder = new UriBuilder(uri)
            {
                Host = uri.Host.ToLowerInvariant(),
                Fragment = string.Empty,
                Query = newQuery.TrimStart('?')
            };

            var result = builder.Uri.ToString();

            // UriBuilder leaves a trailing '?' when the query is empty.
            if (result.EndsWith("?"))
                result = result[..^1];

            return result;
        }
    }
}
