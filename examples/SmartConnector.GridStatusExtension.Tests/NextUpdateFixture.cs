using System;
using NUnit.Framework;

namespace SmartConnector.GridStatusExtension.Tests
{
    [TestFixture]
    public class NextUpdateFixture
    {
        private static readonly DateTime Now =
            new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ComputeNextUpdate_NoLastUpdate_ReturnsDefaultFiveMinutes()
        {
            var result = GridStatusProcessor.ComputeNextUpdate(null, Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(5)));
        }

        [Test]
        public void ComputeNextUpdate_MinValueLastUpdate_ReturnsDefault()
        {
            var result = GridStatusProcessor.ComputeNextUpdate(DateTime.MinValue, Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(5)));
        }

        [Test]
        public void ComputeNextUpdate_FutureLastUpdate_ReturnsDefault()
        {
            var result = GridStatusProcessor.ComputeNextUpdate(Now.AddHours(1), Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(5)));
        }

        [Test]
        public void ComputeNextUpdate_NormalInterval_ProjectsInterval()
        {
            var lastUpdate = Now.AddMinutes(-15);
            var result = GridStatusProcessor.ComputeNextUpdate(lastUpdate, Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(15)));
        }

        [Test]
        public void ComputeNextUpdate_HugeGap_ClampedToOneDay()
        {
            // A long idle gap must not project NextUpdate years into the future.
            var lastUpdate = Now.AddYears(-2);
            var result = GridStatusProcessor.ComputeNextUpdate(lastUpdate, Now);
            Assert.That(result, Is.EqualTo(Now.AddHours(24)));
            Assert.That(result.Year, Is.EqualTo(2026));
        }

        [Test]
        public void ComputeNextUpdate_TinyGap_ClampedToOneMinute()
        {
            var lastUpdate = Now.AddSeconds(-10);
            var result = GridStatusProcessor.ComputeNextUpdate(lastUpdate, Now);
            Assert.That(result, Is.EqualTo(Now.AddMinutes(1)));
        }
    }
}
