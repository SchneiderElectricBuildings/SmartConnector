using System;
using System.IO;
using System.Net;
using System.Text;

namespace SmartConnector.EntsoeExtension
{
    public sealed class EntsoeApiClient : IDisposable
    {
        private const string BaseUrl = "https://web-api.tp.entsoe.eu/api";
        private const int RequestTimeoutMs = 30000;

        private readonly string _securityToken;

        public EntsoeApiClient(string securityToken)
        {
            if (string.IsNullOrWhiteSpace(securityToken))
                throw new ArgumentException("Security token must not be empty.", nameof(securityToken));

            _securityToken = securityToken;

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public string QueryDayAheadPrices(string eicCode, DateTime periodStart, DateTime periodEnd)
        {
            var url = BuildUrl(
                "documentType", "A44",
                "in_Domain", eicCode,
                "out_Domain", eicCode,
                "periodStart", FormatTimestamp(periodStart),
                "periodEnd", FormatTimestamp(periodEnd));
            return ExecuteRequest(url);
        }

        public string QueryActualLoad(string eicCode, DateTime periodStart, DateTime periodEnd)
        {
            var url = BuildUrl(
                "documentType", "A65",
                "processType", "A16",
                "outBiddingZone_Domain", eicCode,
                "periodStart", FormatTimestamp(periodStart),
                "periodEnd", FormatTimestamp(periodEnd));
            return ExecuteRequest(url);
        }

        public string QueryLoadForecast(string eicCode, DateTime periodStart, DateTime periodEnd)
        {
            var url = BuildUrl(
                "documentType", "A65",
                "processType", "A01",
                "outBiddingZone_Domain", eicCode,
                "periodStart", FormatTimestamp(periodStart),
                "periodEnd", FormatTimestamp(periodEnd));
            return ExecuteRequest(url);
        }

        public string QueryGenerationForecast(string eicCode, DateTime periodStart, DateTime periodEnd)
        {
            var url = BuildUrl(
                "documentType", "A71",
                "processType", "A01",
                "in_Domain", eicCode,
                "periodStart", FormatTimestamp(periodStart),
                "periodEnd", FormatTimestamp(periodEnd));
            return ExecuteRequest(url);
        }

        public string QueryWindSolarForecast(string eicCode, DateTime periodStart, DateTime periodEnd)
        {
            var url = BuildUrl(
                "documentType", "A69",
                "processType", "A01",
                "in_Domain", eicCode,
                "periodStart", FormatTimestamp(periodStart),
                "periodEnd", FormatTimestamp(periodEnd));
            return ExecuteRequest(url);
        }

        public static string FormatTimestamp(DateTime dt)
        {
            return dt.ToString("yyyyMMddHHmm");
        }

        private string BuildUrl(params string[] keyValuePairs)
        {
            var sb = new StringBuilder(BaseUrl);
            sb.Append("?securityToken=").Append(Uri.EscapeDataString(_securityToken));

            for (int i = 0; i < keyValuePairs.Length; i += 2)
            {
                sb.Append('&')
                  .Append(Uri.EscapeDataString(keyValuePairs[i]))
                  .Append('=')
                  .Append(Uri.EscapeDataString(keyValuePairs[i + 1]));
            }

            return sb.ToString();
        }

        private static string ExecuteRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = RequestTimeoutMs;
            request.Accept = "application/xml";

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException(
                        $"ENTSO-E API returned HTTP {(int)response.StatusCode}: {response.StatusDescription}");
                }

                using (var stream = response.GetResponseStream())
                {
                    if (stream == null)
                        throw new InvalidOperationException("ENTSO-E API returned no response body.");

                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
