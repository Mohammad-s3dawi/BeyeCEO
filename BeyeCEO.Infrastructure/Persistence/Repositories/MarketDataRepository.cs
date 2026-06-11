using BeyeCEO.Domain.MarketData.Entities;
using BeyeCEO.Domain.MarketData.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Repositories
{
    public class MarketDataRepository : IMarketDataRepository
    {
        private readonly BeyeCeoDbContext _context;

        public MarketDataRepository(BeyeCeoDbContext context)
        {
            _context = context;
        }

        // ── Global ────────────────────────────────────────────

        public async Task<IEnumerable<GlobalIndex>> GetLatestGlobalIndicesAsync()
        {
            return await _context.GlobalIndices
                .Where(x => !x.IsDeleted)
                .GroupBy(x => x.Symbol)
                .Select(g => g.OrderByDescending(x => x.RecordedAt).First())
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<CurrencyRate>> GetLatestCurrencyRatesAsync()
        {
            return await _context.CurrencyRates
                .Where(x => !x.IsDeleted)
                .GroupBy(x => new { x.BaseCurrency, x.QuoteCurrency })
                .Select(g => g.OrderByDescending(x => x.RecordedAt).First())
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Commodity>> GetLatestCommoditiesAsync()
        {
            return await _context.Commodities
                .Where(x => !x.IsDeleted)
                .GroupBy(x => x.Symbol)
                .Select(g => g.OrderByDescending(x => x.RecordedAt).First())
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<InterestRate>> GetLatestInterestRatesAsync()
        {
            return await _context.InterestRates
                .Where(x => !x.IsDeleted)
                .GroupBy(x => new { x.Institution, x.RateType })
                .Select(g => g.OrderByDescending(x => x.EffectiveDate).First())
                .AsNoTracking()
                .ToListAsync();
        }

        // ── Local ─────────────────────────────────────────────

        public async Task<IEnumerable<InterestRate>> GetInterestRatesByCountryAsync(
            string countryCode)
        {
            return await _context.InterestRates
                .Where(x => x.CountryCode == countryCode && !x.IsDeleted)
                .GroupBy(x => x.RateType)
                .Select(g => g.OrderByDescending(x => x.EffectiveDate).First())
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<StockExchangeData?> GetLatestStockExchangeDataAsync(
            string countryCode)
        {
            return await _context.StockExchangeData
                .Where(x => x.CountryCode == countryCode && !x.IsDeleted)
                .OrderByDescending(x => x.TradeDate)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<LocalIndicator>> GetLatestLocalIndicatorsAsync(
            string countryCode)
        {
            return await _context.LocalIndicators
                .Where(x => x.CountryCode == countryCode && !x.IsDeleted)
                .GroupBy(x => x.IndicatorCode)
                .Select(g => g.OrderByDescending(x => x.PeriodDate).First())
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<StockExchangeTopMover>> GetTopMoversAsync(
            string countryCode, DateOnly tradeDate)
        {
            return await _context.StockExchangeTopMovers
                .Where(x =>
                    x.CountryCode == countryCode &&
                    x.TradeDate == tradeDate &&
                    !x.IsDeleted)
                .OrderBy(x => x.MoverType)
                .ThenBy(x => x.Rank)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<StockExchangeHistory>> GetStockHistoryAsync(
            string countryCode, string periodType)
        {
            return await _context.StockExchangeHistories
                .Where(x =>
                    x.CountryCode == countryCode &&
                    x.PeriodType == periodType &&
                    !x.IsDeleted)
                .OrderBy(x => x.RecordedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        // ── Countries ─────────────────────────────────────────

        public async Task<IEnumerable<Country>> GetActiveCountriesAsync()
        {
            return await _context.Countries
                .Where(x => x.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Country?> GetCountryAsync(string countryCode)
        {
            return await _context.Countries
                .Where(x => x.CountryCode == countryCode && x.IsActive)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        // ── Save Global ───────────────────────────────────────

        public async Task SaveGlobalIndexAsync(GlobalIndex index)
        {
            await _context.GlobalIndices.AddAsync(index);
            await _context.SaveChangesAsync();
        }

        public async Task SaveGlobalIndicesRangeAsync(
            IEnumerable<GlobalIndex> indices)
        {
            await _context.GlobalIndices.AddRangeAsync(indices);
            await _context.SaveChangesAsync();
        }

        public async Task SaveCurrencyRateAsync(CurrencyRate rate)
        {
            await _context.CurrencyRates.AddAsync(rate);
            await _context.SaveChangesAsync();
        }

        public async Task SaveCurrencyRatesRangeAsync(
            IEnumerable<CurrencyRate> rates)
        {
            await _context.CurrencyRates.AddRangeAsync(rates);
            await _context.SaveChangesAsync();
        }

        public async Task SaveCommodityAsync(Commodity commodity)
        {
            await _context.Commodities.AddAsync(commodity);
            await _context.SaveChangesAsync();
        }

        public async Task SaveCommoditiesRangeAsync(
            IEnumerable<Commodity> commodities)
        {
            await _context.Commodities.AddRangeAsync(commodities);
            await _context.SaveChangesAsync();
        }

        public async Task SaveInterestRateAsync(InterestRate rate)
        {
            await _context.InterestRates.AddAsync(rate);
            await _context.SaveChangesAsync();
        }

        // ── Save Local ────────────────────────────────────────

        public async Task SaveStockExchangeDataAsync(StockExchangeData data)
        {
            var existing = await _context.StockExchangeData
                .FirstOrDefaultAsync(x =>
                    x.Exchange == data.Exchange &&
                    x.TradeDate == data.TradeDate);

            if (existing != null)
                _context.StockExchangeData.Remove(existing);

            await _context.StockExchangeData.AddAsync(data);
            await _context.SaveChangesAsync();
        }

        public async Task SaveLocalIndicatorAsync(LocalIndicator indicator)
        {
            var existing = await _context.LocalIndicators
                .FirstOrDefaultAsync(x =>
                    x.CountryCode == indicator.CountryCode &&
                    x.IndicatorCode == indicator.IndicatorCode &&
                    x.PeriodDate == indicator.PeriodDate);

            if (existing != null)
                _context.LocalIndicators.Remove(existing);

            await _context.LocalIndicators.AddAsync(indicator);
            await _context.SaveChangesAsync();
        }

        public async Task SaveTopMoversAsync(
            IEnumerable<StockExchangeTopMover> movers)
        {
            // احذف القديم لنفس البورصة ونفس اليوم
            var first = movers.FirstOrDefault();
            if (first == null) return;

            var existing = await _context.StockExchangeTopMovers
                .Where(x =>
                    x.Exchange == first.Exchange &&
                    x.TradeDate == first.TradeDate)
                .ToListAsync();

            if (existing.Any())
                _context.StockExchangeTopMovers.RemoveRange(existing);

            await _context.StockExchangeTopMovers.AddRangeAsync(movers);
            await _context.SaveChangesAsync();
        }

        public async Task SaveStockHistoryAsync(StockExchangeHistory history)
        {
            await _context.StockExchangeHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }
      
    }
}
