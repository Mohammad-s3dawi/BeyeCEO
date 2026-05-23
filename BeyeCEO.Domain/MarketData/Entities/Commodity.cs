using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class Commodity:BaseEntity
    {
        public string Symbol { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public string Currency { get; private set; } = string.Empty;
        public decimal ChangePct { get; private set; }
        public DateTime RecordedAt { get; private set; }
        public string Source { get; private set; } = string.Empty;

        private Commodity() { }

        public static Commodity Create(string symbol, string name, decimal price,
            string currency, decimal changePct, string source)
        {
            if (price < 0)
                throw new ArgumentException("Price cannot be negative");

            return new Commodity
            {
                Symbol = symbol.ToUpper(),
                Name = name,
                Price = price,
                Currency = currency.ToUpper(),
                ChangePct = changePct,
                RecordedAt = DateTime.UtcNow,
                Source = source
            };
        }
    }
}
