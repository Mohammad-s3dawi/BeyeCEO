using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class InterestRate : BaseEntity
    {
        public string Institution { get; private set; } = string.Empty;
        public string CountryCode { get; private set; } = string.Empty;  // ← جديد
        public string RateType { get; private set; } = string.Empty;
        public decimal Rate { get; private set; }
        public DateOnly EffectiveDate { get; private set; }
        public DateTime RecordedAt { get; private set; }

        // Navigation ← جديد
        public Country Country { get; private set; } = null!;

        private InterestRate() { }

        public static InterestRate Create(string institution, string countryCode,
            string rateType, decimal rate, DateOnly effectiveDate)
        {
            if (countryCode.Length != 2)
                throw new ArgumentException("CountryCode must be 2 characters");

            return new InterestRate
            {
                Institution = institution.ToUpper(),
                CountryCode = countryCode.ToUpper(),
                RateType = rateType,
                Rate = rate,
                EffectiveDate = effectiveDate,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}
