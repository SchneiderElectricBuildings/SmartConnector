using System;
using NUnit.Framework;

namespace SmartConnector.WeatherExtension.Tests
{
    [TestFixture]
    public class ResponseParserFixture
    {
        // Sample responses matching the OpenWeatherMap free-tier schema
        // (Geocoding, Current Weather 2.5, Forecast 2.5).

        private const string GeocodeJson =
            "[{\"name\":\"London\",\"local_names\":{\"en\":\"London\"}," +
            "\"lat\":51.5073219,\"lon\":-0.1276474,\"country\":\"GB\",\"state\":\"England\"}]";

        private const string CurrentJson =
            "{\"coord\":{\"lon\":-0.1257,\"lat\":51.5085}," +
            "\"weather\":[{\"id\":803,\"main\":\"Clouds\",\"description\":\"broken clouds\",\"icon\":\"04d\"}]," +
            "\"base\":\"stations\",\"main\":{\"temp\":14.2,\"feels_like\":13.6,\"temp_min\":12.8," +
            "\"temp_max\":15.4,\"pressure\":1014,\"humidity\":72},\"visibility\":10000," +
            "\"wind\":{\"speed\":4.1,\"deg\":230},\"clouds\":{\"all\":75},\"dt\":1718020800," +
            "\"sys\":{\"type\":2,\"country\":\"GB\",\"sunrise\":1717990000,\"sunset\":1718050000}," +
            "\"timezone\":3600,\"id\":2643743,\"name\":\"London\",\"cod\":200}";

        private const string ForecastJson =
            "{\"cod\":\"200\",\"message\":0,\"cnt\":3,\"list\":[" +
            "{\"dt\":1718031600,\"main\":{\"temp\":13.0,\"temp_min\":12.0,\"temp_max\":14.0," +
            "\"pressure\":1013,\"humidity\":70},\"weather\":[{\"description\":\"light rain\"}]," +
            "\"dt_txt\":\"2024-06-10 15:00:00\"}," +
            "{\"dt\":1718110800,\"main\":{\"temp\":16.0,\"temp_min\":15.0,\"temp_max\":18.0," +
            "\"pressure\":1015,\"humidity\":60},\"weather\":[{\"description\":\"clear sky\"}]," +
            "\"dt_txt\":\"2024-06-11 13:00:00\"}," +
            "{\"dt\":1718197200,\"main\":{\"temp\":17.0,\"temp_min\":16.0,\"temp_max\":19.0," +
            "\"pressure\":1016,\"humidity\":55},\"weather\":[{\"description\":\"few clouds\"}]," +
            "\"dt_txt\":\"2024-06-12 13:00:00\"}]," +
            "\"city\":{\"id\":2643743,\"name\":\"London\",\"coord\":{\"lat\":51.5085,\"lon\":-0.1257}," +
            "\"country\":\"GB\",\"timezone\":3600}}";

        // ── Geocoding ──────────────────────────────────────────────────

        [Test]
        public void ParseGeocode_ReturnsFirstMatch()
        {
            var geo = OpenWeatherMapResponseParser.ParseGeocode(GeocodeJson);
            Assert.That(geo, Is.Not.Null);
            Assert.That(geo.Name, Is.EqualTo("London"));
            Assert.That(geo.Country, Is.EqualTo("GB"));
            Assert.That(geo.State, Is.EqualTo("England"));
            Assert.That(geo.Lat, Is.EqualTo(51.5073219).Within(1e-6));
            Assert.That(geo.Lon, Is.EqualTo(-0.1276474).Within(1e-6));
        }

        [Test]
        public void ParseGeocode_EmptyArray_ReturnsNull()
        {
            Assert.That(OpenWeatherMapResponseParser.ParseGeocode("[]"), Is.Null);
        }

        [Test]
        public void ParseGeocode_Empty_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => OpenWeatherMapResponseParser.ParseGeocode(""));
        }

        // ── Current ────────────────────────────────────────────────────

        [Test]
        public void ParseCurrent_ExtractsMainFields()
        {
            var c = OpenWeatherMapResponseParser.ParseCurrent(CurrentJson);
            Assert.That(c.Temperature, Is.EqualTo(14.2));
            Assert.That(c.FeelsLike, Is.EqualTo(13.6));
            Assert.That(c.Pressure, Is.EqualTo(1014));
            Assert.That(c.Humidity, Is.EqualTo(72));
            Assert.That(c.WindSpeed, Is.EqualTo(4.1));
            Assert.That(c.Description, Is.EqualTo("broken clouds"));
        }

        [Test]
        public void ParseCurrent_ConvertsUnixTimestamp()
        {
            var c = OpenWeatherMapResponseParser.ParseCurrent(CurrentJson);
            var expected = DateTimeOffset.FromUnixTimeSeconds(1718020800).UtcDateTime;
            Assert.That(c.ObservedAtUtc, Is.EqualTo(expected));
            Assert.That(c.ObservedAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        // ── Forecast ───────────────────────────────────────────────────

        [Test]
        public void ParseForecast_ReturnsAllPointsAndTimezone()
        {
            var f = OpenWeatherMapResponseParser.ParseForecast(ForecastJson);
            Assert.That(f.Points.Count, Is.EqualTo(3));
            Assert.That(f.TimezoneOffsetSeconds, Is.EqualTo(3600));
        }

        [Test]
        public void ParseForecast_FirstPointFields()
        {
            var f = OpenWeatherMapResponseParser.ParseForecast(ForecastJson);
            var p = f.Points[0];
            Assert.That(p.Temp, Is.EqualTo(13.0));
            Assert.That(p.TempMin, Is.EqualTo(12.0));
            Assert.That(p.TempMax, Is.EqualTo(14.0));
            Assert.That(p.Pressure, Is.EqualTo(1013));
            Assert.That(p.Humidity, Is.EqualTo(70));
            Assert.That(p.Description, Is.EqualTo("light rain"));
            Assert.That(p.TimestampUtc,
                Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1718031600).UtcDateTime));
        }
    }
}
