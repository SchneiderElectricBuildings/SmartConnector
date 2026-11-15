using System;
using System.Collections.Generic;
using System.Linq;
using Mongoose.Ews.Server.Data;
using Mongoose.Ews.Server.Data.Shared;

namespace SmartConnector.WeatherExtension
{
    /// <summary>
    /// The security policy for the EWS Server this extension hosts.
    /// The server only publishes data, so EWS clients get read access,
    /// SHA-256 Digest authentication, and an encrypted address.
    /// The processor applies the policy on every cycle, so a server that
    /// drifts from it is brought back.
    /// </summary>
    public static class EwsServerSecurity
    {
        /// <summary>Name of the property that permits an http:// address.</summary>
        public const string DevelopmentOptOut = "AllowUnencryptedEwsForDevelopment";

        private const string CookiesSetting = "AllowCookies";
        private const string AlgorithmsSetting = "HashAlgorithms";
        private const string MethodsSetting = "SupportedMethods";

        /// <summary>
        /// The operations the server offers to EWS clients. Clients read
        /// values, history, and alarms, and subscribe to changes. They
        /// cannot write, force, or unforce a value. The processor is not
        /// affected, because it writes through the data adapter.
        /// </summary>
        public static EwsServerMethods ReadOnlyMethods()
        {
            return new EwsServerMethods
            {
                GetContainerItems = true,
                GetItems = true,
                GetValues = true,
                GetHistory = true,
                GetHistoricalDataAggregation = true,
                GetHierarchicalInformation = true,
                GetNotification = true,
                GetEnums = true,
                GetWebServiceInformation = true,
                // Advertise the alarm operations even though this extension
                // publishes no alarms. EWS clients such as EcoStruxure Building
                // Operation poll for alarms. With these enabled the server
                // returns an empty alarm list, instead of the OPERATION_NOT_SUPPORTED
                // fault EBO would otherwise log as "CWS Alarm Poll failed".
                GetAlarmEvents = true,
                GetAlarmEventTypes = true,
                GetAlarmHistory = true,
                GetUpdatedAlarmEvents = true,
                AcknowledgeAlarmEvents = true,
                Subscribe = true,
                Unsubscribe = true,
                Renew = true,
                SetValues = false,
                ForceValues = false,
                UnforceValues = false
            };
        }

        /// <summary>The Digest hash algorithms the server offers.</summary>
        public static EwsServerHashingAlgorithms RequiredAlgorithms()
        {
            return new EwsServerHashingAlgorithms { Sha256 = true };
        }

        /// <summary>
        /// Checks the EWS Server address. Returns null when the address is
        /// acceptable, otherwise the message to show.
        /// </summary>
        public static string ValidateAddress(string address, bool allowUnencrypted)
        {
            var scheme = SchemeOf(address);

            if (scheme == Uri.UriSchemeHttps)
                return null;

            if (scheme == Uri.UriSchemeHttp)
            {
                return allowUnencrypted
                    ? null
                    : "EwsAddress must start with https://. To use an http:// address "
                      + "during development, turn on " + DevelopmentOptOut + ".";
            }

            return "EwsAddress must be a complete address that starts with https://.";
        }

        /// <summary>
        /// Checks the address an existing EWS Server listens on. A server keeps
        /// the address it was created with, whatever EwsAddress says later, so
        /// the processor checks the server itself on every cycle. Returns null
        /// when the address is acceptable, otherwise the message to show.
        /// </summary>
        public static string ValidateServerAddress(string serverAddress, bool allowUnencrypted)
        {
            var stored = serverAddress ?? string.Empty;

            // The service reads the stored address as it is, so surrounding
            // spaces are a problem here and are not trimmed away.
            if (stored != stored.Trim())
            {
                return "The address of the EWS Server has leading or trailing spaces. "
                       + "Save it again without them on the EWS Servers tab of the Portal.";
            }

            var scheme = SchemeOf(stored);

            if (scheme == Uri.UriSchemeHttps || (scheme == Uri.UriSchemeHttp && allowUnencrypted))
                return null;

            var message = "The EWS Server does not listen on an https:// address. Change the "
                          + "address of the server on the EWS Servers tab of the Portal, then "
                          + "set EwsAddress to the same value.";

            return scheme == Uri.UriSchemeHttp
                ? message + " To keep an http:// address during development, turn on "
                  + DevelopmentOptOut + "."
                : message;
        }
        // Returns the lower-case scheme of a complete address with a host,
        // or null when the text is not one.
        private static string SchemeOf(string address)
        {
            Uri uri;
            var complete = Uri.TryCreate((address ?? string.Empty).Trim(), UriKind.Absolute, out uri)
                           && !string.IsNullOrEmpty(uri.Host);
            return complete ? uri.Scheme : null;
        }

        /// <summary>
        /// Names the settings of a server that differ from the policy.
        /// An empty list means the server already matches.
        /// </summary>
        public static IReadOnlyList<string> PendingChanges(
            bool allowCookies,
            EwsServerHashingAlgorithms algorithms,
            EwsServerMethods methods)
        {
            var pending = new List<string>();

            if (allowCookies)
                pending.Add(CookiesSetting);
            if (!SameFlags(algorithms, RequiredAlgorithms()))
                pending.Add(AlgorithmsSetting);
            if (!SameFlags(methods, ReadOnlyMethods()))
                pending.Add(MethodsSetting);

            return pending;
        }

        /// <summary>
        /// Brings the server in line with the policy and returns the names
        /// of the settings it changed. It writes only what differs, so a
        /// server that already matches is left untouched.
        /// </summary>
        public static IReadOnlyList<string> Apply(EwsServerDataAdapter adapter)
        {
            var server = adapter.Server;
            var pending = PendingChanges(
                server.AllowCookies,
                server.SupportedHashingAlgorithms,
                server.SupportedMethods);

            if (pending.Contains(CookiesSetting))
                adapter.ModifyServerAllowCookies(false);
            if (pending.Contains(AlgorithmsSetting))
                adapter.ModifyServerSupportedHashAlgorithms(RequiredAlgorithms());
            if (pending.Contains(MethodsSetting))
                adapter.ModifyServerSupportedMethods(ReadOnlyMethods());

            return pending;
        }

        // Two settings objects match when every on/off property matches.
        private static bool SameFlags<T>(T actual, T required) where T : class
        {
            if (actual == null)
                return false;

            return typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(bool) && p.CanRead)
                .All(p => (bool)p.GetValue(actual) == (bool)p.GetValue(required));
        }
    }
}
