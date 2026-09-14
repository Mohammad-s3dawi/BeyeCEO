using BeyeCEO.Domain.KPIs.Entites;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class BeyePerformanceClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<BeyePerformanceClient> _logger;

        // raw kpiName (من الـ API) → مجموعة/مجموعات يظهر فيها + الاسم المعروض + الترتيب — قسم BS بس
        // ← KpiType منجيبها لحالها من kpiResultDto[0].kpiType مش من هون
        // ← fact_finance_Banks_Deposits بتظهر بمجموعتين مختلفتين (Assets/Liabilities) بنفس المفتاح الخام — مقصودة
        private static readonly Dictionary<string, List<(string GroupName, string Alias, int SortOrder)>>
            BSMap = new()
            {
                ["fact_finance_Total_Assets"] =
                    [("Assets", "Total Assets", 0)],
                ["fact_finance_Cash_Equivalents"] =
                    [("Assets", "Cash & EQVL", 1)],
                ["fact_finance_Loans_Advances"] =
                    [("Assets", "Credit Facilities", 3)],
                ["fact_finance_Total_Investment"] =
                    [("Assets", "Total Investment", 4)],

                ["fact_finance_Total_Liabilities"] =
                    [("Liabilities", "Total Liabilities", 0)],
                ["fact_finance_BorrowingsLiabilities"] =
                    [("Liabilities", "Borrowings", 2)],
                ["fact_deposit_CASA_Balance"] =
                    [("Liabilities", "CASA Balance", 3)],
                ["fact_deposit_TimeDeposit_Balance"] =
                    [("Liabilities", "Time Deposit Balance", 4)],

                ["fact_finance_Total_Equity"] =
                    [("Equity", "Total Equity", 0)],
                ["fact_finance_Paid_inCapital"] =
                    [("Equity", "Paid Up Capital", 1)],
                ["fact_finance_Retained_Earnings"] =
                    [("Equity", "Retained Earnings", 2)],
                ["fact_finance_OperationalRiskLossRatio"] =
                    [("Equity", "Operational Risk Loss Ratio", 3)],

                ["fact_finance_Banks_Deposits"] =
                [
                    ("Assets", "Account at Banks", 2),
                    ("Liabilities", "Banks Deposits", 1)
                ],
            };

        public BeyePerformanceClient(HttpClient http, ILogger<BeyePerformanceClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        // ── Fetch — بس بيجيب ويحلل الـ JSON، بدون أي منطق تجميع/تسمية ─────

        public async Task<List<RawKpiResult>> FetchViewDataAsync(
            string beyeApiUrl, int viewId, string beyeApiKey)
        {
            var results = new List<RawKpiResult>();

            try
            {
                var url = $"{beyeApiUrl.TrimEnd('/')}/api/Branch360Cache/RunGetAllComponent" +
                          $"?viewId={viewId}&UseOtherKpi=false&useBalanceEq=false&firstCache=false";

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                request.Headers.Add("X-Culture", "en-us");

                if (!string.IsNullOrWhiteSpace(beyeApiKey))
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", beyeApiKey);

                request.Content = new StringContent(
                    "[]", Encoding.UTF8, "application/json-patch+json");

                var response = await _http.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning(
                        "BeyePerformanceClient: 401 Unauthorized for viewId {ViewId} — " +
                        "API requires a real BeyeApiKey, skipping",
                        viewId);
                    return results;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "BeyePerformanceClient: HTTP {Status} for viewId {ViewId}",
                        (int)response.StatusCode, viewId);
                    return results;
                }

                var body = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(body);

                if (json.RootElement.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning(
                        "BeyePerformanceClient: unexpected response shape for viewId {ViewId}",
                        viewId);
                    return results;
                }

                foreach (var item in json.RootElement.EnumerateArray())
                {
                    try
                    {
                        var kpiName = GetString(item, "kpiName");
                        if (string.IsNullOrEmpty(kpiName)) continue;

                        var hasResultDto =
                            item.TryGetProperty("kpiResultDto", out var resultDtoArr) &&
                            resultDtoArr.ValueKind == JsonValueKind.Array &&
                            resultDtoArr.GetArrayLength() > 0;

                        if (!hasResultDto)
                        {
                            // كومبوننت من نوع تاني (مثلاً KPIWithDimensionGrid لقسم BusinessLine) —
                            // kpiResultDto فاضية دايماً، والقيم الحقيقية جوا
                            // kpiWithDimensionDataMonthlyGrowthBudget بدل هيك
                            if (item.TryGetProperty(
                                    "kpiWithDimensionDataMonthlyGrowthBudget", out var dimArr) &&
                                dimArr.ValueKind == JsonValueKind.Array &&
                                dimArr.GetArrayLength() > 0)
                            {
                                results.Add(BuildDimensionGridResult(item, kpiName, dimArr));
                            }

                            continue;
                        }

                        var dto = resultDtoArr[0];

                        var raw = new RawKpiResult
                        {
                            KpiName = kpiName,
                            ContainerId = GetString(item, "containerId"),
                            KpiAlias = GetString(dto, "kpiAlias") ?? kpiName,
                            KpiType = GetString(dto, "kpiType") ?? "amount",
                            Current = GetDecimal(dto, "current"),
                            AsOfDate = GetDate(dto, "asOfDate")
                                ?? DateOnly.FromDateTime(DateTime.UtcNow),
                        };

                        if (dto.TryGetProperty("growthKpi", out var growthKpi) &&
                            growthKpi.ValueKind != JsonValueKind.Null)
                        {
                            raw.GrowthYtdValue = GetDecimal(growthKpi, "thisYearGrowthValue");
                            raw.GrowthYtdPct = GetDecimal(growthKpi, "thisYearGrowthPercentage");
                            raw.GrowthYtdIcon = GetString(growthKpi, "thisYearIcon");
                            raw.GrowthMtdValue = GetDecimal(growthKpi, "thisMonthGrowthValue");
                            raw.GrowthMtdPct = GetDecimal(growthKpi, "thisMonthGrowthPercentage");
                            raw.GrowthMtdIcon = GetString(growthKpi, "thisMonthIcon");
                        }

                        if (dto.TryGetProperty("budgetKpi", out var budgetKpi) &&
                            budgetKpi.ValueKind != JsonValueKind.Null)
                        {
                            raw.BudgetYtdPct = GetDecimal(budgetKpi, "ytdBudgetPercentage");
                            raw.BudgetYtdIcon = GetString(budgetKpi, "ytdColorIcon");
                        }

                        raw.TrendJson = BuildTrendJson(dto);

                        results.Add(raw);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            "BeyePerformanceClient: failed to parse item for viewId {ViewId}: {Message}",
                            viewId, ex.Message);
                    }
                }

                _logger.LogInformation(
                    "BeyePerformanceClient: ✅ {Count} raw KPIs parsed for viewId {ViewId}",
                    results.Count, viewId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "BeyePerformanceClient: ❌ Failed for viewId {ViewId}: {Message}",
                    viewId, ex.Message);
            }

            return results;
        }

        // بناء RawKpiResult من كومبوننت KPIWithDimensionGrid (مثلاً BusinessLine) —
        // القيمة الحقيقية موزّعة على صفوف dimDesc (Retail/SME/Corporate/...)
        // مش بقيمة واحدة زي باقي الأقسام
        private static RawKpiResult BuildDimensionGridResult(
            JsonElement item, string kpiName, JsonElement dimArr)
        {
            var raw = new RawKpiResult
            {
                KpiName = kpiName,
                ContainerId = GetString(item, "containerId"),
                KpiAlias = GetString(item, "kpiAlias") ?? kpiName,
                IsDimensionGrid = true,
                DimensionRows = new List<DimensionRow>()
            };

            foreach (var row in dimArr.EnumerateArray())
            {
                var dimRow = new DimensionRow
                {
                    DimDesc = GetString(row, "dimDesc") ?? string.Empty,
                    Value = GetDecimal(row, "value") ?? 0
                };

                if (row.TryGetProperty("growthKpi", out var growthKpi) &&
                    growthKpi.ValueKind != JsonValueKind.Null)
                {
                    dimRow.GrowthYtdPct = GetDecimal(growthKpi, "thisYearGrowthPercentage");
                    dimRow.GrowthYtdIcon = GetString(growthKpi, "thisYearIcon");
                }

                if (row.TryGetProperty("budgetKpi", out var budgetKpi) &&
                    budgetKpi.ValueKind != JsonValueKind.Null)
                {
                    dimRow.BudgetYtdPct = GetDecimal(budgetKpi, "ytdBudgetPercentage");
                    dimRow.BudgetYtdIcon = GetString(budgetKpi, "ytdColorIcon");
                }

                var rowKpiType = GetString(row, "kpiType");
                if (!string.IsNullOrEmpty(rowKpiType))
                    raw.KpiType = rowKpiType;

                raw.DimensionRows.Add(dimRow);
            }

            raw.AsOfDate = GetLatestTrendDate(dimArr)
                ?? DateOnly.FromDateTime(DateTime.UtcNow);

            return raw;
        }

        // آخر تاريخ trend متوفر — من مصفوفة "data" تبع أول صف dimDesc، ما فيه
        // asOfDate على مستوى الـ item ولا على مستوى صف الـ dimension نفسه
        private static DateOnly? GetLatestTrendDate(JsonElement dimArr)
        {
            if (dimArr.GetArrayLength() == 0) return null;

            var first = dimArr[0];
            if (!first.TryGetProperty("data", out var dataArr) ||
                dataArr.ValueKind != JsonValueKind.Array ||
                dataArr.GetArrayLength() == 0)
                return null;

            var last = dataArr[dataArr.GetArrayLength() - 1];
            var rawDate = GetString(last, "date");

            return DateTime.TryParse(rawDate, out var d)
                ? DateOnly.FromDateTime(d)
                : null;
        }

        // ── Map — يحوّل النتائج الخام لـ entities، حسب الـ Section ──────

        public List<BankPerformanceMetric> MapToMetrics(
            List<RawKpiResult> raw, Guid bankId, string section)
        {
            if (section.Equals("BS", StringComparison.OrdinalIgnoreCase))
                return MapBS(raw, bankId);

            if (raw.Count > 0 && raw.All(r => r.IsDimensionGrid))
                return MapDimensionGrid(raw, bankId, section);

            return MapGeneric(raw, bankId, section);
        }

        // BS — نفس الـ mapping الثابت المُتحقق منه، مع معالجة الظهور المزدوج لبعض المفاتيح
        private List<BankPerformanceMetric> MapBS(List<RawKpiResult> raw, Guid bankId)
        {
            var results = new List<BankPerformanceMetric>();
            var occurrenceCount = new Dictionary<string, int>();

            foreach (var item in raw)
            {
                if (!BSMap.TryGetValue(item.KpiName, out var mappings)) continue;

                var occurrenceIndex = occurrenceCount.GetValueOrDefault(item.KpiName, 0);
                occurrenceCount[item.KpiName] = occurrenceIndex + 1;

                if (occurrenceIndex >= mappings.Count)
                {
                    _logger.LogWarning(
                        "BeyePerformanceClient: extra occurrence #{Index} of {KpiName} " +
                        "beyond the {Count} expected group(s) — skipping",
                        occurrenceIndex + 1, item.KpiName, mappings.Count);
                    continue;
                }

                var (groupName, alias, sortOrder) = mappings[occurrenceIndex];

                results.Add(BankPerformanceMetric.Create(
                    bankId: bankId,
                    section: "BS",
                    groupName: groupName,
                    kpiName: item.KpiName,
                    kpiAlias: alias,
                    kpiType: item.KpiType,
                    asOfDate: item.AsOfDate,
                    currentValue: item.Current,
                    growthYtdValue: item.GrowthYtdValue,
                    growthYtdPct: item.GrowthYtdPct,
                    growthYtdIcon: item.GrowthYtdIcon,
                    growthMtdValue: item.GrowthMtdValue,
                    growthMtdPct: item.GrowthMtdPct,
                    growthMtdIcon: item.GrowthMtdIcon,
                    budgetYtdPct: item.BudgetYtdPct,
                    budgetYtdIcon: item.BudgetYtdIcon,
                    trendData: item.TrendJson,
                    sortOrder: sortOrder,
                    source: "Beye"));
            }

            return results;
        }

        // PL / أي قسم تاني — بنستخدم containerId يلي راجعة من الـ API نفسها كـ GroupName
        // (تأكدنا منها بالفعل ضد بيانات PL حقيقية: Revenue / Expenses / Net Income)
        // بدل ما نخمّن prefix من الـ kpiAlias
        private List<BankPerformanceMetric> MapGeneric(
            List<RawKpiResult> raw, Guid bankId, string section)
        {
            var results = new List<BankPerformanceMetric>();
            var groupSortCounters = new Dictionary<string, int>();

            foreach (var item in raw)
            {
                var groupName = string.IsNullOrWhiteSpace(item.ContainerId)
                    ? "General"
                    : item.ContainerId;

                var sortOrder = groupSortCounters.GetValueOrDefault(groupName, 0);
                groupSortCounters[groupName] = sortOrder + 1;

                results.Add(BankPerformanceMetric.Create(
                    bankId: bankId,
                    section: section,
                    groupName: groupName,
                    kpiName: item.KpiName,
                    kpiAlias: item.KpiAlias,
                    kpiType: item.KpiType,
                    asOfDate: item.AsOfDate,
                    currentValue: item.Current,
                    growthYtdValue: item.GrowthYtdValue,
                    growthYtdPct: item.GrowthYtdPct,
                    growthYtdIcon: item.GrowthYtdIcon,
                    growthMtdValue: item.GrowthMtdValue,
                    growthMtdPct: item.GrowthMtdPct,
                    growthMtdIcon: item.GrowthMtdIcon,
                    budgetYtdPct: item.BudgetYtdPct,
                    budgetYtdIcon: item.BudgetYtdIcon,
                    trendData: item.TrendJson,
                    sortOrder: sortOrder,
                    source: "Beye"));
            }

            return results;
        }

        // BusinessLine (وأي قسم تاني بشكل KPIWithDimensionGrid) — القيمة الإجمالية
        // = مجموع كل صفوف الـ dimDesc، والتفصيل نفسه بيتخزن بالـ TrendData
        // (مش trend زمني حقيقي، بس بنعيد استخدام نفس العمود للـ breakdown)
        private List<BankPerformanceMetric> MapDimensionGrid(
            List<RawKpiResult> raw, Guid bankId, string section)
        {
            var results = new List<BankPerformanceMetric>();
            var sortOrder = 0;

            foreach (var item in raw)
            {
                if (item.DimensionRows == null || item.DimensionRows.Count == 0) continue;

                var currentValue = item.DimensionRows.Sum(d => d.Value);
                var first = item.DimensionRows[0];

                var breakdown = item.DimensionRows.Select(d => new DimensionBreakdownPoint
                {
                    Date = d.DimDesc,
                    Value = d.Value,
                    GrowthPct = d.GrowthYtdPct,
                    GrowthIcon = d.GrowthYtdIcon,
                    BudgetPct = d.BudgetYtdPct,
                    BudgetIcon = d.BudgetYtdIcon
                });

                results.Add(BankPerformanceMetric.Create(
                    bankId: bankId,
                    section: section,
                    groupName: "BusinessLine",
                    kpiName: item.KpiName,
                    kpiAlias: item.KpiAlias,
                    kpiType: item.KpiType,
                    asOfDate: item.AsOfDate,
                    currentValue: currentValue,
                    growthYtdValue: null,
                    growthYtdPct: first.GrowthYtdPct,
                    growthYtdIcon: first.GrowthYtdIcon,
                    growthMtdValue: null,
                    growthMtdPct: null,
                    growthMtdIcon: null,
                    budgetYtdPct: first.BudgetYtdPct,
                    budgetYtdIcon: first.BudgetYtdIcon,
                    trendData: JsonSerializer.Serialize(breakdown),
                    sortOrder: sortOrder++,
                    source: "Beye"));
            }

            return results;
        }

        private static string? BuildTrendJson(JsonElement dto)
        {
            if (!dto.TryGetProperty("data", out var dataArr) ||
                dataArr.ValueKind != JsonValueKind.Array)
                return null;

            var points = new List<TrendPoint>();

            foreach (var point in dataArr.EnumerateArray())
            {
                var rawDate = GetString(point, "date");
                var formattedDate = DateTime.TryParse(rawDate, out var d)
                    ? d.ToString("yyyy-MM-dd")
                    : rawDate ?? string.Empty;

                points.Add(new TrendPoint
                {
                    Date = formattedDate,
                    Value = GetDecimal(point, "total") ?? 0
                });
            }

            return JsonSerializer.Serialize(points);
        }

        private static decimal? GetDecimal(JsonElement el, string prop)
        {
            if (!el.TryGetProperty(prop, out var v) || v.ValueKind == JsonValueKind.Null)
                return null;

            return v.ValueKind switch
            {
                JsonValueKind.Number => v.GetDecimal(),
                JsonValueKind.String => decimal.TryParse(v.GetString(), out var d) ? d : null,
                _ => null
            };
        }

        private static string? GetString(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null
                ? v.GetString()
                : null;

        private static DateOnly? GetDate(JsonElement el, string prop)
        {
            var s = GetString(el, prop);
            return DateOnly.TryParse(s, out var d) ? d : null;
        }

        private class TrendPoint
        {
            public string Date { get; set; } = string.Empty;
            public decimal Value { get; set; }
        }

        // صف breakdown واحد بالـ TrendData تبع BusinessLine — مش نقطة زمنية حقيقية
        private class DimensionBreakdownPoint
        {
            [JsonPropertyName("date")]
            public string Date { get; set; } = string.Empty;

            [JsonPropertyName("value")]
            public decimal Value { get; set; }

            [JsonPropertyName("growthPct")]
            public decimal? GrowthPct { get; set; }

            [JsonPropertyName("growthIcon")]
            public string? GrowthIcon { get; set; }

            [JsonPropertyName("budgetPct")]
            public decimal? BudgetPct { get; set; }

            [JsonPropertyName("budgetIcon")]
            public string? BudgetIcon { get; set; }
        }
    }

    // نتيجة KPI خام بعد الباد فقط — بدون أي ربط بمجموعة/ترتيب بعد
    public class RawKpiResult
    {
        public string KpiName { get; set; } = string.Empty;
        public string? ContainerId { get; set; }
        public string KpiAlias { get; set; } = string.Empty;
        public string KpiType { get; set; } = "amount";
        public decimal? Current { get; set; }
        public DateOnly AsOfDate { get; set; }
        public decimal? GrowthYtdValue { get; set; }
        public decimal? GrowthYtdPct { get; set; }
        public string? GrowthYtdIcon { get; set; }
        public decimal? GrowthMtdValue { get; set; }
        public decimal? GrowthMtdPct { get; set; }
        public string? GrowthMtdIcon { get; set; }
        public decimal? BudgetYtdPct { get; set; }
        public string? BudgetYtdIcon { get; set; }
        public string? TrendJson { get; set; }

        // كومبوننت KPIWithDimensionGrid (BusinessLine) — القيمة موزّعة على DimensionRows
        // بدل قيمة واحدة، kpiResultDto بتكون فاضية دايماً بهالحالة
        public bool IsDimensionGrid { get; set; }
        public List<DimensionRow>? DimensionRows { get; set; }
    }

    // صف واحد من kpiWithDimensionDataMonthlyGrowthBudget (مثلاً "Retail")
    public class DimensionRow
    {
        public string DimDesc { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal? GrowthYtdPct { get; set; }
        public string? GrowthYtdIcon { get; set; }
        public decimal? BudgetYtdPct { get; set; }
        public string? BudgetYtdIcon { get; set; }
    }
}
