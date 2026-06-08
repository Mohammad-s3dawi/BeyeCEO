using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Application.Auth.DTOs
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto User { get; set; } = null!;
    }

    public class UserInfoDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullNameEN { get; set; } = string.Empty;
        public string FullNameAR { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = string.Empty;
        public string? DefaultCountryCode { get; set; }
        public Guid BankId { get; set; }
        public string AuthType { get; set; } = string.Empty;
    }
}
