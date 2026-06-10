using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class StockExchangeHistory : BaseEntity
    {
        public string CountryCode { get; private set; } = string.Empty;
        public string Exchange { get; private set; } = string.Empty;
        public decimal IndexValue { get; private set; }
        public DateTime RecordedAt { get; private set; }
        public string PeriodType { get; private set; } = string.Empty; // 1D,1W,1M,1Y

        // Navigation
        public Country Country { get; private set; } = null!;

        private StockExchangeHistory() { }

        public static StockExchangeHistory Create(
            string countryCode, string exchange,
            decimal indexValue, string periodType)
        {
            if (!new[] { "1D", "1W", "1M", "1Y" }.Contains(periodType))
                throw new ArgumentException("Invalid PeriodType");

            return new StockExchangeHistory
            {
                CountryCode = countryCode.ToUpper(),
                Exchange = exchange.ToUpper(),
                IndexValue = indexValue,
                PeriodType = periodType,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}
