using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.NewsAggregation.ImageFallback
{
    public interface IRobotsTxtCache
    {
        Task<RobotsRules> GetRulesAsync(string origin, CancellationToken ct = default);
    }

    // Singleton — caches parsed robots.txt per origin for 24h so every page
    // fetch doesn't re-fetch robots.txt. A failed/absent robots.txt caches
    // as "no restrictions" (standard convention), also for 24h, so a
    // temporarily-down robots.txt doesn't get re-tried every single call.
    public class RobotsTxtCache : IRobotsTxtCache
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<RobotsTxtCache> _logger;
        private readonly ConcurrentDictionary<string, (RobotsRules Rules, DateTime FetchedAtUtc)> _cache = new();

        public RobotsTxtCache(IHttpClientFactory httpFactory, ILogger<RobotsTxtCache> logger)
        {
            _httpFactory = httpFactory;
            _logger = logger;
        }

        public async Task<RobotsRules> GetRulesAsync(string origin, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(origin, out var cached) && DateTime.UtcNow - cached.FetchedAtUtc < Ttl)
                return cached.Rules;

            var rules = await FetchAsync(origin, ct);
            _cache[origin] = (rules, DateTime.UtcNow);
            return rules;
        }

        private async Task<RobotsRules> FetchAsync(string origin, CancellationToken ct)
        {
            try
            {
                var client = _httpFactory.CreateClient("NewsImageFallback");
                using var response = await client.GetAsync($"{origin}/robots.txt", ct);
                if (!response.IsSuccessStatusCode) return RobotsRules.Empty;

                var text = await response.Content.ReadAsStringAsync(ct);
                return RobotsRules.Parse(text);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "RobotsTxtCache: could not fetch robots.txt for {Origin} — treating as unrestricted", origin);
                return RobotsRules.Empty;
            }
        }
    }
}
