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
    public record GetLocalEconomyQuery(
        string CountryCode
    ) : IRequest<LocalEconomyDto>;

    // ── Handler ───────────────────────────────────────────────
    public class GetLocalEconomyQueryHandler
        : IRequestHandler<GetLocalEconomyQuery, LocalEconomyDto>
    {
        private readonly IMarketDataRepository _repo;

        public GetLocalEconomyQueryHandler(IMarketDataRepository repo)
        {
            _repo = repo;
        }

        public async Task<LocalEconomyDto> Handle(
            GetLocalEconomyQuery request, CancellationToken ct)
        {
            var countryCode = request.CountryCode.ToUpper();

            // جيب كل البيانات بالتوازي
            var stockTask =await _repo.GetLatestStockExchangeDataAsync(countryCode);
            var indicatorsTask =await _repo.GetLatestLocalIndicatorsAsync(countryCode);
            var ratesTask =await _repo.GetInterestRatesByCountryAsync(countryCode);

            //await Task.WhenAll(stockTask, indicatorsTask, ratesTask);

            var stock =  stockTask;

            var topMovers = stock == null
                ? []
                : await _repo.GetTopMoversAsync(countryCode, stock.TradeDate);

            return new LocalEconomyDto
            {
                CountryCode = countryCode,

                StockExchange = stock == null ? null : new StockExchangeDto
                {
                    Exchange = stock.Exchange,
                    CountryCode = stock.CountryCode,
                    TradingAmount = stock.TradingAmount,
                    TradingVolume = stock.TradingVolume,
                    Transactions = stock.Transactions,
                    BankingIndex = stock.BankingIndex,
                    GeneralIndex = stock.GeneralIndex,
                    TradeDate = stock.TradeDate,
                    NoOfSecurities = stock.NoOfSecurities,
                    Gainers = stock.Gainers,
                    Losers = stock.Losers,
                    Unchanged = stock.Unchanged,
                    ChangePct = stock.ChangePct,
                    PreviousIndex = stock.PreviousIndex,
                    TopMovers = topMovers.Select(m => new TopMoverDto
                    {
                        CompanyName = m.CompanyName,
                        Symbol = m.Symbol,
                        Price = m.Price,
                        ChangePct = m.ChangePct,
                        MoverType = m.MoverType,
                        Rank = m.Rank
                    })
                },

                Indicators = ( indicatorsTask).Select(x => new LocalIndicatorDto
                {
                    IndicatorCode = x.IndicatorCode,
                    IndicatorNameEN = x.IndicatorNameEN,
                    IndicatorNameAR = x.IndicatorNameAR,
                    Value = x.Value,
                    Unit = x.Unit,
                    PeriodDate = x.PeriodDate
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
