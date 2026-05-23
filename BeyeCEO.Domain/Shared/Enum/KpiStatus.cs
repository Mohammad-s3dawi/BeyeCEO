using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.Shared.Enum
{
    public enum KpiStatus
    {
        Good = 1,
        Warning = 2,
        Critical = 3,
        NoData = 4
    }
    public enum TrendDirection
    {
        Up = 1,
        Down = 2,
        Stable = 3
    }

    public enum Period
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annual
    }
}
