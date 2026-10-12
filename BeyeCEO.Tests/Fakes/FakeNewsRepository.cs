using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Domain.News.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeyeCEO.Tests.Fakes
{
    // Hand-rolled fake (no mocking library in this project) — just enough of
    // INewsRepository to observe whether SaveRangeAsync was called and to
    // control what FindExistingSourceUrlsAsync reports.
    public class FakeNewsRepository : INewsRepository
    {
        public int SaveRangeAsyncCallCount { get; private set; }
        public List<NewsArticle> SavedArticles { get; } = new();
        public HashSet<string> ExistingUrls { get; set; } = new();

        // Modeled separately from ExistingUrls so a test can prove a URL
        // that exists ONLY as a soft-deleted row is still reported as
        // existing by FindExistingSourceUrlsAsync (Step 9d hardening).
        public HashSet<string> SoftDeletedUrls { get; set; } = new();

        public Task<IEnumerable<NewsArticle>> GetLatestAsync(NewsScope scope, int count = 10) =>
            Task.FromResult(Enumerable.Empty<NewsArticle>());

        public Task<IEnumerable<NewsArticle>> GetLatestLocalAsync(string countryCode, int count = 10) =>
            Task.FromResult(Enumerable.Empty<NewsArticle>());

        public Task<IEnumerable<NewsArticle>> GetByCategoryAsync(string category, int count = 10) =>
            Task.FromResult(Enumerable.Empty<NewsArticle>());

        public Task<IEnumerable<NewsArticle>> SearchAsync(string keyword, string? countryCode = null) =>
            Task.FromResult(Enumerable.Empty<NewsArticle>());

        public Task<NewsArticle?> GetByIdAsync(Guid id) => Task.FromResult<NewsArticle?>(null);

        public Task SaveAsync(NewsArticle article) => Task.CompletedTask;

        public Task<int> SaveRangeAsync(IEnumerable<NewsArticle> articles)
        {
            SaveRangeAsyncCallCount++;
            var list = articles.ToList();
            SavedArticles.AddRange(list);
            return Task.FromResult(list.Count);
        }

        public Task<HashSet<string>> FindExistingSourceUrlsAsync(IEnumerable<string> candidateUrls)
        {
            var result = new HashSet<string>(
                candidateUrls.Where(u => ExistingUrls.Contains(u) || SoftDeletedUrls.Contains(u)));
            return Task.FromResult(result);
        }

        public List<NewsArticle> RecentArticles { get; set; } = new();

        public Task<List<NewsArticle>> GetRecentArticlesAsync(NewsScope scope, string? countryCode, DateTime sinceUtc) =>
            Task.FromResult(RecentArticles);

        public List<NewsArticle> AllActiveBefore { get; set; } = new();

        public Task<List<NewsArticle>> GetAllActiveBeforeAsync(DateTime beforeUtc) =>
            Task.FromResult(AllActiveBefore);

        public List<NewsArticle> AllBeforeIncludingDeleted { get; set; } = new();

        public Task<List<NewsArticle>> GetAllBeforeIncludingDeletedAsync(DateTime beforeUtc) =>
            Task.FromResult(AllBeforeIncludingDeleted);

        public Dictionary<string, int> ImageUrlCounts { get; set; } = new();

        public Task<Dictionary<string, int>> GetImageUrlCountsAsync(string sourceName) =>
            Task.FromResult(ImageUrlCounts);
    }
}
