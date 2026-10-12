namespace BeyeCEO.Infrastructure.NewsAggregation.ImageFallback
{
    // Pure: a candidate image URL that already appears >= threshold times
    // for the same source (counting both already-stored active rows and
    // items already assigned in this run) is treated as a generic
    // site-wide default/logo rather than a real per-article photo
    // (Step 11a found this exact pattern for BIS/BoE/Fed) and must not be
    // stored.
    public static class SiteDefaultImageGuard
    {
        public const int DefaultThreshold = 3;

        public static bool IsSiteDefault(
            int existingCountInDb, int countAssignedThisRun, int threshold = DefaultThreshold) =>
            existingCountInDb + countAssignedThisRun >= threshold;
    }
}
