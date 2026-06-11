using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class FredClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public FredClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["ExternalApis:Fred:ApiKey"]!;
            _baseUrl = config["ExternalApis:Fred:BaseUrl"]!;
        }

        public async Task<FredObservation?> GetLatestAsync(
            string seriesId)
        {
            var url = $"{_baseUrl}/series/observations" +
                      $"?series_id={seriesId}" +
                      $"&api_key={_apiKey}" +
                      $"&file_type=json" +
                      $"&limit=1" +
                      $"&sort_order=desc";

            var response = await _http.GetStringAsync(url);
            var json = JsonDocument.Parse(response);

            var observations = json.RootElement
                .GetProperty("observations");

            if (observations.GetArrayLength() == 0)
                return null;

            var obs = observations[0];
            var valueStr = obs.GetProperty("value").GetString();

            if (valueStr == "." || string.IsNullOrEmpty(valueStr))
                return null;

            return new FredObservation
            {
                SeriesId = seriesId,
                Value = decimal.Parse(valueStr),
                Date = DateOnly.Parse(
                    obs.GetProperty("date").GetString()!)
            };
        }
    }

    // Series IDs:
    // FEDFUNDS → Fed Funds Rate
    // T10YIE   → 10-Year Breakeven Inflation
    // GDP      → US GDP
    public class FredObservation
    {
        public string SeriesId { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateOnly Date { get; set; }
    }
}
