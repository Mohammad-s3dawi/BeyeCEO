using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class MetalsClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<MetalsClient> _logger;

        private const string GoldUrl = "https://api.gold-api.com/price/XAU";

        public MetalsClient(HttpClient http, ILogger<MetalsClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<decimal?> GetGoldPriceAsync()
        {
            try
            {
                var response = await _http.GetStringAsync(GoldUrl);
                var json = JsonDocument.Parse(response);
                var price = json.RootElement.GetProperty("price").GetDecimal();

                _logger.LogInformation(
                    "MetalsClient: ✅ Gold = ${Price}", price);

                return price;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "MetalsClient: ❌ Failed to fetch gold price: {Message}",
                    ex.Message);
                return null;
            }
        }
    }
}
