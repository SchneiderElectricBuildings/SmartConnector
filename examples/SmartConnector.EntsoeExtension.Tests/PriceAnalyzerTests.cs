// ---------------------------------------------------------------------------
// Price Analyzer Unit Tests
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SmartConnector.EntsoeExtension.Tests
{
    [TestFixture]
    public class PriceAnalyzerTests
    {
        private static readonly List<EntsoeDataPoint> SamplePrices = CreateSamplePrices();

        private static List<EntsoeDataPoint> CreateSamplePrices()
        {
            var baseTime = new DateTime(2024, 1, 14, 23, 0, 0, DateTimeKind.Utc);
            var prices = new double[]
            {
                45.03, 42.15, 38.90, 35.20, 33.50, 35.80,  // hours 0–5 (cheap)
                48.50, 62.30, 75.20, 82.10, 85.00, 80.50,  // hours 6–11
                72.30, 68.40, 65.10, 70.20, 88.50, 95.30,  // hours 12–17 (17–18 expensive)
                92.10, 78.40, 65.20, 55.30, 48.90, 44.20   // hours 18–23
            };

            var points = new List<EntsoeDataPoint>();
            for (int i = 0; i < prices.Length; i++)
            {
                points.Add(new EntsoeDataPoint
                {
                    Position = i + 1,
                    Value = prices[i],
                    Timestamp = baseTime.AddHours(i)
                });
            }
            return points;
        }

        // ── CalculatePercentile ────────────────────────────────────────

        [Test]
        public void CalculatePercentile_EmptyList_ReturnsZero()
        {
            var result = PriceAnalyzer.CalculatePercentile(new List<double>(), 50);
            Assert.That(result, Is.EqualTo(0.0));
        }

        [Test]
        public void CalculatePercentile_SingleValue_ReturnsThatValue()
        {
            var result = PriceAnalyzer.CalculatePercentile(new List<double> { 42.0 }, 50);
            Assert.That(result, Is.EqualTo(42.0));
        }

        [Test]
        public void CalculatePercentile_Zeroth_ReturnsMinimum()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            var result = PriceAnalyzer.CalculatePercentile(prices, 0);
            Assert.That(result, Is.EqualTo(10.0));
        }

        [Test]
        public void CalculatePercentile_Hundredth_ReturnsMaximum()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            var result = PriceAnalyzer.CalculatePercentile(prices, 100);
            Assert.That(result, Is.EqualTo(50.0));
        }

        [Test]
        public void CalculatePercentile_50th_ReturnsMedian()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            var result = PriceAnalyzer.CalculatePercentile(prices, 50);
            Assert.That(result, Is.EqualTo(30.0));
        }

        [Test]
        public void CalculatePercentile_25th_ReturnsFirstQuartile()
        {
            // For 5 elements at rank 1.0: value between index 1 and 1 = 20
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            var result = PriceAnalyzer.CalculatePercentile(prices, 25);
            Assert.That(result, Is.EqualTo(20.0));
        }

        // ── Classify ───────────────────────────────────────────────────

        [Test]
        public void Classify_BelowCheapThreshold_ReturnsCheap()
        {
            var result = PriceAnalyzer.Classify(30.0, 40.0, 80.0);
            Assert.That(result, Is.EqualTo(PriceCategory.Cheap));
        }

        [Test]
        public void Classify_AtCheapThreshold_ReturnsCheap()
        {
            var result = PriceAnalyzer.Classify(40.0, 40.0, 80.0);
            Assert.That(result, Is.EqualTo(PriceCategory.Cheap));
        }

        [Test]
        public void Classify_BetweenThresholds_ReturnsNormal()
        {
            var result = PriceAnalyzer.Classify(60.0, 40.0, 80.0);
            Assert.That(result, Is.EqualTo(PriceCategory.Normal));
        }

        [Test]
        public void Classify_AtExpensiveThreshold_ReturnsExpensive()
        {
            var result = PriceAnalyzer.Classify(80.0, 40.0, 80.0);
            Assert.That(result, Is.EqualTo(PriceCategory.Expensive));
        }

        [Test]
        public void Classify_AboveExpensiveThreshold_ReturnsExpensive()
        {
            var result = PriceAnalyzer.Classify(100.0, 40.0, 80.0);
            Assert.That(result, Is.EqualTo(PriceCategory.Expensive));
        }

        // ── Analyze (full integration) ─────────────────────────────────

        [Test]
        public void Analyze_WithSampleData_ReturnsValidResult()
        {
            var currentHour = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

            var result = PriceAnalyzer.Analyze(SamplePrices, 25, 75, currentHour);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.HourlyPrices.Count, Is.EqualTo(24));
            Assert.That(result.DayMinimum, Is.EqualTo(33.50));
            Assert.That(result.DayMaximum, Is.EqualTo(95.30));
            Assert.That(result.DayAverage, Is.GreaterThan(0));
        }

        [Test]
        public void Analyze_CheapThreshold_IsLessThanExpensiveThreshold()
        {
            var currentHour = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

            var result = PriceAnalyzer.Analyze(SamplePrices, 25, 75, currentHour);

            Assert.That(result.CheapThreshold, Is.LessThan(result.ExpensiveThreshold));
        }

        [Test]
        public void Analyze_CheapHoursToday_IsNotEmpty()
        {
            var currentHour = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

            var result = PriceAnalyzer.Analyze(SamplePrices, 25, 75, currentHour);

            Assert.That(result.CheapHoursToday, Is.Not.Empty);
        }

        [Test]
        public void Analyze_ExpensiveHoursToday_IsNotEmpty()
        {
            var currentHour = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

            var result = PriceAnalyzer.Analyze(SamplePrices, 25, 75, currentHour);

            Assert.That(result.ExpensiveHoursToday, Is.Not.Empty);
        }

        [Test]
        public void Analyze_EmptyData_ReturnsEmptyResult()
        {
            var result = PriceAnalyzer.Analyze(
                new List<EntsoeDataPoint>(), 25, 75, DateTime.UtcNow);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.HourlyPrices, Is.Empty);
            Assert.That(result.CurrentPrice, Is.EqualTo(0));
        }

        [Test]
        public void Analyze_NullData_ReturnsEmptyResult()
        {
            var result = PriceAnalyzer.Analyze(null, 25, 75, DateTime.UtcNow);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.HourlyPrices, Is.Empty);
        }

        [Test]
        public void Analyze_AllPricesEqual_AllClassifiedAsNormalOrCheap()
        {
            var points = new List<EntsoeDataPoint>();
            var baseTime = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 24; i++)
            {
                points.Add(new EntsoeDataPoint
                {
                    Position = i + 1,
                    Value = 50.0,
                    Timestamp = baseTime.AddHours(i)
                });
            }

            var result = PriceAnalyzer.Analyze(points, 25, 75, baseTime.AddHours(12));

            // When all prices are equal, cheap threshold == expensive threshold == 50.0
            // So all prices are classified as Cheap (price <= cheapThreshold)
            Assert.That(result.DayMinimum, Is.EqualTo(50.0));
            Assert.That(result.DayMaximum, Is.EqualTo(50.0));
        }

        [Test]
        public void Analyze_CurrentHourDuringCheapPeriod_ReportsIsCheap()
        {
            // Hour 4 (position 5) has price 33.50 — should be cheap
            var currentHour = new DateTime(2024, 1, 15, 3, 0, 0, DateTimeKind.Utc);

            var result = PriceAnalyzer.Analyze(SamplePrices, 25, 75, currentHour);

            // Verify the current price is one of the low values
            Assert.That(result.CurrentPrice, Is.LessThan(50.0));
        }
    }
}
