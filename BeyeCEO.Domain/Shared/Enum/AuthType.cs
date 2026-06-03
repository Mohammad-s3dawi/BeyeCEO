using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.Shared.Enum
{
    public enum AuthType
    {
        Local = 1,
        Windows = 2
    }

    public enum UserRole
    {
        CEO = 1,
        Admin = 2
    }

    public enum Language
    {
        EN = 1,
        AR = 2
    }

    public enum PeriodType
    {
        Monthly = 1,
        Quarterly = 2,
        Annual = 3
    }
}
