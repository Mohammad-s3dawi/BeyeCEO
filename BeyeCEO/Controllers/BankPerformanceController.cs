using BeyeCEO.API.Extensions;
using BeyeCEO.Application.MarketData.DTOs;
using BeyeCEO.Application.MarketData.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BeyeCEO.API.Controllers
{
    [ApiController]
    [Route("api/bank-performance")]
    [Authorize]
    public class BankPerformanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BankPerformanceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/bank-performance/{bankId}/bs
        [HttpGet("{bankId}/bs")]
        public async Task<IActionResult> GetBS(Guid bankId)
        {
            var result = await _mediator.Send(
                new GetBankPerformanceQuery(bankId, "BS"));

            if (result == null)
                return NotFound(new ApiResponse<string>(
                    false, "No performance data found"));

            return Ok(new ApiResponse<BankPerformanceResponseDto>(true, result));
        }
    }
}
