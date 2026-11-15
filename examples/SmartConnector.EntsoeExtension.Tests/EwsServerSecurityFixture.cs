using Mongoose.Ews.Server.Data;
using NUnit.Framework;

namespace SmartConnector.EntsoeExtension.Tests
{
    [TestFixture]
    public class EwsServerSecurityFixture
    {
        // ── Method set ─────────────────────────────────────────────────

        [Test]
        public void ReadOnlyMethods_AdvertisesNoWriteOperation()
        {
            var methods = EwsServerSecurity.ReadOnlyMethods();

            Assert.That(methods.SetValues, Is.False);
            Assert.That(methods.ForceValues, Is.False);
            Assert.That(methods.UnforceValues, Is.False);
        }

        [Test]
        public void ReadOnlyMethods_KeepsTheReadOperations()
        {
            var methods = EwsServerSecurity.ReadOnlyMethods();

            Assert.That(methods.GetValues, Is.True);
            Assert.That(methods.GetHistory, Is.True);
            Assert.That(methods.Subscribe, Is.True);
            Assert.That(methods.GetAlarmEvents, Is.True);
            Assert.That(methods.GetContainerItems, Is.True);
            Assert.That(methods.GetItems, Is.True);
            Assert.That(methods.GetHierarchicalInformation, Is.True);
            Assert.That(methods.GetNotification, Is.True);
            Assert.That(methods.GetEnums, Is.True);
            Assert.That(methods.GetWebServiceInformation, Is.True);
            Assert.That(methods.Unsubscribe, Is.True);
            Assert.That(methods.Renew, Is.True);
            Assert.That(methods.GetAlarmEventTypes, Is.True);
            Assert.That(methods.GetAlarmHistory, Is.True);
            Assert.That(methods.GetUpdatedAlarmEvents, Is.True);
        }

        // ── Digest algorithms ──────────────────────────────────────────

        [Test]
        public void RequiredAlgorithms_IsSha256Only()
        {
            var algorithms = EwsServerSecurity.RequiredAlgorithms();

            Assert.That(algorithms.Sha256, Is.True);
            Assert.That(algorithms.Md5, Is.False);
            Assert.That(algorithms.Md5Session, Is.False);
            Assert.That(algorithms.Sha256Session, Is.False);
            Assert.That(algorithms.Sha512, Is.False);
            Assert.That(algorithms.Sha512Session, Is.False);
        }

        // ── Address rule ───────────────────────────────────────────────

        [TestCase("https://host:5300/Entsoe")]
        [TestCase("HTTPS://host:5300/Entsoe")]
        [TestCase("  https://host:5300/Entsoe  ")]
        public void ValidateAddress_Https_IsAccepted(string address)
        {
            Assert.That(EwsServerSecurity.ValidateAddress(address, false), Is.Null);
        }

        [Test]
        public void ValidateAddress_Http_IsRejectedAndNamesTheProperty()
        {
            var message = EwsServerSecurity.ValidateAddress("http://host:5300/Entsoe", false);

            Assert.That(message, Does.Contain("AllowUnencryptedEwsForDevelopment"));
        }

        [Test]
        public void ValidateAddress_Http_IsAcceptedWithTheDevelopmentOptOut()
        {
            Assert.That(
                EwsServerSecurity.ValidateAddress("http://host:5300/Entsoe", true), Is.Null);
        }

        [TestCase("ftp://host/Weather")]
        [TestCase("host:5300/Entsoe")]
        [TestCase("")]
        [TestCase(null)]
        public void ValidateAddress_OtherSchemes_AreRejectedEvenWithTheOptOut(string address)
        {
            Assert.That(EwsServerSecurity.ValidateAddress(address, true), Is.Not.Null);
        }

        [Test]
        public void ValidateAddress_WithoutAHost_IsRejected()
        {
            Assert.That(EwsServerSecurity.ValidateAddress("https://", true), Is.Not.Null);
        }

        // ── Address of an existing server ──────────────────────────────

        [TestCase("https://host:5300/Server")]
        [TestCase("HTTPS://HOST:5300/Server")]
        public void ValidateServerAddress_Https_IsAccepted(string address)
        {
            Assert.That(EwsServerSecurity.ValidateServerAddress(address, false), Is.Null);
        }

