using BeyeCEO.Application.MarketData.DTOs;
using BeyeCEO.Domain.MarketData.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Application.MarketData.Queries
{
    // ── Query ─────────────────────────────────────────────────
    public record GetBankAdsQuery(string CountryCode) : IRequest<BankAdsListDto>;

    // ── Handler ───────────────────────────────────────────────
    public class GetBankAdsQueryHandler
        : IRequestHandler<GetBankAdsQuery, BankAdsListDto>
    {
        private readonly IMarketDataRepository _repo;

        public GetBankAdsQueryHandler(IMarketDataRepository repo)
        {
            _repo = repo;
        }

        public async Task<BankAdsListDto> Handle(
            GetBankAdsQuery request, CancellationToken ct)
        {
            var ads = await _repo.GetBankAdsAsync(request.CountryCode.ToUpper());

            return new BankAdsListDto
            {
                // مرتبة حسب البنك (Grouped by bankName عبر الترتيب)
                Ads = ads.Select(x => new BankAdDto
                {
                    Id = x.Id,
                    BankName = x.BankName,
                    BankNameAR = x.BankNameAR,
                    ImageUrl = x.ImageUrl,
                    AltText = x.AltText,
                    ScrapedAt = x.ScrapedAt
                })
            };
        }
    }
}
