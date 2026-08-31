using BeyeCEO.Domain.Shared;
using System;

namespace BeyeCEO.Domain.KPIs.Entites
{
    public class BankPerformanceMetric : BaseEntity
    {
        public Guid BankId { get; private set; }
        public string Section { get; private set; } = string.Empty;
        public string GroupName { get; private set; } = string.Empty;
        public string KpiName { get; private set; } = string.Empty;
        public string KpiAlias { get; private set; } = string.Empty;
        public string KpiType { get; private set; } = string.Empty;

        public decimal? CurrentValue { get; private set; }

        public decimal? GrowthYTDValue { get; private set; }
        public decimal? GrowthYTDPct { get; private set; }
        public string? GrowthYTDIcon { get; private set; }

        public decimal? GrowthMTDValue { get; private set; }
        public decimal? GrowthMTDPct { get; private set; }
        public string? GrowthMTDIcon { get; private set; }

        public decimal? BudgetYTDPct { get; private set; }
        public string? BudgetYTDIcon { get; private set; }

        public DateOnly AsOfDate { get; private set; }
        public string? TrendData { get; private set; }  // JSON as-is
        public int SortOrder { get; private set; }
        public string Source { get; private set; } = "Beye";
        public DateTime RecordedAt { get; private set; }

        private BankPerformanceMetric() { }

        public static BankPerformanceMetric Create(
            Guid bankId, string section, string groupName,
            string kpiName, string kpiAlias, string kpiType,
            DateOnly asOfDate,
            decimal? currentValue = null,
            decimal? growthYtdValue = null, decimal? growthYtdPct = null, string? growthYtdIcon = null,
            decimal? growthMtdValue = null, decimal? growthMtdPct = null, string? growthMtdIcon = null,
            decimal? budgetYtdPct = null, string? budgetYtdIcon = null,
            string? trendData = null, int sortOrder = 0, string source = "Beye")
        {
            if (string.IsNullOrWhiteSpace(section))
                throw new ArgumentException("Section is required");

            if (string.IsNullOrWhiteSpace(kpiName))
                throw new ArgumentException("KpiName is required");

            return new BankPerformanceMetric
            {
                BankId = bankId,
                Section = section,
                GroupName = groupName,
                KpiName = kpiName,
                KpiAlias = kpiAlias,
                KpiType = kpiType,
                CurrentValue = currentValue,
                GrowthYTDValue = growthYtdValue,
                GrowthYTDPct = growthYtdPct,
                GrowthYTDIcon = growthYtdIcon,
                GrowthMTDValue = growthMtdValue,
                GrowthMTDPct = growthMtdPct,
                GrowthMTDIcon = growthMtdIcon,
                BudgetYTDPct = budgetYtdPct,
                BudgetYTDIcon = budgetYtdIcon,
                AsOfDate = asOfDate,
                TrendData = trendData,
                SortOrder = sortOrder,
                Source = source,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}
