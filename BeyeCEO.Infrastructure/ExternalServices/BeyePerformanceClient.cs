using BeyeCEO.Domain.KPIs.Entites;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class BeyePerformanceClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<BeyePerformanceClient> _logger;

        // raw kpiName (من الـ API) → مجموعة/مجموعات يظهر فيها + الاسم المعروض + الترتيب
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

        public async Task<List<BankPerformanceMetric>> FetchBSAsync(
            Guid bankId, string beyeApiUrl, string beyeApiKey)
        {
            var results = new List<BankPerformanceMetric>();

            try
            {
                var url = $"{beyeApiUrl.TrimEnd('/')}/api/CEODashboard/GetViewComponentsData" +
                          $"?viewKeyName=BankPerformanceCeoB/S";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // ما بعرف بعد إذا الـ API بده Authorization — لسا مفيش مفتاح حقيقي
                if (!string.IsNullOrWhiteSpace(beyeApiKey))
                    request.Headers.Add("Authorization", $"Bearer {beyeApiKey}");

                request.Headers.Add("Content-Type", "application/json");

                var response = await _http.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning(
                        "BeyePerformanceClient: 401 Unauthorized for bank {BankId} — " +
                        "API requires a real BeyeApiKey, skipping",
                        bankId);
                    return results;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "BeyePerformanceClient: HTTP {Status} for bank {BankId}",
                        (int)response.StatusCode, bankId);
                    return results;
                }

                var body = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(body);

                if (json.RootElement.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning(
                        "BeyePerformanceClient: unexpected response shape for bank {BankId}",
                        bankId);
                    return results;
                }

                // ← الـ API بترجع fact_finance_Banks_Deposits كعنصرين منفصلين بنفس
                // القيم (وحدة لكل مجموعة) — مش عنصر واحد بحتاج نفنّشه لصفين.
                // فبنتابع عدد مرات ظهور كل kpiName ونربط كل ظهور بالـ mapping يلي يقابله بالترتيب.
                var occurrenceCount = new Dictionary<string, int>();

                foreach (var item in json.RootElement.EnumerateArray())
                {
                    try
                    {
                        var kpiName = GetString(item, "kpiName");
                        if (string.IsNullOrEmpty(kpiName)) continue;

                        if (!BSMap.TryGetValue(kpiName, out var mappings)) continue;

                        var occurrenceIndex = occurrenceCount.GetValueOrDefault(kpiName, 0);
                        occurrenceCount[kpiName] = occurrenceIndex + 1;

                        if (occurrenceIndex >= mappings.Count)
                        {
                            _logger.LogWarning(
                                "BeyePerformanceClient: extra occurrence #{Index} of {KpiName} " +
                                "beyond the {Count} expected group(s) — skipping",
                                occurrenceIndex + 1, kpiName, mappings.Count);
                            continue;
                        }

                        var (groupName, alias, sortOrder) = mappings[occurrenceIndex];

                        if (!item.TryGetProperty("kpiResultDto", out var resultDtoArr) ||
                            resultDtoArr.ValueKind != JsonValueKind.Array ||
                            resultDtoArr.GetArrayLength() == 0)
                            continue;

                        var dto = resultDtoArr[0];

                        var kpiType = GetString(dto, "kpiType") ?? "amount";
                        var current = GetDecimal(dto, "current");
                        var asOfDate = GetDate(dto, "asOfDate")
                            ?? DateOnly.FromDateTime(DateTime.UtcNow);

                        decimal? growthYtdValue = null, growthYtdPct = null;
                        decimal? growthMtdValue = null, growthMtdPct = null;
                        string? growthYtdIcon = null, growthMtdIcon = null;

                        if (dto.TryGetProperty("growthKpi", out var growthKpi) &&
                            growthKpi.ValueKind != JsonValueKind.Null)
                        {
                            growthYtdValue = GetDecimal(growthKpi, "thisYearGrowthValue");
                            growthYtdPct = GetDecimal(growthKpi, "thisYearGrowthPercentage");
                            growthYtdIcon = GetString(growthKpi, "thisYearIcon");
                            growthMtdValue = GetDecimal(growthKpi, "thisMonthGrowthValue");
                            growthMtdPct = GetDecimal(growthKpi, "thisMonthGrowthPercentage");
                            growthMtdIcon = GetString(growthKpi, "thisMonthIcon");
                        }

                        decimal? budgetYtdPct = null;
                        string? budgetYtdIcon = null;

                        if (dto.TryGetProperty("budgetKpi", out var budgetKpi) &&
                            budgetKpi.ValueKind != JsonValueKind.Null)
                        {
                            budgetYtdPct = GetDecimal(budgetKpi, "ytdBudgetPercentage");
                            budgetYtdIcon = GetString(budgetKpi, "ytdColorIcon");
                        }

                        var trendData = BuildTrendJson(dto);

                        results.Add(BankPerformanceMetric.Create(
                            bankId: bankId,
                            section: "BS",
                            groupName: groupName,
                            kpiName: kpiName,
                            kpiAlias: alias,
                            kpiType: kpiType,
                            asOfDate: asOfDate,
                            currentValue: current,
                            growthYtdValue: growthYtdValue,
                            growthYtdPct: growthYtdPct,
                            growthYtdIcon: growthYtdIcon,
                            growthMtdValue: growthMtdValue,
                            growthMtdPct: growthMtdPct,
                            growthMtdIcon: growthMtdIcon,
                            budgetYtdPct: budgetYtdPct,
                            budgetYtdIcon: budgetYtdIcon,
                            trendData: trendData,
                            sortOrder: sortOrder,
                            source: "Beye"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            "BeyePerformanceClient: failed to parse item for bank {BankId}: {Message}",
                            bankId, ex.Message);
                    }
                }

                _logger.LogInformation(
                    "BeyePerformanceClient: ✅ {Count} metrics parsed for bank {BankId}",
                    results.Count, bankId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "BeyePerformanceClient: ❌ Failed for bank {BankId}: {Message}",
                    bankId, ex.Message);
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
    }
}
