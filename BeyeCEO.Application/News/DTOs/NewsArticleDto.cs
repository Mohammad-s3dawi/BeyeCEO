using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Application.News.DTOs
{
    public class NewsArticleDto
    {
        public Guid Id { get; set; }
        public string TitleEN { get; set; } = string.Empty;
        public string TitleAR { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public string SourceLogoUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? CountryCode { get; set; }
        public int Scope { get; set; }         // 1=Local, 2=International
        public int ReadTimeMinutes { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    public class NewsArticleDetailDto : NewsArticleDto
    {
        public string ContentEN { get; set; } = string.Empty;
        public string ContentAR { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
    }

    public class NewsListDto
    {
        public IEnumerable<NewsArticleDto> Local { get; set; } = [];
        public IEnumerable<NewsArticleDto> International { get; set; } = [];
    }
}
