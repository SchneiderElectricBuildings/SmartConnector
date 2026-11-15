using Mongoose.Common;
using Mongoose.Ews.Server.Data;
using Ews.Common;
using System.Linq;
using Mongoose.Test;
using Mongoose.Test.Processors;
using NUnit.Framework;

namespace SmartConnector.EntsoeExtension.Tests
{
    [TestFixture]
    public class ProcessorFixture : SmartConnectorTestFixtureBase,
        IProcessorTestFixture<EntsoeProcessor>
    {
        protected override void FixtureOneTimeSetup_Base()
        {
            // The Execute tests need the Smart Connector container. It is started
            // by name, so this project builds without a reference to the service.
            System.Type.GetType("Mongoose.Service.Mongoose, Mongoose.Service")
                ?.GetMethod("InitIoC")?.Invoke(null, null);
        }

        public EntsoeProcessor CreateTestableProcessor()
        {
            return new EntsoeProcessor
            {
                ServerName = "Test ENTSOE Energy Data",
                Realm = "TestRealm",
                EwsAddress = "https://localhost:5300/TestEntsoeEnergyData",
                UserName = "YOUR_USERNAME",
                Password = "YOUR_PASSWORD",
                Country = SupportedCountry.Netherlands,
                ApiToken = "YOUR_ENTSOE_TOKEN",
                CheapPercentile = 25,
                ExpensivePercentile = 75,
                BackfillDays = 30,
                UpdatePrices = true,
                UpdateLoad = true,
                UpdateGeneration = true
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
        public void ValidateTest_HttpAddress_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.EwsAddress = "http://localhost:5300/TestEntsoeEnergyData";
            var results = this.ValidateProcessor(processor);
            Assert.That(
                results.Any(r => r.Message.Contains("AllowUnencryptedEwsForDevelopment")),
                Is.True);
        }

        [Test]
        public void ValidateTest_HttpAddressWithDevelopmentOptOut_Succeeds()
        {
            var processor = CreateTestableProcessor();
            processor.EwsAddress = "http://localhost:5300/TestEntsoeEnergyData";
            processor.AllowUnencryptedEwsForDevelopment = true;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ValidateTest_MissingApiToken_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.ApiToken = null;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
        }

        [Test]
        public void ValidateTest_CheapPercentileAboveExpensive_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.CheapPercentile = 80;
            processor.ExpensivePercentile = 60;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
        }

        [Test]
        public void ValidateTest_Sweden_Succeeds()
        {
            var processor = CreateTestableProcessor();
            processor.Country = SupportedCountry.Sweden;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Empty,
                "Sweden (multi-zone) should validate cleanly");
        }

        [Test]
        public void ValidateTest_Norway_Succeeds()
        {
            var processor = CreateTestableProcessor();
            processor.Country = SupportedCountry.Norway;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Empty,
                "Norway (5-zone) should validate cleanly");
        }

        // ── Area Mapping Tests ─────────────────────────────────────────

        [Test]
        public void GetZoneCodes_Sweden_Returns4Zones()
        {
            var zones = AreaMapping.GetZoneCodes(SupportedCountry.Sweden);
            Assert.That(zones.Length, Is.EqualTo(4));
            Assert.That(zones, Does.Contain("SE_1"));
            Assert.That(zones, Does.Contain("SE_4"));
        }

        [Test]
        public void GetZoneCodes_Norway_Returns5Zones()
        {
            var zones = AreaMapping.GetZoneCodes(SupportedCountry.Norway);
            Assert.That(zones.Length, Is.EqualTo(5));
        }

        [Test]
        public void GetZoneCodes_Netherlands_Returns1Zone()
        {
            var zones = AreaMapping.GetZoneCodes(SupportedCountry.Netherlands);
            Assert.That(zones.Length, Is.EqualTo(1));
            Assert.That(zones[0], Is.EqualTo("NL"));
        }

        [Test]
        public void GetZoneCodes_Denmark_Returns2Zones()
        {
            var zones = AreaMapping.GetZoneCodes(SupportedCountry.Denmark);
            Assert.That(zones.Length, Is.EqualTo(2));
            Assert.That(zones, Does.Contain("DK_1"));
            Assert.That(zones, Does.Contain("DK_2"));
        }

        [Test]
        public void GetCountryDisplayName_ReturnsReadableName()
        {
            Assert.That(AreaMapping.GetCountryDisplayName(SupportedCountry.CzechRepublic),
                Is.EqualTo("Czech Republic"));
            Assert.That(AreaMapping.GetCountryDisplayName(SupportedCountry.GreatBritain),
                Is.EqualTo("Great Britain"));
        }

        [Test]
        public void AllSupportedCountries_HaveAtLeastOneZone()
        {
            foreach (SupportedCountry country in
                System.Enum.GetValues(typeof(SupportedCountry)))
            {
                var zones = AreaMapping.GetZoneCodes(country);
                Assert.That(zones.Length, Is.GreaterThan(0),
                    $"{country} has no zone codes mapped");
            }
        }

        [Test]
        public void AllZoneCodes_ResolveToAreaInfo()
        {
            foreach (SupportedCountry country in
                System.Enum.GetValues(typeof(SupportedCountry)))
            {
                foreach (var zoneCode in AreaMapping.GetZoneCodes(country))
                {
                    var area = AreaMapping.Lookup(zoneCode);
                    Assert.That(area, Is.Not.Null,
                        $"Zone code '{zoneCode}' for {country} did not resolve");
                }
            }
        }

        // ── Integration Tests ──────────────────────────────────────────
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
                processor.ServerName, processor.UserName, processor.Password))
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
