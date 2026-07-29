using BeyeCEO.API.Extensions;
using BeyeCEO.Application.News.DTOs;
using BeyeCEO.Application.News.Queries;
using BeyeCEO.Domain.News.Interfaces;
using BeyeCEO.Infrastructure.ExternalServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BeyeCEO.API.Controllers
{

    [ApiController]
    [Route("api/news")]
    [Authorize]
    public class NewsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly RssNewsClient _rssNews;
        private readonly INewsRepository _newsRepo;

        public NewsController(
            IMediator mediator, RssNewsClient rssNews, INewsRepository newsRepo)
        {
            _mediator = mediator;
            _rssNews = rssNews;
            _newsRepo = newsRepo;
        }

        // GET /api/news/test-rss/{countryCode} — temporary diagnostic endpoint
        [HttpGet("test-rss/{countryCode}")]
        [AllowAnonymous]
        public async Task<IActionResult> TestRss(string countryCode)
        {
            var articles = await _rssNews.FetchLocalNewsAsync(countryCode);

            if (articles.Count > 0)
                await _newsRepo.SaveRangeAsync(articles);

            var result = new
            {
                count = articles.Count,
                articles = articles.Select(a => new
                {
                    a.TitleEN,
                    a.TitleAR,
                    a.SourceName,
                    a.SourceUrl,
                    a.PublishedAt
                })
            };

            return Ok(new ApiResponse<object>(true, result));
        }

        // GET /api/news/{countryCode}
        // يجيب المحلي للبلد + الدولي مع بعض
        [HttpGet("{countryCode}")]
        public async Task<IActionResult> GetLatestNews(
            string countryCode,
            [FromQuery] int count = 10)
        {
            var result = await _mediator.Send(
                new GetLatestNewsQuery(countryCode, count));

            return Ok(new ApiResponse<NewsListDto>(true, result));
        }

        // GET /api/news/detail/{id}
        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetNewsDetail(Guid id)
        {
            var result = await _mediator.Send(
                new GetNewsDetailQuery(id));

            if (result == null)
                return NotFound(new ApiResponse<string>(
                    false, "Article not found"));

            return Ok(new ApiResponse<NewsArticleDetailDto>(true, result));
        }

        // GET /api/news/search?keyword=banking&countryCode=JO
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string keyword,
            [FromQuery] string? countryCode = null)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new ApiResponse<string>(
                    false, "Keyword is required"));

            var result = await _mediator.Send(
                new SearchNewsQuery(keyword, countryCode));

            return Ok(new ApiResponse<IEnumerable<NewsArticleDto>>(
                true, result));
        }
    }
}
