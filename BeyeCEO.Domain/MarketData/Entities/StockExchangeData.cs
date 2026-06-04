using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class StockExchangeData:BaseEntity
    {
        public string CountryCode { get; private set; } = string.Empty;
        public string Exchange { get; private set; } = string.Empty;    // 'ASE', 'EGX', 'TADAWUL'
        public decimal TradingAmount { get; private set; }
        public long TradingVolume { get; private set; }
        public int Transactions { get; private set; }
        public decimal BankingIndex { get; private set; }
        public decimal GeneralIndex { get; private set; }
        public DateOnly TradeDate { get; private set; }
        public DateTime RecordedAt { get; private set; }

        // Navigation
        public Country Country { get; private set; } = null!;

        private StockExchangeData() { }

        public static StockExchangeData Create(string countryCode, string exchange,
            decimal tradingAmount, long tradingVolume, int transactions,
            decimal bankingIndex, decimal generalIndex, DateOnly tradeDate)
        {
            if (countryCode.Length != 2)
                throw new ArgumentException("CountryCode must be 2 characters");

            if (string.IsNullOrWhiteSpace(exchange))
                throw new ArgumentException("Exchange name is required");

            return new StockExchangeData
            {
                CountryCode = countryCode.ToUpper(),
                Exchange = exchange.ToUpper(),
                TradingAmount = tradingAmount,
                TradingVolume = tradingVolume,
                Transactions = transactions,
                BankingIndex = bankingIndex,
                GeneralIndex = generalIndex,
                TradeDate = tradeDate,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}
