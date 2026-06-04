using BeyeCEO.Domain.KPIs.Entites;
using BeyeCEO.Domain.KPIs.Interfaces;
using BeyeCEO.Domain.Shared.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Repositories
{
    public class KpiRepository : IKpiRepository
    {
        private readonly BeyeCeoDbContext _context;

        public KpiRepository(BeyeCeoDbContext context)
        {
            _context = context;
        }

        // ── Snapshots ─────────────────────────────────────────

        public async Task<IEnumerable<KpiSnapshot>> GetLatestByBankCountryAsync(
            Guid bankCountryId)
        {
            // آخر قيمة لكل KPI للفرع المحدد
            return await _context.KpiSnapshots
                .Where(x => x.BankCountryId == bankCountryId && !x.IsDeleted)
                .GroupBy(x => x.KpiCode)
                .Select(g => g.OrderByDescending(x => x.PeriodDate).First())
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<KpiSnapshot?> GetLatestAsync(
            Guid bankCountryId, string kpiCode)
        {
            return await _context.KpiSnapshots
                .Where(x =>
                    x.BankCountryId == bankCountryId &&
                    x.KpiCode == kpiCode &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.PeriodDate)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<KpiSnapshot>> GetHistoryAsync(
            Guid bankCountryId, string kpiCode,
            DateOnly from, DateOnly to,
            PeriodType periodType = PeriodType.Monthly)
        {
            return await _context.KpiSnapshots
                .Where(x =>
                    x.BankCountryId == bankCountryId &&
                    x.KpiCode == kpiCode &&
                    x.PeriodType == periodType &&
                    x.PeriodDate >= from &&
                    x.PeriodDate <= to &&
                    !x.IsDeleted)
                .OrderBy(x => x.PeriodDate)
                .AsNoTracking()
                .ToListAsync();
        }

        // ── Definitions ───────────────────────────────────────

        public async Task<IEnumerable<KpiDefinition>> GetAllDefinitionsAsync()
        {
            return await _context.KpiDefinitions
                .Where(x => x.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<KpiDefinition?> GetDefinitionAsync(string kpiCode)
        {
            return await _context.KpiDefinitions
                .Where(x => x.Code == kpiCode && x.IsActive)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        // ── Targets ───────────────────────────────────────────

        public async Task<IEnumerable<KpiTarget>> GetTargetsByBankCountryAsync(
            Guid bankCountryId, short year)
        {
            return await _context.KpiTargets
                .Where(x =>
                    x.BankCountryId == bankCountryId &&
                    x.Year == year)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<KpiTarget?> GetTargetAsync(
            Guid bankCountryId, string kpiCode,
            short year, byte? quarter = null)
        {
            return await _context.KpiTargets
                .Where(x =>
                    x.BankCountryId == bankCountryId &&
                    x.KpiCode == kpiCode &&
                    x.Year == year &&
                    x.Quarter == quarter)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        // ── Save ──────────────────────────────────────────────

        public async Task SaveSnapshotAsync(KpiSnapshot snapshot)
        {
            await _context.KpiSnapshots.AddAsync(snapshot);
            await _context.SaveChangesAsync();
        }

        public async Task SaveSnapshotRangeAsync(IEnumerable<KpiSnapshot> snapshots)
        {
            await _context.KpiSnapshots.AddRangeAsync(snapshots);
            await _context.SaveChangesAsync();
        }

        public async Task SaveTargetAsync(KpiTarget target)
        {
            // Upsert — لو موجود لنفس البنك + KPI + سنة + ربع
            var existing = await _context.KpiTargets
                .FirstOrDefaultAsync(x =>
                    x.BankCountryId == target.BankCountryId &&
                    x.KpiCode == target.KpiCode &&
                    x.Year == target.Year &&
                    x.Quarter == target.Quarter);

            if (existing != null)
                _context.KpiTargets.Remove(existing);

            await _context.KpiTargets.AddAsync(target);
            await _context.SaveChangesAsync();
        }
    }
}
