using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SmartConnector.WeatherExtension.Tests
{
    [TestFixture]
    public class ForecastAggregatorFixture
    {
        private static readonly DateTime Reference =
            new DateTime(2024, 6, 10, 12, 0, 0, DateTimeKind.Utc);

        private static ForecastPoint Point(
            string utc, double min, double max, double pressure, double humidity, string desc)
        {
            return new ForecastPoint
            {
                TimestampUtc = DateTime.Parse(utc,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal
                    | System.Globalization.DateTimeStyles.AssumeUniversal),
                Temp = (min + max) / 2,
                TempMin = min,
                TempMax = max,
                Pressure = pressure,
                Humidity = humidity,
                Description = desc
            };
        }

        private static List<ForecastPoint> SamplePoints()
        {
            return new List<ForecastPoint>
            {
                // Same day as reference — must be excluded
                Point("2024-06-10T18:00:00Z", 10, 16, 1009, 85, "drizzle"),
                // Day +1: two steps
                Point("2024-06-11T06:00:00Z", 8, 14, 1010, 80, "rain"),
                Point("2024-06-11T12:00:00Z", 12, 18, 1014, 60, "sunny"),
                // Day +2
                Point("2024-06-12T13:00:00Z", 14, 20, 1016, 50, "clear sky"),
                // Day +3
                Point("2024-06-13T12:00:00Z", 15, 22, 1018, 45, "hot"),
            };
        }

        [Test]
        public void Aggregate_ExcludesReferenceDayAndRollsUpDaily()
        {
            var days = ForecastAggregator.Aggregate(SamplePoints(), 0, Reference, 3);

            Assert.That(days.Count, Is.EqualTo(3));
            Assert.That(days[0].Date, Is.EqualTo(new DateTime(2024, 6, 11)));

            var d1 = days[0];
            Assert.That(d1.HighTemp, Is.EqualTo(18));      // max of temp_max
            Assert.That(d1.LowTemp, Is.EqualTo(8));        // min of temp_min
            Assert.That(d1.Pressure, Is.EqualTo(1012));    // avg(1010,1014)
            Assert.That(d1.Humidity, Is.EqualTo(70));      // avg(80,60)
            Assert.That(d1.Description, Is.EqualTo("sunny")); // step nearest local noon
        }

        [Test]
        public void Aggregate_RespectsTimezoneOffsetWhenBucketingDays()
        {
            // A point at 23:00 UTC falls on the next local day at +2h offset.
            var points = new List<ForecastPoint>
            {
                Point("2024-06-11T23:00:00Z", 10, 15, 1012, 70, "night")
            };
            var days = ForecastAggregator.Aggregate(points, 7200, Reference, 3);
            Assert.That(days.Count, Is.EqualTo(1));
            Assert.That(days[0].Date, Is.EqualTo(new DateTime(2024, 6, 12)));
        }

        [Test]
        public void Aggregate_CapsToRequestedDayCount()
        {
            var days = ForecastAggregator.Aggregate(SamplePoints(), 0, Reference, 2);
            Assert.That(days.Count, Is.EqualTo(2));
        }

        [Test]
        public void Aggregate_EmptyOrNull_ReturnsEmpty()
        {
            Assert.That(ForecastAggregator.Aggregate(null, 0, Reference, 3), Is.Empty);
            Assert.That(ForecastAggregator.Aggregate(
                new List<ForecastPoint>(), 0, Reference, 3), Is.Empty);
        }
    }
}
