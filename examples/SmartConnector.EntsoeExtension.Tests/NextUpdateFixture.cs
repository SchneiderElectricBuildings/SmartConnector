using System;
using NUnit.Framework;

namespace SmartConnector.EntsoeExtension.Tests
{
    [TestFixture]
    public class NextUpdateFixture
    {
        private static readonly DateTime Now =
            new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ComputeNextUpdate_NoLastUpdate_ReturnsDefaultFiveMinutes()
        {
            var result = EntsoeProcessor.ComputeNextUpdate(null, Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(5)));
        }

        [Test]
        public void ComputeNextUpdate_MinValueLastUpdate_ReturnsDefault()
        {
            var result = EntsoeProcessor.ComputeNextUpdate(DateTime.MinValue, Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(5)));
        }

        [Test]
        public void ComputeNextUpdate_FutureLastUpdate_ReturnsDefault()
        {
            var result = EntsoeProcessor.ComputeNextUpdate(Now.AddHours(1), Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(5)));
        }

        [Test]
        public void ComputeNextUpdate_NormalInterval_ProjectsInterval()
        {
            var lastUpdate = Now.AddMinutes(-15);
            var result = EntsoeProcessor.ComputeNextUpdate(lastUpdate, Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(15)));
        }

        [Test]
        public void ComputeNextUpdate_HourlyInterval_ProjectsInterval()
        {
            var lastUpdate = Now.AddHours(-1);
            var result = EntsoeProcessor.ComputeNextUpdate(lastUpdate, Now);
            Assert.That(result, Is.EqualTo(Now.AddHours(1)));
        }

        [Test]
        public void ComputeNextUpdate_HugeGap_ClampedToOneDay()
        {
            // The bug: a ~2-year idle gap projected NextUpdate into 2028.
            var lastUpdate = Now.AddYears(-2);
            var result = EntsoeProcessor.ComputeNextUpdate(lastUpdate, Now);
            Assert.That(result, Is.EqualTo(Now.AddHours(24)));
            Assert.That(result.Year, Is.EqualTo(2026));
        }

        [Test]
        public void ComputeNextUpdate_GapJustOverOneDay_ClampedToOneDay()
        {
            var lastUpdate = Now.AddDays(-3);
            var result = EntsoeProcessor.ComputeNextUpdate(lastUpdate, Now);
            Assert.That(result, Is.EqualTo(Now.AddHours(24)));
        }

        [Test]
        public void ComputeNextUpdate_TinyGap_ClampedToOneMinute()
        {
            var lastUpdate = Now.AddSeconds(-10);
            var result = EntsoeProcessor.ComputeNextUpdate(lastUpdate, Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(1)));
        }
    }
}
