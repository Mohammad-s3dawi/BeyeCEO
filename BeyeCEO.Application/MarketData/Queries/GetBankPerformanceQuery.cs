using BeyeCEO.Application.MarketData.DTOs;
using BeyeCEO.Domain.MarketData.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Application.MarketData.Queries
{
    // ── Query ─────────────────────────────────────────────────
    public record GetBankPerformanceQuery(
        Guid BankId, string Section
    ) : IRequest<BankPerformanceResponseDto?>;

    // ── Handler ───────────────────────────────────────────────
    public class GetBankPerformanceQueryHandler
        : IRequestHandler<GetBankPerformanceQuery, BankPerformanceResponseDto?>
    {
        private readonly IMarketDataRepository _repo;

        // ترتيب المجموعات: Assets → Liabilities → Equity
        private static readonly string[] GroupOrder =
            ["Assets", "Liabilities", "Equity"];

        public GetBankPerformanceQueryHandler(IMarketDataRepository repo)
        {
            _repo = repo;
        }

        public async Task<BankPerformanceResponseDto?> Handle(
            GetBankPerformanceQuery request, CancellationToken ct)
        {
            var metrics = await _repo.GetBankPerformanceAsync(
                request.BankId, request.Section);

            if (metrics.Count == 0) return null;

            var groups = metrics
                .GroupBy(x => x.GroupName)
                .OrderBy(g =>
                {
                    var idx = Array.IndexOf(GroupOrder, g.Key);
                    return idx >= 0 ? idx : int.MaxValue;
                })
                .Select(g => new BankPerformanceGroupDto
                {
                    GroupName = g.Key,
                    Kpis = g.OrderBy(x => x.SortOrder).Select(x => new BankPerformanceKpiDto
                    {
                        KpiAlias = x.KpiAlias,
                        KpiType = x.KpiType,
                        CurrentValue = x.CurrentValue,
                        GrowthYTDPct = x.GrowthYTDPct,
                        GrowthYTDIcon = x.GrowthYTDIcon,
                        GrowthMTDPct = x.GrowthMTDPct,
                        GrowthMTDIcon = x.GrowthMTDIcon,
                        BudgetYTDPct = x.BudgetYTDPct,
                        BudgetYTDIcon = x.BudgetYTDIcon,
                        AsOfDate = x.AsOfDate,
                        TrendData = x.TrendData,
                        SortOrder = x.SortOrder
                    })
                });

            return new BankPerformanceResponseDto
            {
                // أحدث تاريخ موجود بالدفعة — مش أول عنصر بس (بعض الـ KPIs بتتأخر)
                AsOfDate = metrics.Max(x => x.AsOfDate),
                BankId = request.BankId,
                Groups = groups
            };
        }
    }
}
