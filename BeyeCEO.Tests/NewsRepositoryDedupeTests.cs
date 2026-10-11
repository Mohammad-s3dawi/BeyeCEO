using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeyeCEO.Tests
{
    public class NewsRepositoryDedupeTests
    {
        private static NewsArticle MakeArticle(string sourceUrl, string title = "Title") =>
            NewsArticle.Create(
                titleEN: title,
                titleAR: "",
                contentEN: "body",
                contentAR: "",
                summary: "summary",
                sourceName: "Test Source",
                sourceLogoUrl: "",
                sourceUrl: sourceUrl,
                imageUrl: "",
                category: "Banking",
                scope: NewsScope.International,
                publishedAt: DateTime.UtcNow);

        [Fact]
        public void DedupeIncomingBatch_CollapsesExactDuplicateLinks()
        {
            var articles = new List<NewsArticle>
            {
                MakeArticle("https://example.com/a", "First copy"),
                MakeArticle("https://example.com/a", "Second copy (same article, Guardian cross-section overlap)"),
                MakeArticle("https://example.com/b", "A different article"),
            };

            var result = NewsRepository.DedupeIncomingBatch(articles);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, a => a.TitleEN == "First copy");
            Assert.Contains(result, a => a.SourceUrl == "https://example.com/b");
        }

        [Fact]
        public void DedupeIncomingBatch_CollapsesLinksThatOnlyDifferByUtmParamsOrHostCasing()
        {
            var articles = new List<NewsArticle>
            {
                MakeArticle("https://Example.com/article?utm_source=feed&ref=abc"),
                MakeArticle("https://example.com/article?ref=abc&utm_campaign=x"),
                MakeArticle("https://example.com/article?ref=abc#fragment"),
            };

            var result = NewsRepository.DedupeIncomingBatch(articles);

            Assert.Single(result);
        }

        [Fact]
        public void DedupeIncomingBatch_KeepsDistinctArticlesSeparate()
        {
            var articles = new List<NewsArticle>
            {
                MakeArticle("https://example.com/one"),
                MakeArticle("https://example.com/two"),
                MakeArticle("https://example.com/three"),
            };

            var result = NewsRepository.DedupeIncomingBatch(articles);

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void DedupeIncomingBatch_EmptyInput_ReturnsEmpty()
        {
            Assert.Empty(NewsRepository.DedupeIncomingBatch([]));
        }

        [Theory]
        [InlineData(2601, true)]
        [InlineData(2627, true)]
        [InlineData(547, false)]   // FK/check constraint violation — not a duplicate
        [InlineData(0, false)]
        [InlineData(-1, false)]
        public void IsDuplicateKeyErrorNumber_OnlyTrueForSqlServerUniqueViolationCodes(int number, bool expected)
        {
            Assert.Equal(expected, NewsRepository.IsDuplicateKeyErrorNumber(number));
        }
    }
}
