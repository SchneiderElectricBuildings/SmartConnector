using Mongoose.Test;
using Mongoose.Test.Processors;
using NUnit.Framework;

namespace SmartConnector.WeatherExtension.Tests
{
    [TestFixture]
    public class ProcessorFixture : SmartConnectorTestFixtureBase,
        IProcessorTestFixture<WeatherProcessor>
    {
        protected override void FixtureOneTimeSetup_Base()
        {
        }

        public WeatherProcessor CreateTestableProcessor()
        {
            return new WeatherProcessor
            {
                ServerName = "Test Weather Service",
                Realm = "TestRealm",
                EwsAddress = "http://localhost:5300/TestWeatherService",
                UserName = "YOUR_USERNAME",
                Password = "YOUR_PASSWORD",
                ApiKey = "YOUR_OPENWEATHERMAP_KEY",
                City = "London,GB",
                Units = WeatherUnits.Metric,
                UpdateForecast = true
            };
        }

        // ── Validation Tests ───────────────────────────────────────────

        [Test]
        public void ValidateTest()
        {
            var processor = CreateTestableProcessor();
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Empty,
                "Default processor should validate cleanly");
        }

        [Test]
        public void ValidateTest_SameUserNameAndPassword_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.UserName = "SameValue";
            processor.Password = "SameValue";
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
        }

        [Test]
        public void ValidateTest_MissingApiKey_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.ApiKey = null;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
        }

        [Test]
        public void ValidateTest_EmptyCity_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.City = "";
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
        }

        [Test]
        public void ValidateTest_ImperialUnits_Succeeds()
        {
            var processor = CreateTestableProcessor();
            processor.Units = WeatherUnits.Imperial;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Empty);
        }

        // ── Integration Tests ──────────────────────────────────────────
        // These require a running Smart Connector instance with StructureMap
        // and an activated OpenWeatherMap API key.
        // Run manually with: dotnet test --filter "FullyQualifiedName~ExecuteTest"

        [Test, Explicit("Requires SmartConnector runtime (StructureMap)")]
        [Category("Integration")]
        public void ExecuteTest()
        {
            this.RunExecuteTest();
        }

        [Test, Explicit("Requires SmartConnector runtime (StructureMap)")]
        [Category("Integration")]
        public void CancelTest()
        {
            this.RunCancelTest();
        }
    }
}
