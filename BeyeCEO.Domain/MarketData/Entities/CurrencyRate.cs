using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class CurrencyRate:BaseEntity
    {

        public string BaseCurrency { get; private set; } = string.Empty;
        public string QuoteCurrency { get; private set; } = string.Empty;
        public decimal Rate { get; private set; }
        public DateTime RecordedAt { get; private set; }
        public string Source { get; private set; } = string.Empty;

        private CurrencyRate() { }

        public static CurrencyRate Create(string baseCurrency, string quoteCurrency,
            decimal rate, string source)
        {
            if (rate <= 0)
                throw new ArgumentException("Rate must be greater than zero");

            return new CurrencyRate
            {
                BaseCurrency = baseCurrency.ToUpper(),
                QuoteCurrency = quoteCurrency.ToUpper(),
                Rate = rate,
                RecordedAt = DateTime.UtcNow,
                Source = source
            };
        }

        public string Pair => $"{BaseCurrency}/{QuoteCurrency}";
    }
}
