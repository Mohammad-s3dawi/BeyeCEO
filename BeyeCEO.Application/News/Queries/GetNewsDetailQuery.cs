using BeyeCEO.Application.News.DTOs;
using BeyeCEO.Domain.News.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Application.News.Queries
{
    // ── Query ─────────────────────────────────────────────────
    public record GetNewsDetailQuery(Guid Id) : IRequest<NewsArticleDetailDto?>;

    // ── Handler ───────────────────────────────────────────────
    public class GetNewsDetailQueryHandler
        : IRequestHandler<GetNewsDetailQuery, NewsArticleDetailDto?>
    {
        private readonly INewsRepository _repo;

        public GetNewsDetailQueryHandler(INewsRepository repo)
        {
            _repo = repo;
        }

        public async Task<NewsArticleDetailDto?> Handle(
            GetNewsDetailQuery request, CancellationToken ct)
        {
            var article = await _repo.GetByIdAsync(request.Id);

            if (article == null) return null;

            return new NewsArticleDetailDto
            {
                Id = article.Id,
                TitleEN = article.TitleEN,
                TitleAR = article.TitleAR,
                ContentEN = article.ContentEN,
                ContentAR = article.ContentAR,
                Summary = article.Summary,
                SourceName = article.SourceName,
                SourceLogoUrl = article.SourceLogoUrl,
                SourceUrl = article.SourceUrl,
                ImageUrl = article.ImageUrl,
                Category = article.Category,
                CountryCode = article.CountryCode,
                Scope = (int)article.Scope,
                ReadTimeMinutes = article.ReadTimeMinutes,
                PublishedAt = article.PublishedAt
            };
        }
    }
}
