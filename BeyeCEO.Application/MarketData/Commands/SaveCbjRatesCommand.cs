using BeyeCEO.Domain.MarketData.Entities;
using BeyeCEO.Domain.MarketData.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Application.MarketData.Commands
{
    // ── Command ───────────────────────────────────────────────
    public record SaveCbjRatesCommand(
        string CountryCode,
        decimal MainRate,
        decimal ReDiscountRate,
        decimal RepoRate,
        decimal DepositWindowRate,
        DateOnly EffectiveDate
    ) : IRequest<Unit>;

    // ── Handler ───────────────────────────────────────────────
    public class SaveCbjRatesCommandHandler
        : IRequestHandler<SaveCbjRatesCommand, Unit>
    {
        private readonly IMarketDataRepository _repo;

        public SaveCbjRatesCommandHandler(IMarketDataRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(
            SaveCbjRatesCommand request, CancellationToken ct)
        {
            var rates = new (string Code, string NameEN, string NameAR, decimal Value)[]
            {
                ("MAIN_RATE", "Main Interest Rate",
                    "سعر الفائدة الرئيسي", request.MainRate),
                ("REDISCOUNT_RATE", "Re-Discount Rate",
                    "سعر إعادة الخصم", request.ReDiscountRate),
                ("REPO_RATE", "Overnight Repurchase Rate",
                    "سعر اتفاقيات إعادة الشراء", request.RepoRate),
                ("DEPOSIT_RATE", "Overnight Deposit Window Rate",
                    "سعر نافذة الإيداع الليلي", request.DepositWindowRate),
            };

            foreach (var rate in rates)
            {
                var indicator = LocalIndicator.Create(
                    countryCode: request.CountryCode,
                    indicatorCode: rate.Code,
                    nameEN: rate.NameEN,
                    nameAR: rate.NameAR,
                    value: rate.Value,
                    unit: "%",
                    periodDate: request.EffectiveDate);

                await _repo.SaveLocalIndicatorAsync(indicator);
            }

            return Unit.Value;
        }
    }
}
