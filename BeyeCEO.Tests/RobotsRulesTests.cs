using BeyeCEO.Infrastructure.NewsAggregation.ImageFallback;

namespace BeyeCEO.Tests
{
    public class RobotsRulesTests
    {
        [Fact]
        public void Parse_EmptyOrNull_AllowsEverything()
        {
            var rules = RobotsRules.Parse(null);

            Assert.True(rules.IsAllowed("/anything", "*"));
            Assert.Null(rules.GetCrawlDelaySeconds("*"));
        }

        [Fact]
        public void IsAllowed_DisallowForGenericStar_BlocksMatchingPrefix()
        {
            var rules = RobotsRules.Parse("""
                User-agent: *
                Disallow: /admin/
                """);

            Assert.False(rules.IsAllowed("/admin/users", "BeyeCEO-NewsReader"));
            Assert.True(rules.IsAllowed("/news/article-1", "BeyeCEO-NewsReader"));
        }

        [Fact]
        public void IsAllowed_AllowOverridesDisallow_WhenAllowIsLongerOrEqual()
        {
            var rules = RobotsRules.Parse("""
                User-agent: *
                Disallow: /core/
                Allow: /core/*.jpg
                """);

            Assert.True(rules.IsAllowed("/core/photo.jpg", "*"));
            Assert.False(rules.IsAllowed("/core/private.html", "*"));
        }

        [Fact]
        public void IsAllowed_SpecificUserAgentBlock_TakesPriorityOverStar()
        {
            var rules = RobotsRules.Parse("""
                User-agent: ClaudeBot
                Disallow: /

                User-agent: *
                Allow: /
                """);

            Assert.False(rules.IsAllowed("/anything", "ClaudeBot"));
            Assert.True(rules.IsAllowed("/anything", "SomeOtherBot"));
        }

        [Fact]
        public void IsAllowed_MultipleConsecutiveUserAgentLines_ShareTheSameRules()
        {
            // Mirrors Finextra's real robots.txt structure: many consecutive
            // User-agent lines sharing one Disallow:/ block.
            var rules = RobotsRules.Parse("""
                User-agent: BotOne
                User-agent: BotTwo
                Disallow: /
                """);

            Assert.False(rules.IsAllowed("/x", "BotOne"));
            Assert.False(rules.IsAllowed("/x", "BotTwo"));
        }

        [Fact]
        public void GetCrawlDelaySeconds_ReturnsDeclaredValue_ForGenericStar()
        {
            var rules = RobotsRules.Parse("""
                User-agent: *
                Crawl-delay: 60
                """);

            Assert.Equal(60, rules.GetCrawlDelaySeconds("*"));
            Assert.Equal(60, rules.GetCrawlDelaySeconds("AnyBot"));
        }

        [Fact]
        public void IsAllowed_EmptyDisallowValue_MeansAllowEverything()
        {
            var rules = RobotsRules.Parse("""
                User-agent: *
                Disallow:
                """);

            Assert.True(rules.IsAllowed("/anything", "*"));
        }

        [Fact]
        public void IsAllowed_WildcardInDisallowPattern_Matches()
        {
            var rules = RobotsRules.Parse("""
                User-agent: *
                Disallow: /*?*returnurl=
                """);

            Assert.False(rules.IsAllowed("/page?returnurl=https://evil.example", "*"));
            Assert.True(rules.IsAllowed("/page?other=1", "*"));
        }
    }
}
