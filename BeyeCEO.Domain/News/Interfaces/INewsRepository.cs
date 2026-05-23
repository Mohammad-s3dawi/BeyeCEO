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
        Task<IEnumerable<NewsArticle>> GetLatestAsync(NewsScope scope, int count = 10);
        Task<IEnumerable<NewsArticle>> GetByCategoryAsync(string category, int count = 10);
        Task<IEnumerable<NewsArticle>> SearchAsync(string keyword);
        Task<NewsArticle?> GetByIdAsync(Guid id);
        Task SaveAsync(NewsArticle article);
        Task SaveRangeAsync(IEnumerable<NewsArticle> articles);
    }
}
