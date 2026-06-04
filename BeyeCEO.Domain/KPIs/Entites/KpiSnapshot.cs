using BeyeCEO.Domain.MarketData.Entities;
using BeyeCEO.Domain.Shared;
using BeyeCEO.Domain.Shared.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.KPIs.Entites
{
    public class KpiSnapshot:BaseEntity
    {
        public Guid BankCountryId { get; private set; }
        public string KpiCode { get; private set; } = string.Empty;
        public decimal Value { get; private set; }
        public decimal? TargetValue { get; private set; }
        public decimal? PreviousValue { get; private set; }
        public DateTime RecordedAt { get; private set; }
        public DateOnly PeriodDate { get; private set; }
        public PeriodType PeriodType { get; private set; }
        public string Source { get; private set; } = "ETL";

        // Navigation
        public BankCountry BankCountry { get; private set; } = null!;

        private KpiSnapshot() { }

        public static KpiSnapshot Create(Guid bankCountryId, string kpiCode,
            decimal value, DateOnly periodDate, PeriodType periodType,
            decimal? targetValue = null, decimal? previousValue = null,
            string source = "ETL")
        {
            return new KpiSnapshot
            {
                BankCountryId = bankCountryId,
                KpiCode = kpiCode.ToUpper(),
                Value = value,
                TargetValue = targetValue,
                PreviousValue = previousValue,
                RecordedAt = DateTime.UtcNow,
                PeriodDate = periodDate,
                PeriodType = periodType,
                Source = source
            };
        }

        // Business Rules
        public TrendDirection GetTrend()
        {
            if (!PreviousValue.HasValue) return TrendDirection.Stable;
            if (Value > PreviousValue.Value) return TrendDirection.Up;
            if (Value < PreviousValue.Value) return TrendDirection.Down;
            return TrendDirection.Stable;
        }

        public bool IsAboveTarget()
            => TargetValue.HasValue && Value >= TargetValue.Value;

        public decimal? VarianceFromTarget()
            => TargetValue.HasValue ? Value - TargetValue.Value : null;

    }
}
