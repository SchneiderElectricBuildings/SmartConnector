using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SmartConnector.GridStatusExtension.Tests
{
    [TestFixture]
    public class BackfillPlannerFixture
    {
        private static readonly DateTime Now =
            new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

        private static DataRow Row(int hour)
        {
            return new DataRow
            {
                IntervalStartUtc =
                    new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc).AddHours(hour),
                Columns = new Dictionary<string, object>()
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
        public void Create_ZeroBackfillDays_DoesNotBackfill()
        {
            var plan = BackfillPlanner.Create(null, Now, 0);
            Assert.That(plan.ShouldBackfill, Is.False);
        }

        // -- Plan: reconnection gap ---------------------------------------------

        [Test]
        public void Create_SmallGap_DoesNotBackfill()
        {
            var plan = BackfillPlanner.Create(Now.AddMinutes(-15), Now, 30);
            Assert.That(plan.ShouldBackfill, Is.False);
        }

        [Test]
        public void Create_GapAboveThreshold_PlansGapBackfill()
        {
            var lastUpdate = Now.AddHours(-6);
            var plan = BackfillPlanner.Create(lastUpdate, Now, 30);
            Assert.That(plan.ShouldBackfill, Is.True);
            Assert.That(plan.Start, Is.EqualTo(lastUpdate));
            Assert.That(plan.FilterAfter, Is.EqualTo(lastUpdate));
        }

        [Test]
        public void Create_GapLongerThanBackfillDays_CapsWindow()
        {
            var lastUpdate = Now.AddDays(-60);
            var plan = BackfillPlanner.Create(lastUpdate, Now, 30);
            Assert.That(plan.ShouldBackfill, Is.True);
            Assert.That(plan.Start, Is.EqualTo(Now.AddDays(-30)));
            Assert.That(plan.FilterAfter, Is.EqualTo(lastUpdate));
        }

        // -- FilterRows ----------------------------------------------------------

        [Test]
        public void FilterRows_Null_ReturnsEmpty()
        {
            Assert.That(BackfillPlanner.FilterRows(null, null), Is.Empty);
        }

        [Test]
        public void FilterRows_NoFilter_KeepsValidRows()
        {
            var rows = new List<DataRow> { Row(1), Row(2), Row(3) };
            var result = BackfillPlanner.FilterRows(rows, null);
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public void FilterRows_DropsMinValueTimestamps()
        {
            var rows = new List<DataRow>
            {
                Row(1),
                new DataRow { IntervalStartUtc = DateTime.MinValue, Columns = new Dictionary<string, object>() },
                Row(2)
            };
            var result = BackfillPlanner.FilterRows(rows, null);
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void FilterRows_WithFilter_KeepsOnlyLaterRows()
        {
            var rows = new List<DataRow> { Row(8), Row(9), Row(10), Row(11) };
            var filterAfter = new DateTime(2026, 6, 9, 9, 0, 0, DateTimeKind.Utc);

            var result = BackfillPlanner.FilterRows(rows, filterAfter);

            Assert.That(result.Count, Is.EqualTo(2));
            foreach (var r in result)
                Assert.That(r.IntervalStartUtc, Is.GreaterThan(filterAfter));
        }

        [Test]
        public void FilterRows_ReturnsOrderedByTimestamp()
        {
            var rows = new List<DataRow> { Row(10), Row(6), Row(22), Row(1) };
            var result = BackfillPlanner.FilterRows(rows, null);

            for (int i = 1; i < result.Count; i++)
                Assert.That(result[i].IntervalStartUtc,
                    Is.GreaterThanOrEqualTo(result[i - 1].IntervalStartUtc));
        }
    }
}
