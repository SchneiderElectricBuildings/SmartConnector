// ---------------------------------------------------------------------------
// Processor Test Fixture
//
// Integration test fixture following the IProcessorTestFixture<T> pattern
// from Developer Guide Section 8.4.
// ---------------------------------------------------------------------------

using Mongoose.Test;
using Mongoose.Test.Processors;
using NUnit.Framework;

namespace SmartConnector.GridStatusExtension.Tests
{
    [TestFixture]
    public class ProcessorFixture : SmartConnectorTestFixtureBase,
        IProcessorTestFixture<GridStatusProcessor>
    {
        protected override void FixtureOneTimeSetup_Base()
        {
        }

        public GridStatusProcessor CreateTestableProcessor()
        {
            return new GridStatusProcessor
            {
                ServerName = "Test Grid Status",
                Realm = "TestRealm",
                EwsAddress = "http://localhost:5300/TestGridStatus",
                UserName = "YOUR_USERNAME",
                Password = "YOUR_PASSWORD",
                Market = SupportedMarket.CAISO,
                ApiKey = "YOUR_API_KEY",
                ApiBaseUrl = "https://api.gridstatus.io/v1",
                RowLimit = 100,
                CheapPercentile = 25,
                ExpensivePercentile = 75,
                PeakRiskMediumThreshold = 0.85,
                PeakRiskHighThreshold = 0.95,
                CarbonCleanThreshold = 200,
                CarbonDirtyThreshold = 500,
                BackfillDays = 30,
                UpdatePrices = true,
                UpdateLoad = true,
                UpdateFuelMix = true
            };
        }

        // -- Validation Tests ------------------------------------------------

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
        public void ValidateTest_AllSupportedMarkets_Succeed()
        {
            foreach (SupportedMarket market in System.Enum.GetValues(typeof(SupportedMarket)))
            {
                var processor = CreateTestableProcessor();
                processor.Market = market;
                var results = this.ValidateProcessor(processor);
                Assert.That(results, Is.Empty,
                    $"Market '{market}' should validate cleanly");
            }
        }

        [Test]
        public void ValidateTest_DefaultPriceLocation_UsedWhenEmpty()
        {
            var processor = CreateTestableProcessor();
            processor.Market = SupportedMarket.ERCOT;
            processor.PriceLocation = null;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Empty,
                "Empty PriceLocation should validate (uses market default)");
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
        public void ValidateTest_CheapAboveExpensive_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.CheapPercentile = 80;
            processor.ExpensivePercentile = 60;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
        }

        [Test]
        public void ValidateTest_PeakRiskMediumAboveHigh_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.PeakRiskMediumThreshold = 0.98;
            processor.PeakRiskHighThreshold = 0.85;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
        }

        [Test]
        public void ValidateTest_CarbonCleanAboveDirty_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.CarbonCleanThreshold = 600;
            processor.CarbonDirtyThreshold = 200;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
        }

        // -- Integration Tests -----------------------------------------------
        // These require a running Smart Connector instance with StructureMap.
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
