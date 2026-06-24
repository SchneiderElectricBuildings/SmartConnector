// ---------------------------------------------------------------------------
// Area Mapping Unit Tests
// ---------------------------------------------------------------------------

using NUnit.Framework;

namespace SmartConnector.EntsoeExtension.Tests
{
    [TestFixture]
    public class AreaMappingTests
    {
        [TestCase("NL", "10YNL----------L")]
        [TestCase("nl", "10YNL----------L")]
        [TestCase("DE_LU", "10Y1001A1001A82H")]
        [TestCase("FR", "10YFR-RTE------C")]
        [TestCase("NO_1", "10YNO-1--------2")]
        [TestCase("GB", "10YGB----------A")]
        [TestCase("SE_3", "10Y1001A1001A46L")]
        public void Lookup_ValidCode_ReturnsCorrectEicCode(
            string countryCode, string expectedEic)
        {
            var area = AreaMapping.Lookup(countryCode);

            Assert.That(area, Is.Not.Null,
                $"Area should be found for code '{countryCode}'");
            Assert.That(area.EicCode, Is.EqualTo(expectedEic));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        [TestCase("XX")]
        [TestCase("INVALID")]
        public void Lookup_InvalidCode_ReturnsNull(string countryCode)
        {
            var area = AreaMapping.Lookup(countryCode);
            Assert.That(area, Is.Null);
        }

        [Test]
        public void Lookup_IsCaseInsensitive()
        {
            var upper = AreaMapping.Lookup("NL");
            var lower = AreaMapping.Lookup("nl");
            var mixed = AreaMapping.Lookup("Nl");

            Assert.That(upper, Is.Not.Null);
            Assert.That(lower, Is.Not.Null);
            Assert.That(mixed, Is.Not.Null);
            Assert.That(upper.EicCode, Is.EqualTo(lower.EicCode));
            Assert.That(upper.EicCode, Is.EqualTo(mixed.EicCode));
        }

        [Test]
        public void Lookup_ReturnsDescription()
        {
            var area = AreaMapping.Lookup("NL");

            Assert.That(area.Description, Is.Not.Null.And.Not.Empty);
            Assert.That(area.Description, Does.Contain("Netherlands"));
        }

        [Test]
        public void Lookup_ReturnsTimeZoneId()
        {
            var area = AreaMapping.Lookup("NL");

            Assert.That(area.TimeZoneId, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void SupportedCodes_ContainsExpectedEntries()
        {
            var codes = AreaMapping.SupportedCodes;

            Assert.That(codes, Is.Not.Empty);
            Assert.That(codes, Does.Contain("NL"));
            Assert.That(codes, Does.Contain("DE_LU"));
            Assert.That(codes, Does.Contain("FR"));
            Assert.That(codes, Does.Contain("GB"));
        }

        [Test]
        public void GetTimeZone_ValidCode_ReturnsTimeZoneInfo()
        {
            var tz = AreaMapping.GetTimeZone("NL");

            Assert.That(tz, Is.Not.Null);
        }

        [Test]
        public void GetTimeZone_InvalidCode_ReturnsNull()
        {
            var tz = AreaMapping.GetTimeZone("XX");

            Assert.That(tz, Is.Null);
        }

        [TestCase("NL")]
        [TestCase("DE_LU")]
        [TestCase("FR")]
        [TestCase("AT")]
        [TestCase("BE")]
        [TestCase("CH")]
        public void Lookup_AllMajorCountries_HaveValidEicCodes(string code)
        {
            var area = AreaMapping.Lookup(code);

            Assert.That(area, Is.Not.Null);
            Assert.That(area.EicCode, Does.StartWith("10Y").Or.StartWith("10Z"));
            Assert.That(area.EicCode.Length, Is.GreaterThanOrEqualTo(16));
        }
    }
}
