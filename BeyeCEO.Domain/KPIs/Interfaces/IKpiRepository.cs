using BeyeCEO.Domain.KPIs.Entites;
using BeyeCEO.Domain.Shared.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.KPIs.Interfaces
{
    public interface IKpiRepository
    {
        // ── Snapshots ─────────────────────────────────────────
        Task<IEnumerable<KpiSnapshot>> GetLatestByBankCountryAsync(Guid bankCountryId);
        Task<KpiSnapshot?> GetLatestAsync(Guid bankCountryId, string kpiCode);
        Task<IEnumerable<KpiSnapshot>> GetHistoryAsync(
            Guid bankCountryId, string kpiCode,
            DateOnly from, DateOnly to,
            PeriodType periodType = PeriodType.Monthly);

        // ── Definitions ───────────────────────────────────────
        Task<IEnumerable<KpiDefinition>> GetAllDefinitionsAsync();
        Task<KpiDefinition?> GetDefinitionAsync(string kpiCode);

        // ── Targets ───────────────────────────────────────────
        Task<IEnumerable<KpiTarget>> GetTargetsByBankCountryAsync(Guid bankCountryId, short year);
        Task<KpiTarget?> GetTargetAsync(Guid bankCountryId, string kpiCode,
            short year, byte? quarter = null);

        // ── Save ──────────────────────────────────────────────
        Task SaveSnapshotAsync(KpiSnapshot snapshot);
        Task SaveSnapshotRangeAsync(IEnumerable<KpiSnapshot> snapshots);
        Task SaveTargetAsync(KpiTarget target);
    }
}
