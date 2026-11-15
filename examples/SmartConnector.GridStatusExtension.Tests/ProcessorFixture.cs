using Mongoose.Common;
using Mongoose.Ews.Server.Data;
using Ews.Common;
using System.Linq;
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
            // The Execute tests need the Smart Connector container. It is started
            // by name, so this project builds without a reference to the service.
            System.Type.GetType("Mongoose.Service.Mongoose, Mongoose.Service")
                ?.GetMethod("InitIoC")?.Invoke(null, null);
        }

        public GridStatusProcessor CreateTestableProcessor()
        {
            return new GridStatusProcessor
            {
                ServerName = "Test Grid Status",
                Realm = "TestRealm",
                EwsAddress = "https://localhost:5300/TestGridStatus",
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
        public void ValidateTest_HttpAddress_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.EwsAddress = "http://localhost:5300/TestGridStatus";
            var results = this.ValidateProcessor(processor);
            Assert.That(
                results.Any(r => r.Message.Contains("AllowUnencryptedEwsForDevelopment")),
                Is.True);
        }

        [Test]
        public void ValidateTest_HttpAddressWithDevelopmentOptOut_Succeeds()
        {
            var processor = CreateTestableProcessor();
            processor.EwsAddress = "http://localhost:5300/TestGridStatus";
            processor.AllowUnencryptedEwsForDevelopment = true;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Empty);
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

        // After a run, the server must be on the security policy and every
        // value item must be read-only for EWS clients.
        [Test, Explicit("Requires SmartConnector runtime (StructureMap)")]
        [Category("Integration")]
        public void ExecuteTest_LeavesTheServerOnTheSecurityPolicy()
        {
            this.RunExecuteTest();

            var processor = CreateTestableProcessor();
            using (var adapter = EwsServerDataAdapter.ConnectExisting(
                $"{processor.ServerName} {processor.Market}", processor.UserName, processor.Password))
            {
                Assert.That(adapter.Server.AllowCookies, Is.False);
                Assert.That(adapter.Server.SupportedHashingAlgorithms.Sha256, Is.True);
                Assert.That(adapter.Server.SupportedHashingAlgorithms.Md5, Is.False);
                Assert.That(adapter.Server.SupportedMethods.SetValues, Is.False);
                Assert.That(adapter.Server.SupportedMethods.ForceValues, Is.False);
                Assert.That(adapter.Server.SupportedMethods.UnforceValues, Is.False);
                Assert.That(
                    EwsServerSecurity.PendingChanges(
                        adapter.Server.AllowCookies,
                        adapter.Server.SupportedHashingAlgorithms,
                        adapter.Server.SupportedMethods),
                    Is.Empty);
                Assert.That(
                    adapter.ValueItems.All(v => v.Writeable == EwsValueWriteableEnum.ReadOnly),
                    Is.True);
            }
        }

        // A server keeps the address it was created with. When that address
        // is http:// and the development property is off, the cycle must stop
        // before it writes any data. Run this against a new server.
        [Test, Explicit("Requires SmartConnector runtime (StructureMap)")]
        [Category("Integration")]
        public void ExecuteTest_ServerOnHttpWithoutTheOptOut_Stops()
        {
            var first = CreateTestableProcessor();
            first.ServerName = "Test Http Address Server";
            first.EwsAddress = first.EwsAddress.Replace("https://", "http://");
            first.AllowUnencryptedEwsForDevelopment = true;
            this.RunTestableProcessor(first);

            var second = CreateTestableProcessor();
            second.ServerName = "Test Http Address Server";
            var prompts = this.RunTestableProcessor(second);

            Assert.That(
                prompts.Any(p => p.Severity == PromptSeverity.MayNotContinue
                                 && p.Message.Contains("https://")),
                Is.True);
        }
    }
}
