using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class CBJIndicator:BaseEntity
    {
        public string IndicatorCode { get; private set; } = string.Empty;
        public string IndicatorNameEN { get; private set; } = string.Empty;
        public string IndicatorNameAR { get; private set; } = string.Empty;
        public decimal Value { get; private set; }
        public string Unit { get; private set; } = string.Empty;
        public DateOnly PeriodDate { get; private set; }
        public DateTime RecordedAt { get; private set; }

        private CBJIndicator() { }

        public static CBJIndicator Create(string code, string nameEN, string nameAR,
            decimal value, string unit, DateOnly periodDate)
        {
            return new CBJIndicator
            {
                IndicatorCode = code.ToUpper(),
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
