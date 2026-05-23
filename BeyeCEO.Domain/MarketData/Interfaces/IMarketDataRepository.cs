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

        // Global
        Task<IEnumerable<GlobalIndex>> GetLatestGlobalIndicesAsync();
        Task<IEnumerable<CurrencyRate>> GetLatestCurrencyRatesAsync();
        Task<IEnumerable<Commodity>> GetLatestCommoditiesAsync();
        Task<IEnumerable<InterestRate>> GetLatestInterestRatesAsync();

        // Local — Jordan
        Task<ASEData?> GetLatestASEDataAsync();
        Task<IEnumerable<CBJIndicator>> GetLatestCBJIndicatorsAsync();

        // Save
        Task SaveGlobalIndexAsync(GlobalIndex index);
        Task SaveCurrencyRateAsync(CurrencyRate rate);
        Task SaveCommodityAsync(Commodity commodity);
        Task SaveInterestRateAsync(InterestRate rate);
        Task SaveASEDataAsync(ASEData data);
        Task SaveCBJIndicatorAsync(CBJIndicator indicator);
    }
}
