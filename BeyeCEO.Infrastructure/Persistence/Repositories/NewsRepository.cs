using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Domain.News.Interfaces;
using BeyeCEO.Infrastructure.NewsAggregation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Repositories
{
    public class NewsRepository : INewsRepository
    {
        private readonly BeyeCeoDbContext _context;

        public NewsRepository(BeyeCeoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NewsArticle>> GetLatestAsync(
            NewsScope scope, int count = 10)
        {
            return await _context.NewsArticles
                .Where(x => !x.IsDeleted && x.Scope == scope)
                .OrderByDescending(x => x.PublishedAt)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<NewsArticle>> GetLatestLocalAsync(
            string countryCode, int count = 10)
        {
            return await _context.NewsArticles
                .Where(x => !x.IsDeleted
                    && x.Scope == NewsScope.Local
                    && x.CountryCode == countryCode)
                .OrderByDescending(x => x.PublishedAt)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<NewsArticle>> GetByCategoryAsync(
            string category, int count = 10)
        {
            return await _context.NewsArticles
                .Where(x => !x.IsDeleted && x.Category == category)
                .OrderByDescending(x => x.PublishedAt)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<NewsArticle>> SearchAsync(
            string keyword, string? countryCode = null)
        {
            var query = _context.NewsArticles
                .Where(x => !x.IsDeleted &&
                    (x.TitleEN.Contains(keyword) ||
                     x.TitleAR.Contains(keyword) ||
                     x.Summary.Contains(keyword)));

            // لو في countryCode — جيب محلي لهاد البلد + الدولي
            if (countryCode != null)
                query = query.Where(x =>
                    x.CountryCode == countryCode ||
                    x.CountryCode == null);

            return await query
                .OrderByDescending(x => x.PublishedAt)
                .Take(20)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<NewsArticle?> GetByIdAsync(Guid id)
        {
            return await _context.NewsArticles
                .Where(x => x.Id == id && !x.IsDeleted)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task SaveAsync(NewsArticle article)
        {
            await _context.NewsArticles.AddAsync(article);
            await _context.SaveChangesAsync();
        }

        public async Task<int> SaveRangeAsync(IEnumerable<NewsArticle> articles)
        {
            var deduped = DedupeIncomingBatch(articles);
            if (deduped.Count == 0) return 0;

            var urls = deduped.Select(a => a.SourceUrl).ToList();

            var existingUrls = await _context.NewsArticles
                .Where(a => urls.Contains(a.SourceUrl))
                .Select(a => a.SourceUrl)
                .ToListAsync();

            var newArticles = deduped
                .Where(a => !existingUrls.Contains(a.SourceUrl))
                .ToList();

            if (newArticles.Count == 0) return 0;

            try
            {
                await _context.NewsArticles.AddRangeAsync(newArticles);
                await _context.SaveChangesAsync();
                return newArticles.Count;
            }
            catch (DbUpdateException batchEx) when (IsDuplicateKeyViolation(batchEx))
            {
                // Something in this batch collided with a row inserted by a
                // concurrent run between our existence check and this save.
                // Fall back to one row at a time so a single duplicate
                // doesn't roll back the whole batch — skip only the
                // offending row(s), never crash the job.
                _context.ChangeTracker.Clear();

                var inserted = 0;
                foreach (var article in newArticles)
                {
                    try
                    {
                        _context.NewsArticles.Add(article);
                        await _context.SaveChangesAsync();
                        inserted++;
                    }
                    catch (DbUpdateException rowEx) when (IsDuplicateKeyViolation(rowEx))
                    {
                        _context.ChangeTracker.Clear();
                        // Duplicate SourceUrl — another run already saved this one. Skip it.
                    }
                }

                return inserted;
            }
        }

        // Deliberately includes soft-deleted rows (no IsDeleted filter —
        // there is no global EF query filter on this entity to bypass) so a
        // once-removed SourceUrl is never treated as "new" and re-ingested.
        // The unique index is still filtered to active rows only (Step 5),
        // so this check — not the index — is what prevents that (Step 9d).
        public async Task<HashSet<string>> FindExistingSourceUrlsAsync(IEnumerable<string> candidateUrls)
        {
            var candidates = candidateUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
            if (candidates.Count == 0) return new HashSet<string>();

            var existing = await _context.NewsArticles
                .Where(a => candidates.Contains(a.SourceUrl))
                .Select(a => a.SourceUrl)
                .ToListAsync();

            return new HashSet<string>(existing);
        }

        public async Task<List<NewsArticle>> GetRecentArticlesAsync(
            NewsScope scope, string? countryCode, DateTime sinceUtc)
        {
            return await _context.NewsArticles
                .Where(a => !a.IsDeleted && a.Scope == scope && a.CountryCode == countryCode
                    && a.PublishedAt >= sinceUtc)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<NewsArticle>> GetAllActiveBeforeAsync(DateTime beforeUtc)
        {
            return await _context.NewsArticles
                .Where(a => !a.IsDeleted && a.CreatedAt < beforeUtc)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<NewsArticle>> GetAllBeforeIncludingDeletedAsync(DateTime beforeUtc)
        {
            return await _context.NewsArticles
                .Where(a => a.CreatedAt < beforeUtc)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetImageUrlCountsAsync(string sourceName)
        {
            return await _context.NewsArticles
                .Where(a => !a.IsDeleted && a.SourceName == sourceName && a.ImageUrl != null && a.ImageUrl != "")
                .GroupBy(a => a.ImageUrl)
                .Select(g => new { Url = g.Key, Count = g.Count() })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Url, x => x.Count);
        }

        // Pure, unit-testable: collapses same-article duplicates within a
        // single incoming batch (e.g. Guardian returning the same article
        // from two overlapping section searches) by normalized link,
        // keeping the first occurrence.
        public static List<NewsArticle> DedupeIncomingBatch(IEnumerable<NewsArticle> articles)
        {
            return articles
                .GroupBy(a => LinkNormalizer.Normalize(a.SourceUrl))
                .Select(g => g.First())
                .ToList();
        }

        private static bool IsDuplicateKeyViolation(DbUpdateException ex) =>
            ex.InnerException is SqlException sqlEx && IsDuplicateKeyErrorNumber(sqlEx.Number);

        // Pure, unit-testable: SQL Server's two "unique constraint/index
        // violation" error numbers.
        public static bool IsDuplicateKeyErrorNumber(int number) => number is 2601 or 2627;
    }
}
