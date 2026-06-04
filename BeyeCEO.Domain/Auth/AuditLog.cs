using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.Auth
{
    public class AuditLog
    {
        public long Id { get; private set; }  // BIGINT
        public Guid? UserId { get; private set; }
        public Guid? BankId { get; private set; }
        public string Action { get; private set; } = string.Empty;
        public string Resource { get; private set; } = string.Empty;
        public string? CountryCode { get; private set; }
        public string IpAddress { get; private set; } = string.Empty;
        public string UserAgent { get; private set; } = string.Empty;
        public bool IsSuccess { get; private set; } = true;
        public string? ErrorMsg { get; private set; }
        public DateTime Timestamp { get; private set; }

        private AuditLog() { }

        public static AuditLog Create(string action, string resource,
            string ipAddress, Guid? userId = null, Guid? bankId = null,
            string? countryCode = null, string userAgent = "",
            bool isSuccess = true, string? errorMsg = null)
        {
            return new AuditLog
            {
                UserId = userId,
                BankId = bankId,
                Action = action,
                Resource = resource,
                CountryCode = countryCode,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = isSuccess,
                ErrorMsg = errorMsg,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
