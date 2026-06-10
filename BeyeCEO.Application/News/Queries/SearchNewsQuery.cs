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
    public record SearchNewsQuery(
        string Keyword,
        string? CountryCode = null
    ) : IRequest<IEnumerable<NewsArticleDto>>;

    // ── Handler ───────────────────────────────────────────────
    public class SearchNewsQueryHandler
        : IRequestHandler<SearchNewsQuery, IEnumerable<NewsArticleDto>>
    {
        private readonly INewsRepository _repo;

        public SearchNewsQueryHandler(INewsRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<NewsArticleDto>> Handle(
            SearchNewsQuery request, CancellationToken ct)
        {
            var articles = await _repo.SearchAsync(
                request.Keyword,
                request.CountryCode?.ToUpper());

            return articles.Select(
                GetLatestNewsQueryHandler.MapToDto);
        }
    }
}
