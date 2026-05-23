using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class ASEData:BaseEntity
    {
        public decimal TradingAmount { get; private set; }
        public long TradingVolume { get; private set; }
        public int Transactions { get; private set; }
        public decimal BankingIndex { get; private set; }
        public decimal GeneralIndex { get; private set; }
        public DateOnly TradeDate { get; private set; }
        public DateTime RecordedAt { get; private set; }

        private ASEData() { }

        public static ASEData Create(decimal tradingAmount, long tradingVolume,
            int transactions, decimal bankingIndex, decimal generalIndex, DateOnly tradeDate)
        {
            return new ASEData
            {
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
