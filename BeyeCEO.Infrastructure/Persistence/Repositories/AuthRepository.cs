using BeyeCEO.Domain.Auth;
using BeyeCEO.Domain.Auth.Entites;
using BeyeCEO.Domain.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly BeyeCeoDbContext _context;

        public AuthRepository(BeyeCeoDbContext context)
        {
            _context = context;
        }

        // ── Users ─────────────────────────────────────────────

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Where(x => x.Email == email.ToLower().Trim() && x.IsActive)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByWindowsSidAsync(string windowsSid)
        {
            return await _context.Users
                .Where(x => x.WindowsSid == windowsSid && x.IsActive)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Where(x => x.Id == id && x.IsActive)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByIdTrackingAsync(Guid id)
        {
            // مع Tracking — لما بدنا نعدّل على الـ User
            return await _context.Users
                .Where(x => x.Id == id && x.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task SaveUserAsync(User user)
        {
            var existing = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == user.Id);

            if (existing == null)
                await _context.Users.AddAsync(user);
            else
                _context.Entry(existing).CurrentValues.SetValues(user);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        // ── Refresh Tokens ────────────────────────────────────

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Where(x => x.Token == token && !x.IsRevoked)
                .Include(x => x.User)   // محتاج بيانات الـ User للـ JWT
                .FirstOrDefaultAsync();
        }

        public async Task SaveRefreshTokenAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == token);

            if (refreshToken == null) return;

            refreshToken.Revoke();
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllUserTokensAsync(Guid userId)
        {
            // لما الـ CEO يعمل Logout من كل الأجهزة
            var tokens = await _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
                token.Revoke();

            await _context.SaveChangesAsync();
        }

        public async Task CleanupExpiredTokensAsync()
        {
            // يُشغَّل من Background Job أسبوعياً
            var expiredTokens = await _context.RefreshTokens
                .Where(x => x.ExpiresAt < DateTime.UtcNow || x.IsRevoked)
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }

        // ── Audit Log ─────────────────────────────────────────

        public async Task LogAuditAsync(AuditLog log)
        {
            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(
            Guid userId, int count = 50)
        {
            return await _context.AuditLogs
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Timestamp)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
