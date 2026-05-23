using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class InterestRate:BaseEntity
    {
        public string Institution { get; private set; } = string.Empty;
        public string RateType { get; private set; } = string.Empty;
        public decimal Rate { get; private set; }
        public DateOnly EffectiveDate { get; private set; }
        public DateTime RecordedAt { get; private set; }

        private InterestRate() { }

        public static InterestRate Create(string institution, string rateType,
            decimal rate, DateOnly effectiveDate)
        {
            return new InterestRate
            {
                Institution = institution.ToUpper(),
                RateType = rateType,
                Rate = rate,
                EffectiveDate = effectiveDate,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}
