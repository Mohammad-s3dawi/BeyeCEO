using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.Auth.Entites
{
    public class RefreshToken:BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; } = false;
        public DateTime? RevokedAt { get; private set; }
        public string DeviceInfo { get; private set; } = string.Empty;
        public User User { get; private set; } = null!;
        private RefreshToken() { }

        public static RefreshToken Create(Guid userId, string token,
            int expiryDays = 7, string deviceInfo = "")
        {
            return new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                IsRevoked = false,
                DeviceInfo = deviceInfo
            };
        }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsValid => !IsRevoked && !IsExpired;

        public void Revoke()
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }
    }
}
