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
        Task SaveRangeAsync(IEnumerable<NewsArticle> articles);
    }
}
