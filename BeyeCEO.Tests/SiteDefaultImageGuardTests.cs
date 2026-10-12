using BeyeCEO.Infrastructure.NewsAggregation.ImageFallback;

namespace BeyeCEO.Tests
{
    public class SiteDefaultImageGuardTests
    {
        [Theory]
        [InlineData(0, 0, false)]
        [InlineData(1, 0, false)]
        [InlineData(0, 2, false)]
        [InlineData(2, 0, false)]
        [InlineData(3, 0, true)]
        [InlineData(0, 3, true)]
        [InlineData(2, 1, true)]
        [InlineData(5, 0, true)]
        public void IsSiteDefault_CombinesDbAndRunCounts_AgainstThreshold(
            int existingCountInDb, int countAssignedThisRun, bool expected)
        {
            Assert.Equal(expected, SiteDefaultImageGuard.IsSiteDefault(existingCountInDb, countAssignedThisRun));
        }

        [Fact]
        public void IsSiteDefault_CustomThreshold_IsRespected()
        {
            Assert.False(SiteDefaultImageGuard.IsSiteDefault(1, 0, threshold: 2));
            Assert.True(SiteDefaultImageGuard.IsSiteDefault(2, 0, threshold: 2));
        }
    }
}
