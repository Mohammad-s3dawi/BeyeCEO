using System;
using System.Collections.Generic;

namespace BeyeCEO.Application.MarketData.DTOs
{
    public class BankPerformanceResponseDto
    {
        public DateOnly AsOfDate { get; set; }
        public Guid BankId { get; set; }
        public IEnumerable<BankPerformanceGroupDto> Groups { get; set; } = [];
    }

    public class BankPerformanceGroupDto
    {
        public string GroupName { get; set; } = string.Empty;
        public IEnumerable<BankPerformanceKpiDto> Kpis { get; set; } = [];
    }

    public class BankPerformanceKpiDto
    {
        public string KpiAlias { get; set; } = string.Empty;
        public string KpiType { get; set; } = string.Empty;
        public decimal? CurrentValue { get; set; }
        public decimal? GrowthYTDPct { get; set; }
        public string? GrowthYTDIcon { get; set; }
        public decimal? GrowthMTDPct { get; set; }
        public string? GrowthMTDIcon { get; set; }
        public decimal? BudgetYTDPct { get; set; }
        public string? BudgetYTDIcon { get; set; }
        public DateOnly AsOfDate { get; set; }
        public string? TrendData { get; set; }
        public int SortOrder { get; set; }
    }
}
