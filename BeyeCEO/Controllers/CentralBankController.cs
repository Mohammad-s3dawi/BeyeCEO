using BeyeCEO.API.Extensions;
using BeyeCEO.Application.MarketData.DTOs;
using BeyeCEO.Application.MarketData.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeyeCEO.API.Controllers
{
    [ApiController]
    [Route("api/centralbank")]
    [Authorize]
    public class CentralBankController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CentralBankController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/centralbank/{countryCode}/circulars?tab=general&page=1&pageSize=20
        [HttpGet("{countryCode}/circulars")]
        public async Task<IActionResult> GetCirculars(
            string countryCode,
            [FromQuery] string? tab = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(
                new GetCircularsQuery(countryCode, tab, page, pageSize));

            return Ok(new ApiResponse<CircularsListDto>(true, result));
        }
    }
}
