using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    // Tolerant parser for RSS 2.0, Atom 1.0 and RSS 1.0 / RDF (e.g. BIS).
    // Never throws — unparseable input yields an empty list so a bad feed
    // can be logged and skipped by the caller instead of crashing the run.
    public static class FeedParser
    {
        private static readonly Regex ImgSrcRegex = new(
            "<img[^>]*\\bsrc=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex ControlCharsRegex = new(
            "[\\x00-\\x08\\x0B\\x0C\\x0E-\\x1F]", RegexOptions.Compiled);

        // `baseUrl` (the feed's own URL) resolves protocol-relative ("//host/x")
        // and root-relative ("/x") image URLs — only the scheme/host/port of
        // baseUrl matter for that, so the feed endpoint URL works fine even
        // though it isn't the site's homepage.
        public static List<ParsedFeedItem> Parse(string xml, string format, string? baseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(xml)) return new List<ParsedFeedItem>();

            var fmt = (format ?? "rss").Trim().ToLowerInvariant();
            var cleaned = xml.TrimStart('﻿');

            var doc = TryParseXml(cleaned)
                ?? TryParseXml(ControlCharsRegex.Replace(cleaned, string.Empty))
                ?? TryParseXml(TruncateTrailingGarbage(cleaned));
            if (doc?.Root == null) return new List<ParsedFeedItem>();

            try
            {
                return fmt == "atom" ? ParseAtom(doc, baseUrl) : ParseRssLike(doc, baseUrl);
            }
            catch
            {
                // Tolerant: a malformed individual item should not blow up the whole feed.
                return new List<ParsedFeedItem>();
            }
        }

        private static XDocument? TryParseXml(string xml)
        {
            try { return XDocument.Parse(xml, LoadOptions.None); }
            catch { return null; }
        }

        // Some WAFs/CDNs (observed on a Cloudflare-fronted feed) inject a bot
        // -challenge <script> snippet after the real XML root closes, which
        // breaks strict XML parsing with "multiple root elements". If a known
        // root closing tag is found with extra content trailing it, drop
        // everything after that tag and try again.
        private static string TruncateTrailingGarbage(string xml)
        {
            foreach (var rootTag in new[] { "</rss>", "</feed>", "</rdf:RDF>" })
            {
                var idx = xml.IndexOf(rootTag, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;

                var endIdx = idx + rootTag.Length;
                return endIdx < xml.Length ? xml[..endIdx] : xml;
            }

            return xml;
        }

        private static List<ParsedFeedItem> ParseRssLike(XDocument doc, string? baseUrl)
        {
            var result = new List<ParsedFeedItem>();

            foreach (var item in doc.Descendants().Where(e => e.Name.LocalName == "item"))
            {
                try
                {
                    var title = ElementText(item, "title");
                    var link = ElementText(item, "link");
                    var description = ElementText(item, "description");
                    var contentEncoded = ElementText(item, "encoded"); // content:encoded
                    var pubDate = ElementText(item, "pubDate") ?? ElementText(item, "date"); // dc:date (RDF)

                    var enclosureUrl = item.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "enclosure")
                        ?.Attribute("url")?.Value;

                    var mediaContentUrl = item.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "content" && e.Attribute("url") != null)
                        ?.Attribute("url")?.Value;

                    var mediaThumbnailUrl = item.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "thumbnail" && e.Attribute("url") != null)
                        ?.Attribute("url")?.Value;

                    var image = FirstNonEmpty(
                        enclosureUrl,
                        mediaContentUrl,
                        mediaThumbnailUrl,
                        ExtractFirstImgSrc(contentEncoded),
                        ExtractFirstImgSrc(description));

                    var (publishedAtUtc, guessed) = ParseDate(pubDate);

                    result.Add(new ParsedFeedItem
                    {
                        Title = CleanText(title),
                        Link = LinkNormalizer.Normalize(link),
                        RawLink = link?.Trim() ?? string.Empty,
                        Summary = Summarize(description ?? contentEncoded),
                        PublishedAtUtc = publishedAtUtc,
                        DateGuessed = guessed,
                        ImageUrl = ResolveImageUrl(image, baseUrl)
                    });
                }
                catch
                {
                    // skip this single item, keep going
                }
            }

            return result;
        }

        private static List<ParsedFeedItem> ParseAtom(XDocument doc, string? baseUrl)
        {
            var result = new List<ParsedFeedItem>();

            foreach (var entry in doc.Descendants().Where(e => e.Name.LocalName == "entry"))
            {
                try
                {
                    var title = ElementText(entry, "title");
                    var summaryRaw = ElementText(entry, "summary") ?? ElementText(entry, "content");
                    var updated = ElementText(entry, "updated") ?? ElementText(entry, "published");

                    var links = entry.Elements().Where(e => e.Name.LocalName == "link").ToList();
                    var link = links.FirstOrDefault(e => (string?)e.Attribute("rel") == "alternate")
                        ?? links.FirstOrDefault(e => e.Attribute("rel") == null)
                        ?? links.FirstOrDefault();
                    var linkHref = link?.Attribute("href")?.Value;

                    var enclosureHref = links
                        .FirstOrDefault(e => (string?)e.Attribute("rel") == "enclosure")
                        ?.Attribute("href")?.Value;

                    var mediaContentUrl = entry.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "content" && e.Attribute("url") != null)
                        ?.Attribute("url")?.Value;

                    var mediaThumbnailUrl = entry.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "thumbnail" && e.Attribute("url") != null)
                        ?.Attribute("url")?.Value;

                    var image = FirstNonEmpty(
                        enclosureHref,
                        mediaContentUrl,
                        mediaThumbnailUrl,
                        ExtractFirstImgSrc(summaryRaw));

                    var (publishedAtUtc, guessed) = ParseDate(updated);

                    result.Add(new ParsedFeedItem
                    {
                        Title = CleanText(title),
                        Link = LinkNormalizer.Normalize(linkHref),
                        RawLink = linkHref?.Trim() ?? string.Empty,
                        Summary = Summarize(summaryRaw),
                        PublishedAtUtc = publishedAtUtc,
                        DateGuessed = guessed,
                        ImageUrl = ResolveImageUrl(image, baseUrl)
                    });
                }
                catch
                {
                    // skip this single entry, keep going
                }
            }

            return result;
        }

        // Resolves protocol-relative ("//host/x") and root-relative ("/x")
        // image URLs against the feed's own URL — only scheme/host/port of
        // `baseUrl` matter for a root-relative resolution, so the feed
        // endpoint itself is a fine base even though it isn't the homepage.
        // Already-absolute URLs pass through unchanged.
        private static string? ResolveImageUrl(string? image, string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(image)) return null;
            var trimmed = image.Trim();

            if (trimmed.StartsWith("//", StringComparison.Ordinal))
                return "https:" + trimmed;

            if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
                return trimmed;

            if (!string.IsNullOrWhiteSpace(baseUrl) &&
                Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) &&
                Uri.TryCreate(baseUri, trimmed, out var resolved))
                return resolved.ToString();

            return trimmed;
        }

        private static string? ElementText(XElement parent, string localName) =>
            parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;

        private static string? FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        private static string? ExtractFirstImgSrc(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;
            var match = ImgSrcRegex.Match(html);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string CleanText(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return WebUtility.HtmlDecode(raw).Trim();
        }

        private static string Summarize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            var noTags = TagRegex.Replace(raw, " ");
            var decoded = WebUtility.HtmlDecode(noTags);
            var collapsed = Regex.Replace(decoded, "\\s+", " ").Trim();

            return collapsed.Length > 600 ? collapsed[..600] : collapsed;
        }

        private static (DateTime utc, bool guessed) ParseDate(string? raw)
        {
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var candidate = raw.Trim();

                if (DateTimeOffset.TryParse(
                        candidate,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var dto))
                {
                    return (dto.UtcDateTime, false);
                }
            }

            return (DateTime.UtcNow, true);
        }
    }
}
