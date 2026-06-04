using BeyeCEO.Domain.Shared.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.KPIs.Entites
{
    public class KpiDefinition
    {
        public string Code { get; private set; } = string.Empty;
        public string NameEN { get; private set; } = string.Empty;
        public string NameAR { get; private set; } = string.Empty;
        public string DescriptionEN { get; private set; } = string.Empty;
        public string DescriptionAR { get; private set; } = string.Empty;
        public string Unit { get; private set; } = string.Empty;
        public string Category { get; private set; } = string.Empty;
        public decimal? WarningThreshold { get; private set; }
        public decimal? AlertThreshold { get; private set; }
        public bool IsHigherBetter { get; private set; } = true;
        public bool IsActive { get; private set; } = true;

        private KpiDefinition() { }

        public static KpiDefinition Create(string code, string nameEN, string nameAR,
            string unit, string category, bool isHigherBetter,
            decimal? warningThreshold = null, decimal? alertThreshold = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("KPI code is required");

            return new KpiDefinition
            {
                Code = code.ToUpper(),
                NameEN = nameEN,
                NameAR = nameAR,
                Unit = unit,
                Category = category,
                IsHigherBetter = isHigherBetter,
                WarningThreshold = warningThreshold,
                AlertThreshold = alertThreshold,
                IsActive = true
            };
        }

        // Business Rule — يحدد حالة الـ KPI
        public KpiStatus EvaluateStatus(decimal value)
        {
            if (AlertThreshold.HasValue)
            {
                bool breachesAlert = IsHigherBetter
                    ? value < AlertThreshold.Value
                    : value > AlertThreshold.Value;

                if (breachesAlert) return KpiStatus.Critical;
            }

            if (WarningThreshold.HasValue)
            {
                bool breachesWarning = IsHigherBetter
                    ? value < WarningThreshold.Value
                    : value > WarningThreshold.Value;

                if (breachesWarning) return KpiStatus.Warning;
            }

            return KpiStatus.Good;
        }
    }
}