        [TestCase(" https://host:5300/Server")]
        [TestCase("https://host:5300/Server ")]
        public void ValidateServerAddress_SurroundingSpaces_AreRejected(string address)
        {
            Assert.That(
                EwsServerSecurity.ValidateServerAddress(address, false),
                Does.Contain("leading or trailing spaces"));
        }

        [Test]
        public void ValidateServerAddress_OtherSchemeWithTheOptOut_DoesNotSuggestTheOptOut()
        {
            var message = EwsServerSecurity.ValidateServerAddress("ftp://host/Server", true);

            Assert.That(message, Does.Not.Contain("AllowUnencryptedEwsForDevelopment"));
        }

        [Test]
        public void ValidateServerAddress_Http_IsRejectedAndNamesTheProperty()
        {
            var message = EwsServerSecurity.ValidateServerAddress("http://host:5300/Server", false);

            Assert.That(message, Does.Contain("AllowUnencryptedEwsForDevelopment"));
        }

        [Test]
        public void ValidateServerAddress_Http_IsAcceptedWithTheDevelopmentOptOut()
        {
            Assert.That(
                EwsServerSecurity.ValidateServerAddress("http://host:5300/Server", true), Is.Null);
        }

        [TestCase("ftp://host/Server")]
        [TestCase("")]
        [TestCase(null)]
        public void ValidateServerAddress_AnythingElse_IsRejectedEvenWithTheOptOut(string address)
        {
            Assert.That(EwsServerSecurity.ValidateServerAddress(address, true), Is.Not.Null);
        }

        // ── Drift detection ────────────────────────────────────────────

        [Test]
        public void PendingChanges_ServerThatMatchesThePolicy_HasNone()
        {
            var pending = EwsServerSecurity.PendingChanges(
                false,
                EwsServerSecurity.RequiredAlgorithms(),
                EwsServerSecurity.ReadOnlyMethods());

            Assert.That(pending, Is.Empty);
        }

        [Test]
        public void PendingChanges_ServerOffPolicy_NamesEverySetting()
        {
            var algorithms = new EwsServerHashingAlgorithms { Md5 = true, Sha256 = true };
            var methods = EwsServerSecurity.ReadOnlyMethods();
            methods.SetValues = true;

            var pending = EwsServerSecurity.PendingChanges(true, algorithms, methods);

            Assert.That(pending, Is.EquivalentTo(
                new[] { "AllowCookies", "HashAlgorithms", "SupportedMethods" }));
        }

        [Test]
        public void PendingChanges_OnlyCookiesOn_NamesOnlyCookies()
        {
            var pending = EwsServerSecurity.PendingChanges(
                true,
                EwsServerSecurity.RequiredAlgorithms(),
                EwsServerSecurity.ReadOnlyMethods());

            Assert.That(pending, Is.EquivalentTo(new[] { "AllowCookies" }));
        }

        [Test]
        public void PendingChanges_MissingSettings_AreTreatedAsDifferent()
        {
            var pending = EwsServerSecurity.PendingChanges(false, null, null);

            Assert.That(pending, Is.EquivalentTo(
                new[] { "HashAlgorithms", "SupportedMethods" }));
        }

        [Test]
        public void PendingChanges_ReadOperationTurnedOff_NamesTheMethods()
        {
            var methods = EwsServerSecurity.ReadOnlyMethods();
            methods.GetValues = false;

            var pending = EwsServerSecurity.PendingChanges(
                false, EwsServerSecurity.RequiredAlgorithms(), methods);

            Assert.That(pending, Is.EquivalentTo(new[] { "SupportedMethods" }));
        }

        [Test]
        public void PendingChanges_ExtraOperationTurnedOn_NamesTheMethods()
        {
            var methods = EwsServerSecurity.ReadOnlyMethods();
            methods.GetSystemEvents = true;

            var pending = EwsServerSecurity.PendingChanges(
                false, EwsServerSecurity.RequiredAlgorithms(), methods);

            Assert.That(pending, Is.EquivalentTo(new[] { "SupportedMethods" }));
        }

        [Test]
        public void PendingChanges_ExtraAlgorithmOffered_NamesTheAlgorithms()
        {
            var algorithms = EwsServerSecurity.RequiredAlgorithms();
            algorithms.Sha512 = true;

            var pending = EwsServerSecurity.PendingChanges(
                false, algorithms, EwsServerSecurity.ReadOnlyMethods());

            Assert.That(pending, Is.EquivalentTo(new[] { "HashAlgorithms" }));
        }
    }
}