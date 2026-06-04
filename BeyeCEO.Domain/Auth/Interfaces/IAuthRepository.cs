using BeyeCEO.Domain.Auth.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.Auth.Interfaces
{
    public interface IAuthRepository
    {
        // ── Users ─────────────────────────────────────────────
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByWindowsSidAsync(string windowsSid);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByIdTrackingAsync(Guid id);
        Task SaveUserAsync(User user);
        Task UpdateUserAsync(User user);

        // ── Refresh Tokens ────────────────────────────────────
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task SaveRefreshTokenAsync(RefreshToken token);
        Task RevokeRefreshTokenAsync(string token);
        Task RevokeAllUserTokensAsync(Guid userId);
        Task CleanupExpiredTokensAsync();

        // ── Audit Log ─────────────────────────────────────────
        Task LogAuditAsync(AuditLog log);
        Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(Guid userId, int count = 50);
    }
}
