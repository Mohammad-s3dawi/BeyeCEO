using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    public static class NewsArticleMapper
    {
        public const int MaxSourceUrlLength = 800;

        // Pure: a candidate counts as "already in DB" if EITHER its
        // normalized or its raw (pre-normalization) form matches an
        // existing non-deleted row's SourceUrl — old rows were stored with
        // raw, unnormalized URLs before LinkNormalizer existed.
        public static bool MatchesExisting(string normalizedUrl, string? rawUrl, ISet<string> existingUrls) =>
            existingUrls.Contains(normalizedUrl) ||
            (!string.IsNullOrWhiteSpace(rawUrl) && existingUrls.Contains(rawUrl));

        // Pure: maps one parsed+filtered feed item to a NewsArticle, or
        // returns null (and logs) if the normalized SourceUrl is too long to
        // store. Fills only the matching-language title/summary columns —
        // the other language follows the existing column nullability
        // (TitleEN/TitleAR are NOT NULL -> "", SummaryEN/SummaryAR are
        // nullable -> null). ContentEN/ContentAR stay "" since RSS/Atom/RDF
        // feeds only ever give us a summary, never full article body.
        public static NewsArticle? MapToArticle(
            ParsedFeedItem item,
            KeywordCheckResult check,
            FeedOptions feed,
            ILogger? logger = null)
        {
            var sourceUrl = LinkNormalizer.Normalize(item.Link);

            if (sourceUrl.Length > MaxSourceUrlLength)
            {
                logger?.LogWarning(
                    "Feed {Name}: skipping item with SourceUrl longer than {Max} chars: {Title}",
                    feed.Name, MaxSourceUrlLength, item.Title);
                return null;
            }

            var scope = string.Equals(feed.Scope, "International", StringComparison.OrdinalIgnoreCase)
                ? NewsScope.International
                : NewsScope.Local;

            var isAr = string.Equals(feed.Language, "ar", StringComparison.OrdinalIgnoreCase);

            var publishedAt = item.PublishedAtUtc;
            if (item.DateGuessed)
            {
                logger?.LogWarning(
                    "Feed {Name}: item '{Title}' had no parseable date, using now()", feed.Name, item.Title);
                publishedAt = DateTime.UtcNow;
            }

            return NewsArticle.Create(
                titleEN: isAr ? string.Empty : item.Title,
                titleAR: isAr ? item.Title : string.Empty,
                contentEN: string.Empty,
                contentAR: string.Empty,
                summary: item.Summary,
                sourceName: feed.Name,
                sourceLogoUrl: string.Empty,
                sourceUrl: sourceUrl,
                imageUrl: item.ImageUrl ?? string.Empty,
                category: "Banking",
                scope: scope,
                publishedAt: publishedAt,
                countryCode: scope == NewsScope.Local ? feed.CountryCode : null,
                summaryEN: isAr ? null : item.Summary,
                summaryAR: isAr ? item.Summary : null,
                originalLanguage: feed.Language,
                translationStatus: "NotRequested",
                isBankingRelated: true,
                isOfficialSource: check.IsOfficial,
                feedName: feed.Name);
        }

        // Re-wraps an already-built Guardian NewsArticle (GuardianClient has
        // no knowledge of keyword-filter verdicts) with the new optional
        // fields filled in, after it has passed the filter. Guardian is never
        // treated as an "official" source.
        public static NewsArticle? RemapGuardianArticle(NewsArticle article, ILogger? logger = null)
        {
            var sourceUrl = LinkNormalizer.Normalize(article.SourceUrl);

            if (sourceUrl.Length > MaxSourceUrlLength)
            {
                logger?.LogWarning(
                    "Guardian: skipping item with SourceUrl longer than {Max} chars: {Title}",
                    MaxSourceUrlLength, article.TitleEN);
                return null;
            }

            return NewsArticle.Create(
                titleEN: article.TitleEN,
                titleAR: article.TitleAR,
                contentEN: article.ContentEN,
                contentAR: article.ContentAR,
                summary: article.Summary,
                sourceName: article.SourceName,
                sourceLogoUrl: article.SourceLogoUrl,
                sourceUrl: sourceUrl,
                imageUrl: article.ImageUrl,
                category: article.Category,
                scope: NewsScope.International,
                publishedAt: article.PublishedAt,
                countryCode: null,
                summaryEN: article.Summary,
                summaryAR: null,
                originalLanguage: "en",
                translationStatus: "NotRequested",
                isBankingRelated: true,
                isOfficialSource: false,
                feedName: "The Guardian");
        }
    }
}
