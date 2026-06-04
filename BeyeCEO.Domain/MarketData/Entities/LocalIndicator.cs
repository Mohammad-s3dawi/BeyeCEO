using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class LocalIndicator: BaseEntity
    {
        public string CountryCode { get; private set; } = string.Empty;
        public string IndicatorCode { get; private set; } = string.Empty;   // 'MAIN_RATE', 'INFLATION'
        public string IndicatorNameEN { get; private set; } = string.Empty;
        public string IndicatorNameAR { get; private set; } = string.Empty;
        public decimal Value { get; private set; }
        public string Unit { get; private set; } = string.Empty;            // '%', 'JOD Million'
        public DateOnly PeriodDate { get; private set; }
        public DateTime RecordedAt { get; private set; }

        // Navigation
        public Country Country { get; private set; } = null!;

        private LocalIndicator() { }

        public static LocalIndicator Create(string countryCode, string indicatorCode,
            string nameEN, string nameAR, decimal value, string unit, DateOnly periodDate)
        {
            if (countryCode.Length != 2)
                throw new ArgumentException("CountryCode must be 2 characters");

            if (string.IsNullOrWhiteSpace(indicatorCode))
                throw new ArgumentException("IndicatorCode is required");

            return new LocalIndicator
            {
                CountryCode = countryCode.ToUpper(),
                IndicatorCode = indicatorCode.ToUpper(),
                IndicatorNameEN = nameEN,
                IndicatorNameAR = nameAR,
                Value = value,
                Unit = unit,
                PeriodDate = periodDate,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}
