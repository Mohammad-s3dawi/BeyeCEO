using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class StockExchangeTopMover : BaseEntity
    {
        public string CountryCode { get; private set; } = string.Empty;
        public string Exchange { get; private set; } = string.Empty;
        public string CompanyName { get; private set; } = string.Empty;
        public string Symbol { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public decimal ChangePct { get; private set; }
        public string MoverType { get; private set; } = string.Empty; // Gainer/Loser/Unchanged
        public byte Rank { get; private set; }   // 1-5
        public DateOnly TradeDate { get; private set; }
        public DateTime RecordedAt { get; private set; }

        // Navigation
        public Country Country { get; private set; } = null!;

        private StockExchangeTopMover() { }

        public static StockExchangeTopMover Create(
            string countryCode, string exchange,
            string companyName, string symbol,
            decimal price, decimal changePct,
            string moverType, byte rank, DateOnly tradeDate)
        {
            if (rank < 1 || rank > 5)
                throw new ArgumentException("Rank must be between 1 and 5");

            if (!new[] { "Gainer", "Loser", "Unchanged" }.Contains(moverType))
                throw new ArgumentException("Invalid MoverType");

            return new StockExchangeTopMover
            {
                CountryCode = countryCode.ToUpper(),
                Exchange = exchange.ToUpper(),
                CompanyName = companyName,
                Symbol = symbol.ToUpper(),
                Price = price,
                ChangePct = changePct,
                MoverType = moverType,
                Rank = rank,
                TradeDate = tradeDate,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}
