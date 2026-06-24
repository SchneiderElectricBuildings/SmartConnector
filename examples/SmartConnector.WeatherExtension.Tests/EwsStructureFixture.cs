using NUnit.Framework;

namespace SmartConnector.WeatherExtension.Tests
{
    [TestFixture]
    public class EwsStructureFixture
    {
        [Test]
        public void Root_IsWeather()
        {
            Assert.That(EwsStructure.Root, Is.EqualTo("/Weather"));
        }

        [Test]
        public void RootMeta_Paths()
        {
            Assert.That(EwsStructure.MetaLastUpdate, Is.EqualTo("/Weather/Meta/LastUpdate"));
            Assert.That(EwsStructure.MetaNextUpdate, Is.EqualTo("/Weather/Meta/NextUpdate"));
            Assert.That(EwsStructure.MetaDataHealthy, Is.EqualTo("/Weather/Meta/DataHealthy"));
            Assert.That(EwsStructure.MetaLastError, Is.EqualTo("/Weather/Meta/LastError"));
            Assert.That(EwsStructure.MetaCycleCount, Is.EqualTo("/Weather/Meta/CycleCount"));
            Assert.That(EwsStructure.MetaApiCalls, Is.EqualTo("/Weather/Meta/ApiCallsTotal"));
            Assert.That(EwsStructure.MetaTimeZone, Is.EqualTo("/Weather/Meta/TimeZone"));
        }

        [Test]
        public void Current_Paths()
        {
            Assert.That(EwsStructure.CurrentTemperature,
                Is.EqualTo("/Weather/Current/Temperature"));
            Assert.That(EwsStructure.CurrentTemperatureHistory,
                Is.EqualTo("/Weather/Current/TemperatureHistory"));
            Assert.That(EwsStructure.CurrentObservedAt,
                Is.EqualTo("/Weather/Current/ObservedAt"));
        }

        [Test]
        public void ForecastHelpers_BuildDayScopedPaths()
        {
            Assert.That(EwsStructure.ForecastDay(1), Is.EqualTo("/Weather/Forecast/Day1"));
            Assert.That(EwsStructure.ForecastDate(2),
                Is.EqualTo("/Weather/Forecast/Day2/Date"));
            Assert.That(EwsStructure.ForecastHighTemp(3),
                Is.EqualTo("/Weather/Forecast/Day3/HighTemp"));
            Assert.That(EwsStructure.ForecastLowTemp(1),
                Is.EqualTo("/Weather/Forecast/Day1/LowTemp"));
        }

        [Test]
        public void ForecastDays_IsThree()
        {
            Assert.That(EwsStructure.ForecastDays, Is.EqualTo(3));
        }

        [Test]
        public void TrendDefinitions_HasThreeCurrentConditionTrends()
        {
            Assert.That(EwsStructure.TrendDefinitions.Length, Is.EqualTo(3));
            foreach (var (trendId, valueId, name) in EwsStructure.TrendDefinitions)
            {
                Assert.That(trendId, Is.Not.Null.And.Not.Empty);
                Assert.That(valueId, Is.Not.Null.And.Not.Empty);
                Assert.That(name, Is.Not.Null.And.Not.Empty);
                Assert.That(trendId, Is.Not.EqualTo(valueId));
                Assert.That(trendId, Does.EndWith("History"));
            }
        }
    }
}
