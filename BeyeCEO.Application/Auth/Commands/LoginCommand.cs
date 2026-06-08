using BeyeCEO.Application.Auth.DTOs;
using BeyeCEO.Domain.Auth;
using BeyeCEO.Domain.Auth.Entites;
using BeyeCEO.Domain.Auth.Interfaces;
using BeyeCEO.Domain.Shared.Enum;
using MediatR;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Application.Auth.Commands
{ // ── Command — request واحد للاثنين ────────────────────
    public record LoginCommand(
        string Email,
        string? Password,       // nullable — Windows ما بيرسل Password
        string DeviceInfo = ""
    ) : IRequest<AuthResponseDto>;

    // ── Handler ───────────────────────────────────────────
    public class LoginCommandHandler
        : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        private readonly IAuthRepository _authRepo;
        private readonly IJwtService _jwtService;

        public LoginCommandHandler(
            IAuthRepository authRepo,
            IJwtService jwtService)
        {
            _authRepo = authRepo;
            _jwtService = jwtService;
        }

        public async Task<AuthResponseDto> Handle(
            LoginCommand request, CancellationToken ct)
        {
            // 1. جيب الـ User بالـ Email
            var user = await _authRepo.GetByEmailAsync(request.Email)
                ?? throw new UnauthorizedAccessException(
                    "Invalid email or password");

            // 2. تأكد إنه Active
            if (!user.IsActive)
                throw new UnauthorizedAccessException(
                    "Account is disabled");

            // 3. شيك على الـ AuthType تبعه
            if (user.AuthType == AuthType.Local)
            {
                // Local — لازم Password
                if (string.IsNullOrWhiteSpace(request.Password))
                    throw new UnauthorizedAccessException(
                        "Password is required");

                if (!BCrypt.Net.BCrypt.Verify(
                    request.Password, user.PasswordHash))
                    throw new UnauthorizedAccessException(
                        "Invalid email or password");
            }
            else if (user.AuthType == AuthType.Windows)
            {
                // Windows — السيرفر تحقق بالفعل
                // ما نحتاج نتحقق من Password
            }

            // 4. سجّل الـ Login
            user.RecordLogin();
            await _authRepo.UpdateUserAsync(user);

            // 5. ولّد الـ Tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // 6. احفظ الـ Refresh Token
            await _authRepo.SaveRefreshTokenAsync(
                RefreshToken.Create(
                    user.Id, refreshToken, 7, request.DeviceInfo));

            // 7. سجّل الـ Audit
            await _authRepo.LogAuditAsync(AuditLog.Create(
                action: "LOGIN",
                resource: "/api/auth/login",
                ipAddress: string.Empty,
                userId: user.Id,
                bankId: user.BankId,
                isSuccess: true));

            // 8. أرجع الـ Response
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullNameEN = user.FullNameEN,
                    FullNameAR = user.FullNameAR,
                    Role = user.Role.ToString(),
                    PreferredLanguage = user.PreferredLanguage.ToString(),
                    DefaultCountryCode = user.DefaultCountryCode,
                    BankId = user.BankId,
                    AuthType = user.AuthType.ToString()
                }
            };
        }
    }
}

