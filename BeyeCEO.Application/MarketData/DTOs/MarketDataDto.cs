using BeyeCEO.Domain.MarketData.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Application.MarketData.DTOs
{
    public class GlobalMarketsDto
    {
        public IEnumerable<GlobalIndexDto> Indices { get; set; } = [];
        public IEnumerable<CurrencyRateDto> Currencies { get; set; } = [];
        public IEnumerable<CommodityDto> Commodities { get; set; } = [];
        public IEnumerable<InterestRateDto> InterestRates { get; set; } = [];
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
    public class LocalEconomyDto
    {
        public string CountryCode { get; set; } = string.Empty;
        public StockExchangeDto? StockExchange { get; set; }
        public IEnumerable<LocalIndicatorDto> Indicators { get; set; } = [];
        public IEnumerable<InterestRateDto> InterestRates { get; set; } = [];
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
    public class GlobalIndexDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePct { get; set; }
        public string Region { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
    }
    public class CurrencyRateDto
    {
        public string BaseCurrency { get; set; } = string.Empty;
        public string QuoteCurrency { get; set; } = string.Empty;
        public string Pair { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime RecordedAt { get; set; }
    }
    public class CommodityDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal ChangePct { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class InterestRateDto
    {
        public string Institution { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string RateType { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateOnly EffectiveDate { get; set; }
    }
    // ── Local ─────────────────────────────────────────────────
    public class StockExchangeDto
    {
        public string Exchange { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public decimal TradingAmount { get; set; }
        public long TradingVolume { get; set; }
        public int Transactions { get; set; }
        public int NoOfSecurities { get; set; }    // ← جديد
        public decimal BankingIndex { get; set; }
        public decimal GeneralIndex { get; set; }
        public decimal ChangePct { get; set; }      // ← جديد
        public decimal PreviousIndex { get; set; }  // ← جديد
        public int Gainers { get; set; }            // ← جديد
        public int Losers { get; set; }             // ← جديد
        public int Unchanged { get; set; }          // ← جديد
        public DateOnly TradeDate { get; set; }
        public IEnumerable<TopMoverDto> TopMovers { get; set; } = []; // ← جديد
    }

    // ← جديد
    public class TopMoverDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal ChangePct { get; set; }
        public string MoverType { get; set; } = string.Empty;
        public byte Rank { get; set; }
    }

    // ← جديد
    public class StockHistoryDto
    {
        public decimal IndexValue { get; set; }
        public DateTime RecordedAt { get; set; }
        public string PeriodType { get; set; } = string.Empty;
    }

    public class LocalIndicatorDto
    {
        public string IndicatorCode { get; set; } = string.Empty;
        public string IndicatorNameEN { get; set; } = string.Empty;
        public string IndicatorNameAR { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateOnly PeriodDate { get; set; }
    }
}
