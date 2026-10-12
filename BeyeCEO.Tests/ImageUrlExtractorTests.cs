using BeyeCEO.Infrastructure.NewsAggregation.ImageFallback;
using System;

namespace BeyeCEO.Tests
{
    public class ImageUrlExtractorTests
    {
        private static readonly Uri PageUri = new("https://example.com/news/article-1");

        [Fact]
        public void ExtractCandidate_PrefersOgImage_OverTwitterAndLink()
        {
            const string html = """
                <meta property="og:image" content="https://example.com/og.jpg" />
                <meta name="twitter:image" content="https://example.com/tw.jpg" />
                <link rel="image_src" href="https://example.com/link.jpg" />
                """;

            Assert.Equal("https://example.com/og.jpg", ImageUrlExtractor.ExtractCandidate(html));
        }

        [Fact]
        public void ExtractCandidate_FallsBackToTwitterImage_WhenNoOgImage()
        {
            const string html = """<meta name="twitter:image" content="https://example.com/tw.jpg" />""";

            Assert.Equal("https://example.com/tw.jpg", ImageUrlExtractor.ExtractCandidate(html));
        }

        [Fact]
        public void ExtractCandidate_FallsBackToLinkImageSrc_WhenNoMetaTags()
        {
            const string html = """<link rel="image_src" href="https://example.com/link.jpg" />""";

            Assert.Equal("https://example.com/link.jpg", ImageUrlExtractor.ExtractCandidate(html));
        }

        [Fact]
        public void ExtractCandidate_FallsBackToJsonLdImage_WhenNothingElseMatches()
        {
            const string html = """<script type="application/ld+json">{"image":"https://example.com/jsonld.jpg"}</script>""";

            Assert.Equal("https://example.com/jsonld.jpg", ImageUrlExtractor.ExtractCandidate(html));
        }

        [Fact]
        public void ExtractCandidate_NoImageTagsAtAll_ReturnsNull()
        {
            Assert.Null(ImageUrlExtractor.ExtractCandidate("<p>just text, no meta tags</p>"));
        }

        [Fact]
        public void Normalize_ProtocolRelative_ResolvesToHttps()
        {
            var result = ImageUrlExtractor.Normalize("//cdn.example.com/photo.jpg", PageUri);

            Assert.Equal("https://cdn.example.com/photo.jpg", result?.ToString());
        }

        [Fact]
        public void Normalize_RootRelative_ResolvesAgainstPageOrigin()
        {
            var result = ImageUrlExtractor.Normalize("/images/photo.jpg", PageUri);

            Assert.Equal("https://example.com/images/photo.jpg", result?.ToString());
        }

        [Fact]
        public void Normalize_PlainRelative_ResolvesAgainstPageDirectory()
        {
            var result = ImageUrlExtractor.Normalize("photo.jpg", PageUri);

            Assert.Equal("https://example.com/news/photo.jpg", result?.ToString());
        }

        [Fact]
        public void Normalize_AlreadyAbsolute_PassesThroughUnchanged()
        {
            var result = ImageUrlExtractor.Normalize("https://other.example.com/x.jpg", PageUri);

            Assert.Equal("https://other.example.com/x.jpg", result?.ToString());
        }

        [Fact]
        public void Normalize_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(ImageUrlExtractor.Normalize(null, PageUri));
            Assert.Null(ImageUrlExtractor.Normalize("", PageUri));
        }

        [Theory]
        [InlineData("https://example.com/themes/custom/bis/meta-default.jpg")]
        [InlineData("https://example.com/logo.png")]
        [InlineData("https://example.com/favicon.ico")]
        [InlineData("https://example.com/boe-default-social.jpg")]
        [InlineData("https://example.com/placeholder.jpg")]
        [InlineData("https://example.com/sprite.png")]
        [InlineData("https://example.com/icon-32x32.png")]
        public void LooksLikeNonPhoto_RecognizesKnownNonPhotoPatterns(string url)
        {
            Assert.True(ImageUrlExtractor.LooksLikeNonPhoto(new Uri(url)));
        }

        [Fact]
        public void LooksLikeNonPhoto_RealArticlePhoto_IsNotFlagged()
        {
            var uri = new Uri("https://almamlakatv.com/images/articles/big/2026/10/6acc8f16bf451.jpeg");

            Assert.False(ImageUrlExtractor.LooksLikeNonPhoto(uri));
        }
    }
}
