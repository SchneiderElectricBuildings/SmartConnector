// ---------------------------------------------------------------------------
// ENTSO-E Response Parser Unit Tests
// ---------------------------------------------------------------------------

using System;
using System.IO;
using NUnit.Framework;

namespace SmartConnector.EntsoeExtension.Tests
{
    [TestFixture]
    public class EntsoeResponseParserTests
    {
        private string _priceXml;
        private string _loadXml;

        [OneTimeSetUp]
        public void LoadTestData()
        {
            var testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");
            _priceXml = File.ReadAllText(Path.Combine(testDataDir, "DayAheadPricesResponse.xml"));
            _loadXml = File.ReadAllText(Path.Combine(testDataDir, "ActualLoadResponse.xml"));
        }

        // ── Price Document Parsing ─────────────────────────────────────

        [Test]
        public void Parse_PriceDocument_ReturnsDocumentTypeA44()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            Assert.That(result.DocumentType, Is.EqualTo("A44"));
        }

        [Test]
        public void Parse_PriceDocument_HasOneTimeSeries()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            Assert.That(result.TimeSeries, Has.Count.EqualTo(1));
        }

        [Test]
        public void Parse_PriceDocument_Has24DataPoints()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            var points = result.GetAllPoints();
            Assert.That(points.Count, Is.EqualTo(24));
        }

        [Test]
        public void Parse_PriceDocument_FirstPointHasCorrectValue()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            var points = result.GetAllPoints();
            Assert.That(points[0].Value, Is.EqualTo(45.03));
        }

        [Test]
        public void Parse_PriceDocument_LastPointHasCorrectValue()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            var points = result.GetAllPoints();
            Assert.That(points[23].Value, Is.EqualTo(44.20));
        }

        [Test]
        public void Parse_PriceDocument_ExtractsCurrency()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            Assert.That(result.TimeSeries[0].Currency, Is.EqualTo("EUR"));
        }

        [Test]
        public void Parse_PriceDocument_ExtractsMeasureUnit()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            Assert.That(result.TimeSeries[0].MeasureUnit, Is.EqualTo("MWH"));
        }

        [Test]
        public void Parse_PriceDocument_PointsHaveSequentialPositions()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            var points = result.GetAllPoints();

            for (int i = 0; i < points.Count; i++)
            {
                Assert.That(points[i].Position, Is.EqualTo(i + 1),
                    $"Point at index {i} should have position {i + 1}");
            }
        }

        [Test]
        public void Parse_PriceDocument_TimestampsAreHourlyFromPeriodStart()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            var points = result.GetAllPoints();

            var expectedStart = new DateTime(2024, 1, 14, 23, 0, 0, DateTimeKind.Utc);
            Assert.That(points[0].Timestamp, Is.EqualTo(expectedStart));
            Assert.That(points[1].Timestamp, Is.EqualTo(expectedStart.AddHours(1)));
        }

        [Test]
        public void Parse_PriceDocument_PeriodHasHourlyResolution()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            var period = result.TimeSeries[0].Periods[0];
            Assert.That(period.Resolution, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void Parse_PriceDocument_GetLatestPoint_ReturnsLastHour()
        {
            var result = EntsoeResponseParser.Parse(_priceXml);
            var latest = result.GetLatestPoint();

            Assert.That(latest, Is.Not.Null);
            Assert.That(latest.Position, Is.EqualTo(24));
            Assert.That(latest.Value, Is.EqualTo(44.20));
        }

        // ── Load Document Parsing ──────────────────────────────────────

        [Test]
        public void Parse_LoadDocument_ReturnsDocumentTypeA65()
        {
            var result = EntsoeResponseParser.Parse(_loadXml);
            Assert.That(result.DocumentType, Is.EqualTo("A65"));
        }

        [Test]
        public void Parse_LoadDocument_Has16QuarterHourlyPoints()
        {
            var result = EntsoeResponseParser.Parse(_loadXml);
            var points = result.GetAllPoints();
            Assert.That(points.Count, Is.EqualTo(16));
        }

        [Test]
        public void Parse_LoadDocument_FirstPointIsLoad()
        {
            var result = EntsoeResponseParser.Parse(_loadXml);
            var points = result.GetAllPoints();
            Assert.That(points[0].Value, Is.EqualTo(12500));
        }

        [Test]
        public void Parse_LoadDocument_PeriodHas15MinResolution()
        {
            var result = EntsoeResponseParser.Parse(_loadXml);
            var period = result.TimeSeries[0].Periods[0];
            Assert.That(period.Resolution, Is.EqualTo(TimeSpan.FromMinutes(15)));
        }

        [Test]
        public void Parse_LoadDocument_ExtractsMeasureUnit()
        {
            var result = EntsoeResponseParser.Parse(_loadXml);
            Assert.That(result.TimeSeries[0].MeasureUnit, Is.EqualTo("MAW"));
        }

        // ── Error Handling ─────────────────────────────────────────────

        [Test]
        public void Parse_NullInput_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => EntsoeResponseParser.Parse(null));
        }

        [Test]
        public void Parse_EmptyString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => EntsoeResponseParser.Parse(""));
        }

        [Test]
        public void Parse_InvalidXml_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(
                () => EntsoeResponseParser.Parse("<not valid xml"));
        }

        [Test]
        public void Parse_AcknowledgementError_ThrowsFormatException()
        {
            var errorXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <Acknowledgement_MarketDocument xmlns=""urn:iec62325.351:tc57wg16:451-1:acknowledgementdocument:7:0"">
                    <Reason>
                        <code>999</code>
                        <text>No matching data found</text>
                    </Reason>
                </Acknowledgement_MarketDocument>";

            var ex = Assert.Throws<FormatException>(
                () => EntsoeResponseParser.Parse(errorXml));
            Assert.That(ex.Message, Does.Contain("No matching data found"));
        }

        // ── Resolution Parsing ─────────────────────────────────────────

        [TestCase("PT15M", 15)]
        [TestCase("PT30M", 30)]
        [TestCase("PT60M", 60)]
        public void ParseResolution_StandardResolutions_ReturnsCorrectMinutes(
            string resolution, int expectedMinutes)
        {
            var result = EntsoeResponseParser.ParseResolution(resolution);
            Assert.That(result.TotalMinutes, Is.EqualTo(expectedMinutes));
        }

        [Test]
        public void ParseResolution_UnknownResolution_DefaultsToOneHour()
        {
            var result = EntsoeResponseParser.ParseResolution("PT999M");
            Assert.That(result, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        // ── Timestamp Formatting ───────────────────────────────────────

        [Test]
        public void FormatTimestamp_ProducesExpectedFormat()
        {
            var dt = new DateTime(2024, 1, 15, 23, 0, 0, DateTimeKind.Utc);
            var result = EntsoeApiClient.FormatTimestamp(dt);
            Assert.That(result, Is.EqualTo("202401152300"));
        }
    }
}
