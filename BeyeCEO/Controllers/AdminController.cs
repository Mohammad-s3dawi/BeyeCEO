using BeyeCEO.API.Extensions;
using BeyeCEO.Application.MarketData.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BeyeCEO.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST /api/admin/cbj-rates
        [HttpPost("cbj-rates")]
        public async Task<IActionResult> SaveCbjRates(
            [FromBody] SaveCbjRatesRequest request)
        {
            await _mediator.Send(new SaveCbjRatesCommand(
                request.CountryCode,
                request.MainRate,
                request.ReDiscountRate,
                request.RepoRate,
                request.DepositWindowRate,
                request.EffectiveDate));

            return Ok(new ApiResponse<string>(
                true, "CBJ rates saved successfully"));
        }
    }

    // ── Request Models ────────────────────────────────────────
    public record SaveCbjRatesRequest(
        string CountryCode,
        decimal MainRate,
        decimal ReDiscountRate,
        decimal RepoRate,
        decimal DepositWindowRate,
        DateOnly EffectiveDate);
}
