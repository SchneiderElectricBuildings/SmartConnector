using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SmartConnector.EntsoeExtension.Tests
{
    [TestFixture]
    public class LoadAnalyzerFixture
    {
        private static EntsoeDataPoint Point(double value, int hour)
        {
            return new EntsoeDataPoint
            {
                Value = value,
                Timestamp = new DateTime(2026, 6, 9, hour, 0, 0, DateTimeKind.Utc)
            };
        }

        // -- FindForecastPeak ----------------------------------------------------

        [Test]
        public void FindForecastPeak_EmptyList_ReturnsZeroAndMinValue()
        {
            var (peak, hour) = LoadAnalyzer.FindForecastPeak(
                new List<EntsoeDataPoint>());
            Assert.That(peak, Is.EqualTo(0.0));
            Assert.That(hour, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void FindForecastPeak_Null_ReturnsZeroAndMinValue()
        {
            var (peak, hour) = LoadAnalyzer.FindForecastPeak(null);
            Assert.That(peak, Is.EqualTo(0.0));
            Assert.That(hour, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void FindForecastPeak_ReturnsMaxValueAndItsHour()
        {
            var points = new List<EntsoeDataPoint>
            {
                Point(5000, 6),
                Point(8200, 18),
                Point(6100, 12)
            };

            var (peak, hour) = LoadAnalyzer.FindForecastPeak(points);
            Assert.That(peak, Is.EqualTo(8200));
            Assert.That(hour.Hour, Is.EqualTo(18));
        }

        [Test]
        public void FindForecastPeak_SinglePoint_ReturnsThatPoint()
        {
            var points = new List<EntsoeDataPoint> { Point(4200, 9) };
            var (peak, hour) = LoadAnalyzer.FindForecastPeak(points);
            Assert.That(peak, Is.EqualTo(4200));
            Assert.That(hour.Hour, Is.EqualTo(9));
        }

        // -- ComputePeakRiskLevel ------------------------------------------------

        [Test]
        public void ComputePeakRiskLevel_ForecastPeakZero_ReturnsLow()
        {
            Assert.That(LoadAnalyzer.ComputePeakRiskLevel(5000, 0), Is.EqualTo(0));
        }

        [TestCase(5000.0, 10000.0, 0)]   // 50 % of peak → low
        [TestCase(8600.0, 10000.0, 1)]   // 86 % of peak → medium
        [TestCase(9600.0, 10000.0, 2)]   // 96 % of peak → high
        [TestCase(10000.0, 10000.0, 2)]  // at peak → high
        public void ComputePeakRiskLevel_ReturnsExpectedLevel(
            double current, double peak, int expected)
        {
            Assert.That(
                LoadAnalyzer.ComputePeakRiskLevel(current, peak),
                Is.EqualTo(expected));
        }

        [Test]
        public void ComputePeakRiskLevel_CustomThresholds_Respected()
        {
            // medium at 0.5, high at 0.7
            Assert.That(
                LoadAnalyzer.ComputePeakRiskLevel(600, 1000, 0.5, 0.7),
                Is.EqualTo(1));
            Assert.That(
                LoadAnalyzer.ComputePeakRiskLevel(800, 1000, 0.5, 0.7),
                Is.EqualTo(2));
        }
    }
}
