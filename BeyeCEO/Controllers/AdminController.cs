using BeyeCEO.API.Extensions;
using BeyeCEO.Application.MarketData.Commands;
using BeyeCEO.Infrastructure.NewsAggregation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BeyeCEO.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IConfiguration _config;
        private readonly NewsPipelineRunner _pipelineRunner;
        private readonly SourcesHealthTracker _health;
        private readonly OldRowsClassificationService _classifier;

        public AdminController(
            IMediator mediator,
            IConfiguration config,
            NewsPipelineRunner pipelineRunner,
            SourcesHealthTracker health,
            OldRowsClassificationService classifier)
        {
            _mediator = mediator;
            _config = config;
            _pipelineRunner = pipelineRunner;
            _health = health;
            _classifier = classifier;
        }

        // POST /api/admin/cbj-rates
        [HttpPost("cbj-rates")]
        public async Task<IActionResult> SaveCbjRates(
            [FromBody] SaveCbjRatesRequest request)
        {
            await _mediator.Send(new SaveCbjRatesCommand(
                request.CountryCode,
                request.MainRate,
                request.ReDiscountRate,
                request.RepoRate,
                request.DepositWindowRate,
                request.EffectiveDate));

            return Ok(new ApiResponse<string>(
                true, "CBJ rates saved successfully"));
        }

        // POST /api/admin/news/dry-run
        // Fetches every enabled feed in News:Feeds, parses it, runs the
        // banking keyword prefilter and cross-source near-duplicate check,
        // and returns diagnostics. Never writes to the database — safe to
        // call repeatedly.
        [HttpPost("news/dry-run")]
        public async Task<IActionResult> NewsDryRun()
        {
            var results = await _pipelineRunner.DryRunAllFeedsAsync();
            return Ok(new ApiResponse<IEnumerable<FeedFetchResult>>(true, results));
        }

        // POST /api/admin/news/run?group=local-rss|international|all&store=false
        // Runs the real pipeline (fetch -> age-skip -> filter -> dedupe ->
        // DB-existence check), optionally persisting. store=true is only
        // honored when News:Storage:Enabled=true in config; otherwise the
        // run still executes (and reports what it would have stored) but
        // nothing is written, with a note explaining why.
        [HttpPost("news/run")]
        public async Task<IActionResult> RunNewsPipeline(
            [FromQuery] string group = "all",
            [FromQuery] bool store = false)
        {
            var validGroups = new[] { "local-rss", "international", "all" };
            if (!validGroups.Contains(group, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new ApiResponse<string>(
                    false, "group must be one of: local-rss, international, all"));
            }

            var storageEnabled = _config.GetValue("News:Storage:Enabled", false);
            var effectiveStore = store && storageEnabled;

            var groups = await _pipelineRunner.RunAsync(group, effectiveStore);

            var response = new
            {
                requestedStore = store,
                storageEnabled,
                effectiveStore,
                note = store && !storageEnabled
                    ? "storage disabled — News:Storage:Enabled is false, so nothing was written even though store=true was requested"
                    : (string?)null,
                groups = groups.Select(g => new
                {
                    group = g.Group,
                    feeds = g.Feeds,
                    totals = new
                    {
                        fetched = g.TotalFetched,
                        tooOld = g.TotalTooOld,
                        rejected = g.TotalRejected,
                        passed = g.TotalPassed,
                        nearDuplicate = g.TotalNearDuplicate,
                        alreadyInDb = g.TotalAlreadyInDb,
                        skippedLongUrl = g.TotalSkippedLongUrl,
                        wouldStoreOrStored = g.TotalWouldStoreOrStored
                    },
                    sampleWouldStoreTitles = g.SampleWouldStoreTitles
                })
            };

            return Ok(new ApiResponse<object>(true, response));
        }

        // GET /api/admin/news/sources-health
        [HttpGet("news/sources-health")]
        public IActionResult NewsSourcesHealth()
        {
            return Ok(new ApiResponse<IEnumerable<SourceHealthEntry>>(true, _health.Snapshot()));
        }

        // GET /api/admin/news/classify-old?beforeUtc=2026-10-11T08:31:39
        // Step 9a — READ ONLY. Applies the current BankingKeywordFilter
        // retroactively to rows already in the database (CreatedAt <
        // beforeUtc) to report which old rows would be kept/removed under
        // today's rules. Never writes to the database.
        [HttpGet("news/classify-old")]
        public async Task<IActionResult> ClassifyOldRows(
            [FromQuery] DateTime? beforeUtc = null, [FromQuery] int keepCap = 150,
            [FromQuery] bool includeDeleted = false)
        {
            var cutoff = beforeUtc ?? new DateTime(2026, 10, 11, 8, 31, 39, DateTimeKind.Unspecified);
            var all = await _classifier.ClassifyAsync(cutoff, includeDeleted);

            var bySource = all
                .GroupBy(r => r.SourceName)
                .Select(g => new
                {
                    sourceName = g.Key,
                    total = g.Count(),
                    keep = g.Count(r => r.WouldKeep),
                    remove = g.Count(r => !r.WouldKeep),
                    keepPct = Math.Round(100.0 * g.Count(r => r.WouldKeep) / g.Count(), 1),
                    removePct = Math.Round(100.0 * g.Count(r => !r.WouldKeep) / g.Count(), 1)
                })
                .OrderByDescending(s => s.total)
                .ToList();

            var keepAll = all.Where(r => r.WouldKeep).ToList();
            var keepCapped = keepAll
                .OrderByDescending(r => r.PublishedAt)
                .Take(keepCap)
                .ToList();

            var removeAll = all.Where(r => !r.WouldKeep).ToList();

            const int seed = 90811; // fixed for reproducibility — see old-rows-remove-sample.md
            var quotaSources = new[] { "jo24.net", "BBC News", "The Guardian" };
            var sampleParts = new List<OldRowClassification>();
            foreach (var src in quotaSources)
            {
                var pool = removeAll.Where(r => r.SourceName == src);
                sampleParts.AddRange(OldRowsClassificationService.SeededSample(pool, 10, seed));
            }

            var remainingPool = removeAll.Where(r => !quotaSources.Contains(r.SourceName));
            sampleParts.AddRange(OldRowsClassificationService.SeededSample(remainingPool, 30, seed));

            var looseMismatches = removeAll
                .Where(r => r.LooksBankingLoose)
                .OrderByDescending(r => r.PublishedAt)
                .Take(40)
                .ToList();

            // Step 9d: when includeDeleted=true, also report how the NEW
            // filter verdict diverges from the CURRENT DB IsDeleted state —
            // i.e. which currently-active rows would now flip to REMOVE,
            // and which currently-soft-deleted rows would now flip to KEEP.
            object? flips = null;
            if (includeDeleted)
            {
                flips = new
                {
                    currentActive = all.Count(r => !r.IsDeleted),
                    currentDeleted = all.Count(r => r.IsDeleted),
                    activeFlippedToRemove = all.Where(r => !r.IsDeleted && !r.WouldKeep).ToList(),
                    deletedFlippedToKeep = all.Where(r => r.IsDeleted && r.WouldKeep).ToList()
                };
            }

            var response = new
            {
                cutoffUtc = cutoff,
                includeDeleted,
                totalRows = all.Count,
                totalKeep = keepAll.Count,
                totalRemove = removeAll.Count,
                flips,
                bySource,
                keep = new { cap = keepCap, shown = keepCapped.Count, note = keepAll.Count > keepCap ? $"showing {keepCap} newest" : "all shown", rows = keepCapped },
                removeSample = new { seed, size = sampleParts.Count, quota = "10 jo24.net + 10 BBC News + 10 The Guardian + 30 random across other sources", rows = sampleParts },
                looseRecallMismatches = new { cap = 40, shown = looseMismatches.Count, rows = looseMismatches },
                // Full id list for the would-REMOVE set — used only to build
                // the (not-run) cleanup/restore SQL scripts, not part of the
                // human-facing report.
                removeAllIds = removeAll.Select(r => r.Id).ToList()
            };

            return Ok(new ApiResponse<object>(true, response));
        }
    }

    // ── Request Models ────────────────────────────────────────
    public record SaveCbjRatesRequest(
        string CountryCode,
        decimal MainRate,
        decimal ReDiscountRate,
        decimal RepoRate,
        decimal DepositWindowRate,
        DateOnly EffectiveDate);
}
