using System;
using NUnit.Framework;

namespace SmartConnector.WeatherExtension.Tests
{
    [TestFixture]
    public class NextUpdateFixture
    {
        private static readonly DateTime Now =
            new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ComputeNextUpdate_NoLastUpdate_ReturnsDefaultFiveMinutes()
        {
            Assert.That(WeatherProcessor.ComputeNextUpdate(null, Now),
                Is.EqualTo(Now.AddMinutes(5)));
        }

        [Test]
        public void ComputeNextUpdate_FutureLastUpdate_ReturnsDefault()
        {
            Assert.That(WeatherProcessor.ComputeNextUpdate(Now.AddHours(1), Now),
                Is.EqualTo(Now.AddMinutes(5)));
        }

        [Test]
        public void ComputeNextUpdate_NormalInterval_ProjectsInterval()
        {
            Assert.That(WeatherProcessor.ComputeNextUpdate(Now.AddMinutes(-15), Now),
                Is.EqualTo(Now.AddMinutes(15)));
        }

        [Test]
        public void ComputeNextUpdate_HugeGap_ClampedToOneDay()
        {
            var result = WeatherProcessor.ComputeNextUpdate(Now.AddYears(-2), Now);
            Assert.That(result, Is.EqualTo(Now.AddHours(24)));
            Assert.That(result.Year, Is.EqualTo(2026));
        }

        [Test]
        public void ComputeNextUpdate_TinyGap_ClampedToOneMinute()
        {
            Assert.That(WeatherProcessor.ComputeNextUpdate(Now.AddSeconds(-10), Now),
                Is.EqualTo(Now.AddMinutes(1)));
        }
    }
}
