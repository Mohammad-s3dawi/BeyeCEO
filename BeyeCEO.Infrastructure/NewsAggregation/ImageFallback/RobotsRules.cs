using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeyeCEO.Infrastructure.NewsAggregation.ImageFallback
{
    // Pure parser/matcher for robots.txt content. Groups consecutive
    // "User-agent:" lines into one block sharing the Disallow/Allow/
    // Crawl-delay directives that follow, matching standard robots.txt
    // grouping semantics. Never makes network calls.
    public class RobotsRules
    {
        private class Block
        {
            public List<string> UserAgents { get; } = new();
            public List<string> Disallow { get; } = new();
            public List<string> Allow { get; } = new();
            public double? CrawlDelaySeconds { get; set; }
            public bool HasDirectives { get; set; }
        }

        private readonly List<Block> _blocks;

        private RobotsRules(List<Block> blocks) => _blocks = blocks;

        // Absent/empty robots.txt == no restrictions (standard convention).
        public static RobotsRules Empty => new(new List<Block>());

        public static RobotsRules Parse(string? robotsTxt)
        {
            var blocks = new List<Block>();
            if (string.IsNullOrWhiteSpace(robotsTxt)) return new RobotsRules(blocks);

            List<string> pendingAgents = new();
            Block? current = null;

            foreach (var rawLine in robotsTxt.Replace("\r\n", "\n").Split('\n'))
            {
                var line = StripComment(rawLine).Trim();
                if (line.Length == 0) continue;

                var colonIdx = line.IndexOf(':');
                if (colonIdx < 0) continue;

                var key = line[..colonIdx].Trim().ToLowerInvariant();
                var value = line[(colonIdx + 1)..].Trim();

                switch (key)
                {
                    case "user-agent":
                        if (current is { HasDirectives: true })
                        {
                            // A new agent after directives starts a fresh group.
                            pendingAgents = new List<string>();
                            current = null;
                        }
                        pendingAgents.Add(value);
                        break;

                    case "disallow":
                        current ??= StartBlock(blocks, pendingAgents);
                        current.HasDirectives = true;
                        if (value.Length > 0) current.Disallow.Add(value);
                        break;

                    case "allow":
                        current ??= StartBlock(blocks, pendingAgents);
                        current.HasDirectives = true;
                        if (value.Length > 0) current.Allow.Add(value);
                        break;

                    case "crawl-delay":
                        current ??= StartBlock(blocks, pendingAgents);
                        current.HasDirectives = true;
                        if (double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var secs))
                            current.CrawlDelaySeconds = secs;
                        break;
                }
            }

            return new RobotsRules(blocks);
        }

        private static Block StartBlock(List<Block> blocks, List<string> agents)
        {
            var block = new Block();
            block.UserAgents.AddRange(agents);
            blocks.Add(block);
            return block;
        }

        private static string StripComment(string line)
        {
            var idx = line.IndexOf('#');
            return idx >= 0 ? line[..idx] : line;
        }

        private Block? FindBlock(string userAgentToken) =>
            _blocks.FirstOrDefault(b => b.UserAgents.Any(
                ua => string.Equals(ua, userAgentToken, StringComparison.OrdinalIgnoreCase)));

        private Block? FindApplicableBlock(string userAgentToken) =>
            FindBlock(userAgentToken) ?? FindBlock("*");

        // True if `path` may be fetched by `userAgentToken` (falling back to
        // the "*" block if there's no UA-specific one). Longest-matching
        // rule wins; an Allow of equal-or-greater length beats a Disallow.
        public bool IsAllowed(string path, string userAgentToken)
        {
            var block = FindApplicableBlock(userAgentToken);
            if (block == null) return true;

            var matchedDisallow = block.Disallow
                .Where(d => PathMatches(path, d))
                .OrderByDescending(d => d.Length)
                .FirstOrDefault();
            if (matchedDisallow == null) return true;

            var matchedAllow = block.Allow
                .Where(a => PathMatches(path, a))
                .OrderByDescending(a => a.Length)
                .FirstOrDefault();

            return matchedAllow != null && matchedAllow.Length >= matchedDisallow.Length;
        }

        public double? GetCrawlDelaySeconds(string userAgentToken) =>
            FindApplicableBlock(userAgentToken)?.CrawlDelaySeconds;

        // Simplified robots.txt path matching: '*' is a wildcard, trailing
        // '$' anchors the end; otherwise it's a prefix match.
        private static bool PathMatches(string path, string pattern)
        {
            if (pattern.Length == 0) return false;

            var hasEndAnchor = pattern.EndsWith('$');
            var core = hasEndAnchor ? pattern[..^1] : pattern;
            var regexPattern = "^" + Regex.Escape(core).Replace("\\*", ".*") + (hasEndAnchor ? "$" : "");
            return Regex.IsMatch(path, regexPattern);
        }
    }
}
