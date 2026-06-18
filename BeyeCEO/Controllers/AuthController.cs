using BeyeCEO.API.Extensions;
using BeyeCEO.Application.Auth.Commands;
using BeyeCEO.Application.Auth.DTOs;
using BeyeCEO.Domain.Auth.Interfaces;
using BeyeCEO.Infrastructure.ExternalServices;
using BeyeCEO.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BeyeCEO.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAuthRepository _authRepo;
        private readonly IJwtService _jwtService;

        public AuthController(IMediator mediator,
            IAuthRepository authRepo, IJwtService jwtService)
        {
            _mediator = mediator;
            _authRepo = authRepo;
            _jwtService = jwtService;
        }

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _mediator.Send(
                    new LoginCommand(request.Email, request.Password, request.DeviceInfo));

                return Ok(new ApiResponse<AuthResponseDto>(true, result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<string>(false, ex.Message));
            }
        }
        [HttpGet("hash/{password}")]
        [AllowAnonymous]
        public IActionResult HashPassword(string password)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            return Ok(hash);
        }

        // POST /api/auth/refresh
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var token = await _authRepo.GetRefreshTokenAsync(request.RefreshToken);

            if (token == null || !token.IsValid)
                return Unauthorized(new ApiResponse<string>(false, "Invalid or expired token"));

            // ولّد Access Token جديد
            var newAccessToken = _jwtService.GenerateAccessToken(token.User);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // الغِ القديم واحفظ الجديد
            await _authRepo.RevokeRefreshTokenAsync(request.RefreshToken);
            await _authRepo.SaveRefreshTokenAsync(
                Domain.Auth.Entites.RefreshToken.Create(
                    token.UserId, newRefreshToken, 7));

            return Ok(new ApiResponse<object>(true, new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken,
                expiresAt = DateTime.UtcNow.AddHours(2)
            }));
        }

        // POST /api/auth/logout
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            await _authRepo.RevokeRefreshTokenAsync(request.RefreshToken);

            await _authRepo.LogAuditAsync(
                Domain.Auth.AuditLog.Create(
                    action: "LOGOUT",
                    resource: "/api/auth/logout",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    userId: Guid.Parse(User.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier)!.Value)));

            return Ok(new ApiResponse<string>(true, "Logged out successfully"));
        }
        // GET /api/auth/test-ase
        [HttpGet("test-ase")]
        [AllowAnonymous]
        public async Task<IActionResult> TestASE(
      [FromServices] ASEClient client)
        {
            var data = await client.FetchMarketDataAsync();
            var movers = await client.FetchTopMoversAsync();

            return Ok(new
            {
                marketData = data == null ? null : new
                {
                    data.GeneralIndex,
                    data.ChangePct,
                    data.Gainers,
                    data.Losers
                },
                topMovers = movers.Select(x => new {
                    x.Symbol,
                    x.CompanyName,
                    x.ChangePct,
                    x.MoverType,
                    x.Rank
                })
            });
        }

        // GET /api/auth/test-cbj
        [HttpGet("test-cbj")]
        [AllowAnonymous]
        public async Task<IActionResult> TestCBJ(
            [FromServices] CBJScraper scraper)
        {
            var indicators = await scraper.FetchAsync();
            return Ok(indicators.Select(x => new {
                x.IndicatorCode,
                x.Value,
                x.Unit
            }));
        }
    }

    // ── Request Models ────────────────────────────────────────
    public record LoginRequest(
        string Email,
        string? Password,
        string DeviceInfo = "");

    public record RefreshRequest(string RefreshToken);

}
