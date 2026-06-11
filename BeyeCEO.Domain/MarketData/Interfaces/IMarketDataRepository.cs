using BeyeCEO.Domain.MarketData.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Interfaces
{

    public interface IMarketDataRepository
    {
        // ── Global ────────────────────────────────────────────
        Task<IEnumerable<GlobalIndex>> GetLatestGlobalIndicesAsync();
        Task<IEnumerable<CurrencyRate>> GetLatestCurrencyRatesAsync();
        Task<IEnumerable<Commodity>> GetLatestCommoditiesAsync();
        Task<IEnumerable<InterestRate>> GetLatestInterestRatesAsync();

        // ── Local — Any Country ───────────────────────────────
        Task<IEnumerable<InterestRate>> GetInterestRatesByCountryAsync(string countryCode);
        Task<StockExchangeData?> GetLatestStockExchangeDataAsync(string countryCode);
        Task<IEnumerable<LocalIndicator>> GetLatestLocalIndicatorsAsync(string countryCode);

        // ── Save Global ───────────────────────────────────────
        Task SaveGlobalIndexAsync(GlobalIndex index);
        Task SaveGlobalIndicesRangeAsync(IEnumerable<GlobalIndex> indices);
        Task SaveCurrencyRateAsync(CurrencyRate rate);
        Task SaveCurrencyRatesRangeAsync(IEnumerable<CurrencyRate> rates);
        Task SaveCommodityAsync(Commodity commodity);
        Task SaveCommoditiesRangeAsync(IEnumerable<Commodity> commodities);
        Task SaveInterestRateAsync(InterestRate rate);

        // ── Save Local ────────────────────────────────────────
        Task SaveStockExchangeDataAsync(StockExchangeData data);
        Task SaveLocalIndicatorAsync(LocalIndicator indicator);

        Task<IEnumerable<StockExchangeTopMover>> GetTopMoversAsync(
    string countryCode, DateOnly tradeDate);

        Task<IEnumerable<StockExchangeHistory>> GetStockHistoryAsync(
            string countryCode, string periodType);

        Task SaveTopMoversAsync(
            IEnumerable<StockExchangeTopMover> movers);

        Task SaveStockHistoryAsync(StockExchangeHistory history);
        Task<IEnumerable<Country>> GetActiveCountriesAsync();

    }
}
