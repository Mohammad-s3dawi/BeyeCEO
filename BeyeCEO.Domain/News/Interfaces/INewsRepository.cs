using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.News.Interfaces
{
    public interface INewsRepository
    {
        // International news
        Task<IEnumerable<NewsArticle>> GetLatestAsync(NewsScope scope, int count = 10);

        // Local news — by country
        Task<IEnumerable<NewsArticle>> GetLatestLocalAsync(string countryCode, int count = 10);

        Task<IEnumerable<NewsArticle>> GetByCategoryAsync(string category, int count = 10);
        Task<IEnumerable<NewsArticle>> SearchAsync(string keyword, string? countryCode = null);
        Task<NewsArticle?> GetByIdAsync(Guid id);
        Task SaveAsync(NewsArticle article);

        // Returns the number of articles actually inserted (duplicates
        // skipped — see NewsRepository for the in-batch/DB dedupe logic).
        Task<int> SaveRangeAsync(IEnumerable<NewsArticle> articles);

        // Which of the given URLs (normalized and/or raw forms all passed in
        // together) already exist as a SourceUrl — among ALL rows,
        // including soft-deleted ones (Step 9d), so a previously removed
        // article is never mistaken for new and re-ingested.
        Task<HashSet<string>> FindExistingSourceUrlsAsync(IEnumerable<string> candidateUrls);

        // Non-deleted rows of the given scope/country published since
        // `sinceUtc` — used for cross-source near-duplicate detection
        // against already-stored articles (read-only).
        Task<List<NewsArticle>> GetRecentArticlesAsync(NewsScope scope, string? countryCode, DateTime sinceUtc);

        // Non-deleted rows created strictly before `beforeUtc` — used by the
        // read-only old-rows classification tool (Step 9a). Never writes.
        Task<List<NewsArticle>> GetAllActiveBeforeAsync(DateTime beforeUtc);

        // ALL rows (active AND soft-deleted) created strictly before
        // `beforeUtc` — used by the read-only old-rows re-classification
        // tool (Step 9d) to see which currently-deleted rows would now flip
        // back to KEEP. Never writes.
        Task<List<NewsArticle>> GetAllBeforeIncludingDeletedAsync(DateTime beforeUtc);
    }
}
