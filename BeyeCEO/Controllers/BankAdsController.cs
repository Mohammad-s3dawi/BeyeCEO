using BeyeCEO.API.Extensions;
using BeyeCEO.Application.MarketData.DTOs;
using BeyeCEO.Application.MarketData.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeyeCEO.API.Controllers
{
    [ApiController]
    [Route("api/bank-ads")]
    [Authorize]
    public class BankAdsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BankAdsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/bank-ads/{countryCode}
        [HttpGet("{countryCode}")]
        public async Task<IActionResult> GetBankAds(string countryCode)
        {
            var result = await _mediator.Send(new GetBankAdsQuery(countryCode));
            return Ok(new ApiResponse<BankAdsListDto>(true, result));
        }
    }
}
