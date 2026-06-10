using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class GlobalIndex : BaseEntity
    {
        public string Symbol { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public decimal Value { get; private set; }
        public decimal Change { get; private set; }
        public decimal ChangePct { get; private set; }
        public string Region { get; private set; } = string.Empty;
        public string Source { get; private set; } = string.Empty;
        public DateTime RecordedAt { get; private set; }

        // ← جديد
        public long Volume { get; private set; }

        private GlobalIndex() { }

        public static GlobalIndex Create(
            string symbol, string name, decimal value,
            decimal change, decimal changePct,
            string region, string source,
            long volume = 0)
        {
            return new GlobalIndex
            {
                Symbol = symbol,
                Name = name,
                Value = value,
                Change = change,
                ChangePct = changePct,
                Region = region,
                Source = source,
                Volume = volume,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}
