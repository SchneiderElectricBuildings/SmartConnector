// ---------------------------------------------------------------------------
// Quick Start Sample, HelloEwsProcessor
//
// Creates its own EWS Server on the first run (no manual Portal setup),
// provisions a single value with the idempotent Ensure* pattern, and writes
// the current UTC time to it on every execution.
// See Developer Guide Section 3, Quick Start
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Ews.Common;
using Mongoose.Common;
using Mongoose.Common.Attributes;
using Mongoose.Ews.Server.Data;
using Mongoose.Process;
using SxL.Common;

namespace Quickstart.HelloEws
{
    [ConfigurationDefaults("Hello EWS Processor",
        "Creates an EWS Server (if needed) and writes the current UTC time to "
        + "/Hello/LastSeen on every execution.")]
    public class HelloEwsProcessor : Processor
    {
        // Disable licensing for the quick start; see Developer Guide Section 10 to enable.
        public override bool IsLicensed => false;

        [Required]
        [DefaultValue("Hello EWS Server")]
        [Tooltip("Name of the EWS Server to create or connect to.")]
        public string ServerName { get; set; }

        [DefaultValue("SmartConnectorRealm")]
        [Tooltip("HTTP Digest authentication realm for the EWS Server.")]
        public string Realm { get; set; }

        [Required]
        [DefaultValue("https://localhost:5300/HelloEws")]
        [Tooltip("HTTPS endpoint address for the EWS Server. Bind a certificate "
            + "to the port before the first run.")]
        public string EwsAddress { get; set; }

        [DefaultValue(false)]
        [Tooltip("Permits an http:// EWS address. For development only.")]
        public bool AllowUnencryptedEwsForDevelopment { get; set; }

        [Required]
        [Tooltip("Username for the EWS Server.")]
        public string UserName { get; set; }

        [Required]
        [EncryptedString]
        [Tooltip("Password for the EWS Server. Stored encrypted in the database.")]
        public string Password { get; set; }

        private EwsServerDataAdapter _adapter;

        // The EWS address must be encrypted, unless the development property is on.
        public override IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            foreach (var result in base.Validate(validationContext))
                yield return result;

            var addressProblem = EwsServerSecurity.ValidateAddress(
                EwsAddress, AllowUnencryptedEwsForDevelopment);
            if (addressProblem != null)
                yield return new ValidationResult(addressProblem);
        }

        protected override IEnumerable<Prompt> Execute_Subclass()
        {
            var prompts = new List<Prompt>();

            var adapter = ConnectOrCreateServer();

            // Apply the security policy on every run, so a server that has
            // drifted from it is brought back before any data is written.
            var applied = EwsServerSecurity.Apply(adapter);
            if (applied.Count > 0)
            {
                Logger.LogInfo(LogCategory.Processor,
                    "EWS Server security settings applied ("
                    + string.Join(", ", applied) + ").");
            }

            if (AllowUnencryptedEwsForDevelopment)
            {
                Logger.LogInfo(LogCategory.Processor,
                    "AllowUnencryptedEwsForDevelopment is on. Turn it off outside development.");
            }

            // A server keeps the address it was created with, so check the
            // server itself, not only the configured EwsAddress.
            var serverAddressProblem = EwsServerSecurity.ValidateServerAddress(
                adapter.Server.Address, AllowUnencryptedEwsForDevelopment);
            if (serverAddressProblem != null)
            {
                prompts.Add(new Prompt
                {
                    Message = serverAddressProblem,
                    Severity = PromptSeverity.MayNotContinue
                });
                return prompts;
            }

            // Provision the structure. Ensure* helpers are idempotent: they
            // create the folder/value on the first run and return the existing
            // items on every run after that, so no manual Portal setup is needed.
            var hello = EnsureFolder(adapter, "/Hello", "Hello", null);
            EnsureValue(adapter, "/Hello/LastSeen", "LastSeen",
                EwsValueTypeEnum.String, hello);

            adapter.ModifyValueItemValue(
                "/Hello/LastSeen",
                DateTime.UtcNow.ToString("o"),
                EwsValueStateEnum.Good);

            Logger.LogInfo(LogCategory.Processor, "Wrote timestamp to /Hello/LastSeen");
            return prompts;
        }

        // Returns an adapter for the EWS Server, creating the server on the
        // first run. ConnectExisting is tried first: on every run after the
        // first it succeeds and we are done. Only when it fails (the server
        // does not yet exist) do we ConnectNew to create it. ConnectNew throws
        // up front if the name is already in use, so it must never run for an
        // existing server.
        private EwsServerDataAdapter ConnectOrCreateServer()
        {
            try
            {
                _adapter = EwsServerDataAdapter.ConnectExisting(ServerName, UserName, Password);
                return _adapter;
            }
            catch (Exception ex)
            {
                Logger.LogDebug(LogCategory.Processor,
                    $"ConnectExisting failed ({ex.GetType().Name}); creating server "
                    + $"'{ServerName}'.");
            }

            // First run. Create the server, then reconnect with a clean context.
            using (EwsServerDataAdapter.ConnectNew(
                name: ServerName, address: EwsAddress.Trim(), realm: Realm,
                username: UserName, password: Password, autoStart: true))
            {
            }

            _adapter = EwsServerDataAdapter.ConnectExisting(ServerName, UserName, Password);
            return _adapter;
        }

        // Returns the existing container or creates it. Idempotent.
        private static EwsContainerItem EnsureFolder(
            EwsServerDataAdapter adapter, string altId, string name, EwsContainerItem parent)
        {
            var existing = adapter.ContainerItems.FirstOrDefault(c => c.AlternateId == altId);
            return existing ?? adapter.AddContainerItem(
                altId, name, name, EwsContainerTypeEnum.Folder, parent);
        }

        // Returns the existing value item or creates it. Idempotent.
        private static EwsValueItem EnsureValue(
            EwsServerDataAdapter adapter, string altId, string name,
            EwsValueTypeEnum type, EwsContainerItem parent)
        {
            var existing = adapter.ValueItems.FirstOrDefault(v => v.AlternateId == altId);
            if (existing != null)
            {
                // A value item created by an earlier version may still be writeable.
                if (existing.Writeable != EwsValueWriteableEnum.ReadOnly)
                {
                    adapter.ModifyValueItemWriteable(
                        existing, EwsValueWriteableEnum.ReadOnly);
                }
                return existing;
            }

            return adapter.AddValueItem(
                altId, name, name, type,
                EwsValueWriteableEnum.ReadOnly, EwsValueForceableEnum.NotForceable,
                EwsValueStateEnum.Uncertain, null, parent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _adapter?.Dispose();
                _adapter = null;
            }
            base.Dispose(disposing);
        }
    }
}
