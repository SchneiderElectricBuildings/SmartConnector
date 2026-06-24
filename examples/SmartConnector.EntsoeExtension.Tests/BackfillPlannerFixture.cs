using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SmartConnector.EntsoeExtension.Tests
{
    [TestFixture]
    public class BackfillPlannerFixture
    {
        private static readonly DateTime Now =
            new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

        private static EntsoeDataPoint Point(int dayOffset, int hour)
        {
            return new EntsoeDataPoint
            {
                Value = 100 + hour,
                Timestamp = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc)
                    .AddDays(dayOffset).AddHours(hour)
            };
        }

        // -- Plan: initial setup -------------------------------------------------

        [Test]
        public void Create_NoLastUpdate_PlansInitialBackfill()
        {
            var plan = BackfillPlanner.Create(null, Now, 30);
            Assert.That(plan.ShouldBackfill, Is.True);
            Assert.That(plan.Start, Is.EqualTo(Now.AddDays(-30)));
            Assert.That(plan.End, Is.EqualTo(Now));
            Assert.That(plan.FilterAfter, Is.Null);
        }

        [Test]
        public void Create_MinValueLastUpdate_PlansInitialBackfill()
        {
            var plan = BackfillPlanner.Create(DateTime.MinValue, Now, 30);
            Assert.That(plan.ShouldBackfill, Is.True);
            Assert.That(plan.FilterAfter, Is.Null);
        }

        // -- Plan: disabled ------------------------------------------------------

        [Test]
        public void Create_ZeroBackfillDays_DoesNotBackfill()
        {
            var plan = BackfillPlanner.Create(null, Now, 0);
            Assert.That(plan.ShouldBackfill, Is.False);
        }

        [Test]
        public void Create_NegativeBackfillDays_DoesNotBackfill()
        {
            var plan = BackfillPlanner.Create(null, Now, -5);
            Assert.That(plan.ShouldBackfill, Is.False);
        }

        // -- Plan: reconnection gap ---------------------------------------------

        [Test]
        public void Create_SmallGap_DoesNotBackfill()
        {
            // 15-minute gap — well under the threshold.
            var lastUpdate = Now.AddMinutes(-15);
            var plan = BackfillPlanner.Create(lastUpdate, Now, 30);
            Assert.That(plan.ShouldBackfill, Is.False);
        }

        [Test]
        public void Create_GapAboveThreshold_PlansGapBackfill()
        {
            // 6-hour gap — backfill only the missed window.
            var lastUpdate = Now.AddHours(-6);
            var plan = BackfillPlanner.Create(lastUpdate, Now, 30);
            Assert.That(plan.ShouldBackfill, Is.True);
            Assert.That(plan.Start, Is.EqualTo(lastUpdate));
            Assert.That(plan.FilterAfter, Is.EqualTo(lastUpdate));
        }

        [Test]
        public void Create_GapLongerThanBackfillDays_CapsWindow()
        {
            // Last update was 60 days ago but BackfillDays is 30.
            var lastUpdate = Now.AddDays(-60);
            var plan = BackfillPlanner.Create(lastUpdate, Now, 30);
            Assert.That(plan.ShouldBackfill, Is.True);
            Assert.That(plan.Start, Is.EqualTo(Now.AddDays(-30)),
                "Window should be capped to BackfillDays");
            Assert.That(plan.FilterAfter, Is.EqualTo(lastUpdate));
        }

        [Test]
        public void Create_GapJustUnderOneHour_DoesNotBackfill()
        {
            var lastUpdate = Now.AddMinutes(-59);
            var plan = BackfillPlanner.Create(lastUpdate, Now, 30);
            Assert.That(plan.ShouldBackfill, Is.False,
                "Hourly schedules should not trigger constant gap backfill");
        }

        // -- FilterPoints --------------------------------------------------------

        [Test]
        public void FilterPoints_Null_ReturnsEmpty()
        {
            var result = BackfillPlanner.FilterPoints(null, null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FilterPoints_NoFilter_KeepsAllValidPoints()
        {
            var points = new List<EntsoeDataPoint>
            {
                Point(0, 1), Point(0, 2), Point(0, 3)
            };
            var result = BackfillPlanner.FilterPoints(points, null);
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public void FilterPoints_DropsMinValueTimestamps()
        {
            var points = new List<EntsoeDataPoint>
            {
                Point(0, 1),
                new EntsoeDataPoint { Value = 5, Timestamp = DateTime.MinValue },
                Point(0, 2)
            };
            var result = BackfillPlanner.FilterPoints(points, null);
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void FilterPoints_WithFilter_KeepsOnlyLaterPoints()
        {
            var points = new List<EntsoeDataPoint>
            {
                Point(0, 8), Point(0, 9), Point(0, 10), Point(0, 11)
            };
            var filterAfter = new DateTime(2026, 6, 9, 9, 0, 0, DateTimeKind.Utc);

            var result = BackfillPlanner.FilterPoints(points, filterAfter);

            Assert.That(result.Count, Is.EqualTo(2));
            foreach (var p in result)
                Assert.That(p.Timestamp, Is.GreaterThan(filterAfter));
        }

        [Test]
        public void FilterPoints_ReturnsOrderedByTimestamp()
        {
            var points = new List<EntsoeDataPoint>
            {
                Point(0, 10), Point(0, 6), Point(0, 22), Point(0, 1)
            };
            var result = BackfillPlanner.FilterPoints(points, null);

            for (int i = 1; i < result.Count; i++)
                Assert.That(result[i].Timestamp,
                    Is.GreaterThanOrEqualTo(result[i - 1].Timestamp));
        }

        // -- ResampleHourly ------------------------------------------------------

        private static EntsoeDataPoint QuarterHourPoint(int hour, int minute)
        {
            return new EntsoeDataPoint
            {
                Value = hour * 100 + minute,
                Timestamp = new DateTime(2026, 6, 9, hour, minute, 0, DateTimeKind.Utc)
            };
        }

        [Test]
        public void ResampleHourly_Null_ReturnsEmpty()
        {
            Assert.That(BackfillPlanner.ResampleHourly(null), Is.Empty);
        }

        [Test]
        public void ResampleHourly_HourlyInput_Unchanged()
        {
            var points = new List<EntsoeDataPoint>
            {
                Point(0, 0), Point(0, 1), Point(0, 2)
            };
            var result = BackfillPlanner.ResampleHourly(points);
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public void ResampleHourly_QuarterHourInput_KeepsOnePerHour()
        {
            var points = new List<EntsoeDataPoint>
            {
                QuarterHourPoint(8, 0), QuarterHourPoint(8, 15),
                QuarterHourPoint(8, 30), QuarterHourPoint(8, 45),
                QuarterHourPoint(9, 0), QuarterHourPoint(9, 15)
            };
            var result = BackfillPlanner.ResampleHourly(points);
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void ResampleHourly_KeepsFirstPointOfEachHour()
        {
            var points = new List<EntsoeDataPoint>
            {
                QuarterHourPoint(8, 0), QuarterHourPoint(8, 15),
                QuarterHourPoint(8, 30)
            };
            var result = BackfillPlanner.ResampleHourly(points);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Timestamp.Minute, Is.EqualTo(0));
        }
    }
}
