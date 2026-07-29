using BeyeCEO.Application.MarketData.DTOs;
using BeyeCEO.Domain.MarketData.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Application.MarketData.Queries
{
    // ── Query ─────────────────────────────────────────────────
    public record GetCircularsQuery(
        string CountryCode,
        string? Tab = null,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<CircularsListDto>;

    // ── Handler ───────────────────────────────────────────────
    public class GetCircularsQueryHandler
        : IRequestHandler<GetCircularsQuery, CircularsListDto>
    {
        private readonly IMarketDataRepository _repo;

        public GetCircularsQueryHandler(IMarketDataRepository repo)
        {
            _repo = repo;
        }

        public async Task<CircularsListDto> Handle(
            GetCircularsQuery request, CancellationToken ct)
        {
            var (items, total) = await _repo.GetCircularsAsync(
                request.CountryCode.ToUpper(), request.Page, request.PageSize);

            return new CircularsListDto
            {
                Circulars = items.Select(c => new CircularDto
                {
                    Id = c.Id,
                    TitleAR = c.TitleAR,
                    TitleEN = c.TitleEN,
                    CircularNumber = c.CircularNumber,
                    CircularDate = c.CircularDate,
                    PdfUrl = c.PdfUrl,
                    FileSize = c.FileSize
                }),
                Total = total
            };
        }
    }
}
