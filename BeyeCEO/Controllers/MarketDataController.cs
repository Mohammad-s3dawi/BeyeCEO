using BeyeCEO.API.Extensions;
using BeyeCEO.Application.MarketData.DTOs;
using BeyeCEO.Application.MarketData.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BeyeCEO.API.Controllers
{
    [ApiController]
    [Route("api/market")]
    [Authorize]
    public class MarketDataController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MarketDataController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/market/global
        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalMarkets()
        {
            var result = await _mediator.Send(
                new GetGlobalMarketsQuery());

            return Ok(new ApiResponse<GlobalMarketsDto>(true, result));
        }

        // GET /api/market/local/{countryCode}
        [HttpGet("local/{countryCode}")]
        public async Task<IActionResult> GetLocalEconomy(
            string countryCode)
        {
            var result = await _mediator.Send(
                new GetLocalEconomyQuery(countryCode));

            return Ok(new ApiResponse<LocalEconomyDto>(true, result));
        }
    }
}
