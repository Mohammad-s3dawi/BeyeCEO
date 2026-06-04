using BeyeCEO.Domain.MarketData.Entities;
using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.KPIs.Entites
{
    public class KpiTarget:BaseEntity
    {
        public Guid BankCountryId { get; private set; }
        public string KpiCode { get; private set; } = string.Empty;
        public decimal TargetValue { get; private set; }
        public short Year { get; private set; }
        public byte? Quarter { get; private set; }   // null = سنوي
        public BankCountry BankCountry { get; private set; } = null!;

        private KpiTarget() { }

        public static KpiTarget Create(Guid bankCountryId, string kpiCode,
            decimal targetValue, short year, byte? quarter = null)
        {
            if (quarter.HasValue && (quarter < 1 || quarter > 4))
                throw new ArgumentException("Quarter must be between 1 and 4");

            return new KpiTarget
            {
                BankCountryId = bankCountryId,
                KpiCode = kpiCode.ToUpper(),
                TargetValue = targetValue,
                Year = year,
                Quarter = quarter
            };
        }

        public bool IsAnnual => Quarter == null;
        public string PeriodLabel => Quarter.HasValue ? $"Q{Quarter} {Year}" : $"{Year}";
    }
}
