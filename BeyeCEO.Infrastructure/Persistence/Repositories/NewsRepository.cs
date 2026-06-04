using BeyeCEO.Domain.News.Entities;
using BeyeCEO.Domain.News.Enums;
using BeyeCEO.Domain.News.Interfaces;
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

        public async Task SaveRangeAsync(IEnumerable<NewsArticle> articles)
        {
            await _context.NewsArticles.AddRangeAsync(articles);
            await _context.SaveChangesAsync();
        }
    }
}
