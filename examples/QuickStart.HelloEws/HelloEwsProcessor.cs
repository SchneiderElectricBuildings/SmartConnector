// ---------------------------------------------------------------------------
// Quick Start Sample — HelloEwsProcessor
//
// Creates its own EWS Server on the first run (no manual Portal setup),
// provisions a single value with the idempotent Ensure* pattern, and writes
// the current UTC time to it on every execution.
// See: Developer Guide Section 3 — Quick Start
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
        [DefaultValue("http://localhost:5300/HelloEws")]
        [Tooltip("HTTP endpoint address for the EWS Server.")]
        public string EwsAddress { get; set; }

        [Required]
        [Tooltip("Username for the EWS Server.")]
        public string UserName { get; set; }

        [Required]
        [EncryptedString]
        [Tooltip("Password for the EWS Server. Stored encrypted in the database.")]
        public string Password { get; set; }

        private EwsServerDataAdapter _adapter;

        protected override IEnumerable<Prompt> Execute_Subclass()
        {
            var prompts = new List<Prompt>();

            var adapter = ConnectOrCreateServer();

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
        // does not yet exist) do we ConnectNew to create it — ConnectNew throws
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

            // First run — create the server, then reconnect with a clean context.
            using (EwsServerDataAdapter.ConnectNew(
                name: ServerName, address: EwsAddress, realm: Realm,
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
            return existing ?? adapter.AddValueItem(
                altId, name, name, type,
                EwsValueWriteableEnum.Writeable, EwsValueForceableEnum.NotForceable,
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
