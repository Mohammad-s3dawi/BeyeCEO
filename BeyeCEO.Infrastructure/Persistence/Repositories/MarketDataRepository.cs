using BeyeCEO.Domain.KPIs.Entites;
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

        // ── Circulars ─────────────────────────────────────────

        public async Task SaveCircularAsync(CentralBankCircular circular)
        {
            var existing = await _context.CentralBankCirculars
                .FirstOrDefaultAsync(x => x.PdfUrl == circular.PdfUrl);

            if (existing != null)
                _context.CentralBankCirculars.Remove(existing);

            await _context.CentralBankCirculars.AddAsync(circular);
            await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<CentralBankCircular> Items, int Total)> GetCircularsAsync(
            string countryCode, int page, int pageSize)
        {
            var query = _context.CentralBankCirculars
                .Where(x => x.CountryCode == countryCode && !x.IsDeleted)
                .OrderByDescending(x => x.CircularDate);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, total);
        }

        // ── Bank Ads ──────────────────────────────────────────

        public async Task SaveBankAdAsync(BankAd ad)
        {
            // قارن على الـ path بس — بتجاهل الدومين واختلاف الـ query string
            var normalizedUrl = new Uri(ad.ImageUrl).AbsolutePath.ToLower();

            var exists = await _context.BankAds
                .AnyAsync(x => x.ImageUrl.Contains(normalizedUrl));

            if (exists) return;

            await _context.BankAds.AddAsync(ad);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<BankAd>> GetBankAdsAsync(string countryCode)
        {
            return await _context.BankAds
                .Where(x => x.CountryCode == countryCode && !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.BankName)
                .ThenByDescending(x => x.ScrapedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        // ── Bank Performance Metrics ──────────────────────────

        public async Task SaveBankPerformanceAsync(
            List<BankPerformanceMetric> metrics, Guid bankId, string section)
        {
            if (metrics.Count == 0) return;

            // كل KPI (GroupName+KpiName) إله تاريخه الخاص — منمسح القديم تبع كل
            // واحد على حدة، مش بتاريخ واحد للدفعة كلها (بعض الـ KPIs بتتأخر شهر)
            var keys = metrics
                .Select(m => new { m.GroupName, m.KpiName })
                .Distinct()
                .ToList();

            foreach (var key in keys)
            {
                var existing = await _context.BankPerformanceMetrics
                    .Where(x =>
                        x.BankId == bankId &&
                        x.Section == section &&
                        x.GroupName == key.GroupName &&
                        x.KpiName == key.KpiName)
                    .ToListAsync();

                if (existing.Count > 0)
                    _context.BankPerformanceMetrics.RemoveRange(existing);
            }

            await _context.BankPerformanceMetrics.AddRangeAsync(metrics);
            await _context.SaveChangesAsync();
        }

        public async Task<List<BankPerformanceMetric>> GetBankPerformanceAsync(
            Guid bankId, string section)
        {
            // آخر تاريخ متوفر لكل KPI لحاله — مش تاريخ واحد للجدول كله،
            // عشان بعض الـ KPIs (زي CASA Balance) بتتأخر شهر عن الباقي.
            // ← GroupBy بمفتاح مركب ما بترجم صح لـ SQL بـ EF Core (EmptyProjectionMember)،
            // فبنجيب كل الصفوف ونعمل الـ grouping بالـ memory بدال ما نسيبها لـ SQL.
            var allMetrics = await _context.BankPerformanceMetrics
                .Where(x => x.BankId == bankId && x.Section == section && !x.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            return allMetrics
                .GroupBy(x => new { x.GroupName, x.KpiName })
                .Select(g => g.OrderByDescending(x => x.AsOfDate).First())
                .OrderBy(x => x.SortOrder)
                .ToList();
        }

        public async Task<IEnumerable<Bank>> GetBeyeEnabledBanksAsync()
        {
            return await _context.Banks
                .Where(x => x.IsActive && x.HasBeyeSystem)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
