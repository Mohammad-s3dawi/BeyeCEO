using BeyeCEO.Application.News.DTOs;
using BeyeCEO.Domain.News.Enums;
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
    public record GetLatestNewsQuery(
        string CountryCode,
        int Count = 10
    ) : IRequest<NewsListDto>;

    // ── Handler ───────────────────────────────────────────────
    public class GetLatestNewsQueryHandler
        : IRequestHandler<GetLatestNewsQuery, NewsListDto>
    {
        private readonly INewsRepository _repo;

        public GetLatestNewsQueryHandler(INewsRepository repo)
        {
            _repo = repo;
        }

        public async Task<NewsListDto> Handle(
            GetLatestNewsQuery request, CancellationToken ct)
        {
            // جيب المحلي والدولي sequential
            var local = await _repo.GetLatestLocalAsync(
                request.CountryCode.ToUpper(), request.Count);

            var international = await _repo.GetLatestAsync(
                NewsScope.International, request.Count);

            return new NewsListDto
            {
                Local = local.Select(MapToDto),
                International = international.Select(MapToDto)
            };
        }

        public static NewsArticleDto MapToDto(
            Domain.News.Entities.NewsArticle x) => new()
            {
                Id = x.Id,
                TitleEN = x.TitleEN,
                TitleAR = x.TitleAR,
                Summary = x.Summary,
                SourceName = x.SourceName,
                SourceLogoUrl = x.SourceLogoUrl,
                ImageUrl = x.ImageUrl,
                Category = x.Category,
                CountryCode = x.CountryCode,
                Scope = (int)x.Scope,
                ReadTimeMinutes = x.ReadTimeMinutes,
                PublishedAt = x.PublishedAt
            };
    }
}
