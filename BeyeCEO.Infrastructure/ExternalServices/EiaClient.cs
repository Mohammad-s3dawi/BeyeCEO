using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{

    public class EiaClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public EiaClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["ExternalApis:Eia:ApiKey"]!;
            _baseUrl = config["ExternalApis:Eia:BaseUrl"]!;
        }

        public async Task<EiaCommodityResult?> GetBrentCrudeAsync()
        {
            return await GetCommodityAsync(
                "petroleum/pri/spt",
                "RBRTE",
                "Brent Crude Oil");
        }

        public async Task<EiaCommodityResult?> GetWtiCrudeAsync()
        {
            return await GetCommodityAsync(
                "petroleum/pri/spt",
                "RWTC",
                "WTI Crude Oil");
        }

        private async Task<EiaCommodityResult?> GetCommodityAsync(
      string route, string seriesId, string name)
        {
            var url = $"{_baseUrl}/{route}/data/" +
                      $"?api_key={_apiKey}" +
                      $"&frequency=daily" +
                      $"&data[0]=value" +
                      $"&facets[series][]={seriesId}" +
                      $"&sort[0][column]=period" +
                      $"&sort[0][direction]=desc" +
                      $"&length=2";

            var response = await _http.GetStringAsync(url);
            var json = JsonDocument.Parse(response);

            var data = json.RootElement
                .GetProperty("response")
                .GetProperty("data");

            if (data.GetArrayLength() == 0) return null;

            var latest = data[0];
            var previous = data.GetArrayLength() > 1 ? data[1] : data[0];

            // ← string مش decimal
            var latestValueStr = latest.GetProperty("value").GetString();
            var previousValueStr = previous.GetProperty("value").GetString();

            if (string.IsNullOrEmpty(latestValueStr)) return null;

            var latestValue = decimal.Parse(latestValueStr);
            var previousValue = decimal.Parse(
                previousValueStr ?? latestValueStr);

            var changePct = previousValue != 0
                ? (latestValue - previousValue) / previousValue * 100
                : 0;

            return new EiaCommodityResult
            {
                Symbol = seriesId,
                Name = name,
                Price = latestValue,
                ChangePct = Math.Round(changePct, 4)
            };
        }
    }

    public class EiaCommodityResult
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal ChangePct { get; set; }
    }
}
