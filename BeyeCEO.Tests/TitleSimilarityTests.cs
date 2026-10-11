using BeyeCEO.Infrastructure.NewsAggregation;

namespace BeyeCEO.Tests
{
    public class TitleSimilarityTests
    {
        [Fact]
        public void Jaccard_SameArabicStory_DifferentSources_IsHighSimilarity()
        {
            // Same CBJ reserves wire story as published (byte-identical, in
            // this real case) by Al Wakeel and Al Mamlaka in the Step 6/7 dry-run.
            var a = "الاحتياطيات الأجنبية للبنك المركزي ترتفع إلى 28.5 مليار دولار في نهاية أيلول";
            var b = "الاحتياطيات الأجنبية للبنك المركزي ترتفع إلى 28.5 مليار دولار في نهاية أيلول";

            var similarity = TitleSimilarity.Jaccard(a, b);

            Assert.True(similarity >= NearDuplicateResolver.JaccardThreshold);
        }

        [Fact]
        public void Jaccard_UnrelatedTitles_IsLowSimilarity()
        {
            var a = "Central bank raises interest rates amid inflation concerns";
            var b = "Local football team wins championship after dramatic final";

            var similarity = TitleSimilarity.Jaccard(a, b);

            Assert.True(similarity < NearDuplicateResolver.JaccardThreshold);
        }

        [Fact]
        public void Jaccard_EnglishPair_SameWordsDifferentOrder_IsHighSimilarity()
        {
            var a = "Jordan central bank raises interest rates amid inflation";
            var b = "Interest rates raised amid inflation by Jordan central bank";

            var similarity = TitleSimilarity.Jaccard(a, b);

            Assert.True(similarity >= NearDuplicateResolver.JaccardThreshold);
        }

        [Fact]
        public void Jaccard_DigitFormattingDifferences_StillMatch()
        {
            var a = "Reserves rise to 28.5 billion dollars in September";
            var b = "Reserves rise to 28,5 billion dollars in September";

            var similarity = TitleSimilarity.Jaccard(a, b);

            Assert.True(similarity >= NearDuplicateResolver.JaccardThreshold);
        }

        [Fact]
        public void Tokenize_EmptyOrNull_ReturnsEmptySet()
        {
            Assert.Empty(TitleSimilarity.Tokenize(null));
            Assert.Empty(TitleSimilarity.Tokenize(""));
            Assert.Empty(TitleSimilarity.Tokenize("   "));
        }

        [Fact]
        public void Tokenize_DropsStopwordsAndPunctuation()
        {
            var tokens = TitleSimilarity.Tokenize("The Bank of England raises rates, again!");

            Assert.DoesNotContain("the", tokens);
            Assert.DoesNotContain("of", tokens);
            Assert.Contains("bank", tokens);
            Assert.Contains("england", tokens);
            Assert.Contains("rates", tokens);
            Assert.Contains("again", tokens);
        }
    }
}
