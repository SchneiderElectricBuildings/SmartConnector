using Mongoose.Test;
using Mongoose.Test.Processors;
using NUnit.Framework;

namespace Quickstart.HelloEws.Tests
{
    [TestFixture]
    public class ProcessorFixture : SmartConnectorTestFixtureBase,
        IProcessorTestFixture<HelloEwsProcessor>
    {
        protected override void FixtureOneTimeSetup_Base()
        {
        }

        public HelloEwsProcessor CreateTestableProcessor()
        {
            return new HelloEwsProcessor
            {
                ServerName = "Test Hello EWS Server",
                Realm = "TestRealm",
                EwsAddress = "http://localhost:5300/TestHelloEws",
                UserName = "YOUR_USERNAME",
                Password = "YOUR_PASSWORD"
            };
        }

        // ── Validation Tests ───────────────────────────────────────────
        // ValidateProcessor evaluates the data-annotation attributes on the
        // processor; it does not require the Smart Connector runtime.

        [Test]
        public void ValidateTest()
        {
            var processor = CreateTestableProcessor();
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Empty,
                "Default processor should validate cleanly");
        }

        [Test]
        public void ValidateTest_MissingUserName_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.UserName = null;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
        }

        [Test]
        public void ValidateTest_MissingPassword_Fails()
        {
            var processor = CreateTestableProcessor();
            processor.Password = null;
            var results = this.ValidateProcessor(processor);
            Assert.That(results, Is.Not.Empty);
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
    }
}
