using BeyeCEO.Application.MarketData.DTOs;
using BeyeCEO.Domain.MarketData.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Application.MarketData.Queries
{
    // ── Query ─────────────────────────────────────────────────
    public record GetGlobalMarketsQuery() : IRequest<GlobalMarketsDto>;

    // ── Handler ───────────────────────────────────────────────
    public class GetGlobalMarketsQueryHandler
        : IRequestHandler<GetGlobalMarketsQuery, GlobalMarketsDto>
    {
        private readonly IMarketDataRepository _repo;

        public GetGlobalMarketsQueryHandler(IMarketDataRepository repo)
        {
            _repo = repo;
        }

        public async Task<GlobalMarketsDto> Handle(
            GetGlobalMarketsQuery request, CancellationToken ct)
        {
            // جيب كل البيانات بالتوازي
            var indicesTask = await _repo.GetLatestGlobalIndicesAsync();
            var currenciesTask = await _repo.GetLatestCurrencyRatesAsync();
            var commoditiesTask =await _repo.GetLatestCommoditiesAsync();
            var ratesTask =await _repo.GetLatestInterestRatesAsync();

            //await Task.WhenAll(indicesTask, currenciesTask,
            //    commoditiesTask, ratesTask);

            return new GlobalMarketsDto
            {
                Indices = ( indicesTask).Select(x => new GlobalIndexDto
                {
                    Symbol = x.Symbol,
                    Name = x.Name,
                    Value = x.Value,
                    Change = x.Change,
                    ChangePct = x.ChangePct,
                    Region = x.Region,
                    RecordedAt = x.RecordedAt
                }),

                Currencies = ( currenciesTask).Select(x => new CurrencyRateDto
                {
                    BaseCurrency = x.BaseCurrency,
                    QuoteCurrency = x.QuoteCurrency,
                    Pair = x.Pair,
                    Rate = x.Rate,
                    RecordedAt = x.RecordedAt
                }),

                Commodities = ( commoditiesTask).Select(x => new CommodityDto
                {
                    Symbol = x.Symbol,
                    Name = x.Name,
                    Price = x.Price,
                    Currency = x.Currency,
                    ChangePct = x.ChangePct,
                    RecordedAt = x.RecordedAt
                }),

                InterestRates = ( ratesTask).Select(x => new InterestRateDto
                {
                    Institution = x.Institution,
                    CountryCode = x.CountryCode,
                    RateType = x.RateType,
                    Rate = x.Rate,
                    EffectiveDate = x.EffectiveDate
                }),

                LastUpdated = DateTime.UtcNow
            };
        }
    }
}
