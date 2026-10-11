using BeyeCEO.Infrastructure.NewsAggregation;
using System;
using System.Linq;

namespace BeyeCEO.Tests
{
    public class FeedParserTests
    {
        [Fact]
        public void Parse_Rss20_ExtractsTitleLinkSummaryDateAndPrefersEnclosureImage()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <rss version="2.0">
                <channel>
                <title>Test Feed</title>
                <item>
                <title>Central bank raises rates</title>
                <link>https://Example.com/article1?utm_source=feed&amp;ref=abc</link>
                <description><![CDATA[<p>The <b>central bank</b> raised rates today.</p><img src="https://example.com/in-desc.jpg"/>]]></description>
                <pubDate>Wed, 07 Oct 2026 10:03:38 +0300</pubDate>
                <enclosure url="https://example.com/enclosure.jpg" type="image/jpeg" />
                </item>
                </channel>
                </rss>
                """;

            var items = FeedParser.Parse(xml, "rss");

            Assert.Single(items);
            var item = items[0];
            Assert.Equal("Central bank raises rates", item.Title);
            Assert.Equal("https://example.com/article1?ref=abc", item.Link);
            Assert.Contains("central bank raised rates today", item.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<b>", item.Summary);
            Assert.Equal("https://example.com/enclosure.jpg", item.ImageUrl);
            Assert.False(item.DateGuessed);
            Assert.Equal(new DateTime(2026, 10, 7, 7, 3, 38, DateTimeKind.Utc), item.PublishedAtUtc);
        }

        [Fact]
        public void Parse_Rss20_FallsBackToDescriptionImage_WhenNoEnclosure()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <rss version="2.0">
                <channel>
                <item>
                <title>No enclosure here</title>
                <link>https://example.com/a2</link>
                <description>&lt;img src="https://example.com/desc-img.jpg"/&gt;Some text</description>
                <pubDate>Wed, 07 Oct 2026 10:03:38 +0300</pubDate>
                </item>
                </channel>
                </rss>
                """;

            var items = FeedParser.Parse(xml, "rss");

            Assert.Equal("https://example.com/desc-img.jpg", items[0].ImageUrl);
        }

        [Fact]
        public void Parse_Rss20_MissingPubDate_MarksDateGuessed()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <rss version="2.0">
                <channel>
                <item>
                <title>No date here</title>
                <link>https://example.com/a3</link>
                <description>text</description>
                </item>
                </channel>
                </rss>
                """;

            var items = FeedParser.Parse(xml, "rss");

            Assert.True(items[0].DateGuessed);
        }

        [Fact]
        public void Parse_Atom_ExtractsAlternateLinkAndNormalizesHostOnly()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <feed xmlns="http://www.w3.org/2005/Atom">
                <title>Test Atom Feed</title>
                <entry>
                <title>بنك ينمو بقوة</title>
                <link rel="alternate" href="https://Example.com/AR/article2#section1"/>
                <summary><![CDATA[ملخص الخبر هنا]]></summary>
                <updated>2026-10-07T10:50:39+03:00</updated>
                </entry>
                </feed>
                """;

            var items = FeedParser.Parse(xml, "atom");

            Assert.Single(items);
            var item = items[0];
            Assert.Equal("بنك ينمو بقوة", item.Title);
            Assert.Equal("https://example.com/AR/article2", item.Link);
            Assert.Equal("ملخص الخبر هنا", item.Summary);
            Assert.False(item.DateGuessed);
            Assert.Equal(new DateTime(2026, 10, 7, 7, 50, 39, DateTimeKind.Utc), item.PublishedAtUtc);
        }

        [Fact]
        public void Parse_Rdf_ExtractsItemsUsingDcDateFallback()
        {
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                         xmlns="http://purl.org/rss/1.0/"
                         xmlns:dc="http://purl.org/dc/elements/1.1/">
                <channel rdf:about="https://www.bis.org/press/">
                <title>BIS Press Releases</title>
                </channel>
                <item rdf:about="https://www.bis.org/press/p261007.htm">
                <title>Basel III applicable in almost all member jurisdictions</title>
                <link>https://www.bis.org/press/p261007.htm</link>
                <description>The Basel Committee announced today that Basel III capital standards are now in force.</description>
                <dc:date>2026-10-05T00:00:00Z</dc:date>
                </item>
                </rdf:RDF>
                """;

            var items = FeedParser.Parse(xml, "rdf");

            Assert.Single(items);
            var item = items[0];
            Assert.Equal("Basel III applicable in almost all member jurisdictions", item.Title);
            Assert.Equal("https://www.bis.org/press/p261007.htm", item.Link);
            Assert.Contains("Basel Committee", item.Summary);
            Assert.False(item.DateGuessed);
            Assert.Equal(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc), item.PublishedAtUtc);
        }

        [Fact]
        public void Parse_TrailingScriptInjectedAfterRootCloses_IsTolerated()
        {
            // Mirrors a real Cloudflare quirk observed on almamlakatv.com/rss.xml:
            // a bot-challenge <script> tag gets appended after </rss> closes,
            // which otherwise breaks strict XML parsing (multiple root elements).
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <rss version="2.0"><channel><item>
                <title>Real item survives the WAF injection</title>
                <link>https://example.com/survivor</link>
                <description>text</description>
                <pubDate>Wed, 07 Oct 2026 10:03:38 +0300</pubDate>
                </item></channel></rss><script>(function(){/* cf challenge */})();</script>
                """;

            var items = FeedParser.Parse(xml, "rss");

            Assert.Single(items);
            Assert.Equal("Real item survives the WAF injection", items[0].Title);
        }

        [Fact]
        public void Parse_MalformedXml_ReturnsEmptyList_NeverThrows()
        {
            const string xml = "<rss><channel><item><title>unterminated";

            var items = FeedParser.Parse(xml, "rss");

            Assert.Empty(items);
        }

        [Fact]
        public void Parse_EmptyOrNullInput_ReturnsEmptyList()
        {
            Assert.Empty(FeedParser.Parse("", "rss"));
            Assert.Empty(FeedParser.Parse(null!, "rss"));
        }

        [Fact]
        public void Parse_Rss20_SummaryIsTruncatedTo600Chars()
        {
            var longText = new string('a', 1000);
            var xml = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <rss version="2.0"><channel><item>
                <title>Long summary</title>
                <link>https://example.com/long</link>
                <description>{longText}</description>
                <pubDate>Wed, 07 Oct 2026 10:03:38 +0300</pubDate>
                </item></channel></rss>
                """;

            var items = FeedParser.Parse(xml, "rss");

            Assert.Equal(600, items[0].Summary.Length);
        }
    }
}
