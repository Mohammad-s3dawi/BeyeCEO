using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class StockExchangeData : BaseEntity
    {
        public string CountryCode { get; private set; } = string.Empty;
        public string Exchange { get; private set; } = string.Empty;
        public decimal TradingAmount { get; private set; }
        public long TradingVolume { get; private set; }
        public int Transactions { get; private set; }
        public decimal BankingIndex { get; private set; }
        public decimal GeneralIndex { get; private set; }
        public DateOnly TradeDate { get; private set; }
        public DateTime RecordedAt { get; private set; }

        // ← جديد
        public int NoOfSecurities { get; private set; }
        public int Gainers { get; private set; }
        public int Losers { get; private set; }
        public int Unchanged { get; private set; }
        public decimal ChangePct { get; private set; }
        public decimal PreviousIndex { get; private set; }

        // Navigation
        public Country Country { get; private set; } = null!;

        private StockExchangeData() { }

        public static StockExchangeData Create(
            string countryCode, string exchange,
            decimal tradingAmount, long tradingVolume,
            int transactions, decimal bankingIndex,
            decimal generalIndex, DateOnly tradeDate,
            int noOfSecurities = 0, int gainers = 0,
            int losers = 0, int unchanged = 0,
            decimal changePct = 0, decimal previousIndex = 0)
        {
            if (countryCode.Length != 2)
                throw new ArgumentException("CountryCode must be 2 characters");

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
                NoOfSecurities = noOfSecurities,
                Gainers = gainers,
                Losers = losers,
                Unchanged = unchanged,
                ChangePct = changePct,
                PreviousIndex = previousIndex,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}
