// ---------------------------------------------------------------------------
// GridStatus.io API Client
//
// REST client for the GridStatus.io v1 API. Uses HttpClient with API key
// authentication via the x-api-key header. Handles rate limiting (HTTP 429)
// with exponential backoff. Requests only the columns needed to minimize
// row consumption on free-tier plans.
//
// Source: https://docs.gridstatus.io/developers/api-reference/query-data
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SmartConnector.GridStatusExtension
{
    /// <summary>
    /// Lightweight REST client for the GridStatus.io v1 API.
    /// </summary>
    public sealed class GridStatusClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private const int MaxRetries = 3;
        private const int InitialBackoffMs = 2000;

        public GridStatusClient(string apiKey, string baseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _baseUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? "https://api.gridstatus.io/v1"
                : baseUrl.TrimEnd('/');

            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Queries a dataset with the given parameters. Returns the raw
        /// JSON response body. Retries on HTTP 429 with exponential backoff.
        /// Does not retry on 401/403 (configuration error).
        /// </summary>
        public string QueryDataset(
            string datasetId,
            DateTime? startTimeUtc = null,
            DateTime? endTimeUtc = null,
            string filterColumn = null,
            string filterValue = null,
            string columns = null,
            int limit = 100,
            string time = null,
            string timezone = "market",
            string resampleFrequency = null)
        {
            var parameters = new List<string>
            {
                "return_format=json",
                "json_schema=array-of-objects",
                $"limit={limit}"
            };

            if (!string.IsNullOrEmpty(timezone))
                parameters.Add($"timezone={Uri.EscapeDataString(timezone)}");

            if (startTimeUtc.HasValue)
                parameters.Add($"start_time={startTimeUtc.Value.ToString("o", CultureInfo.InvariantCulture)}");

            if (endTimeUtc.HasValue)
                parameters.Add($"end_time={endTimeUtc.Value.ToString("o", CultureInfo.InvariantCulture)}");

            if (!string.IsNullOrEmpty(time))
                parameters.Add($"time={Uri.EscapeDataString(time)}");

            if (!string.IsNullOrEmpty(filterColumn) && !string.IsNullOrEmpty(filterValue))
            {
                parameters.Add($"filter_column={Uri.EscapeDataString(filterColumn)}");
                parameters.Add($"filter_value={Uri.EscapeDataString(filterValue)}");
                parameters.Add("filter_operator==");
            }

            if (!string.IsNullOrEmpty(columns))
                parameters.Add($"columns={Uri.EscapeDataString(columns)}");

            if (!string.IsNullOrEmpty(resampleFrequency))
                parameters.Add($"resample_frequency={Uri.EscapeDataString(resampleFrequency)}");

            string url = $"{_baseUrl}/datasets/{Uri.EscapeDataString(datasetId)}/query?{string.Join("&", parameters)}";

            return ExecuteWithRetry(url);
        }

        /// <summary>
        /// Fetches current API usage and plan limits. Returns the raw JSON.
        /// Source: https://docs.gridstatus.io/developers/api-reference/api-usage
        /// </summary>
        public string GetApiUsage()
        {
            string url = $"{_baseUrl}/api_usage";
            return ExecuteWithRetry(url);
        }

        private string ExecuteWithRetry(string url)
        {
            int backoffMs = InitialBackoffMs;

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                // Use synchronous wrapper since Mongoose processors are synchronous
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response;

                try
                {
                    response = _http.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (TaskCanceledException)
                {
                    throw new GridStatusApiException(
                        "Request timed out", HttpStatusCode.RequestTimeout);
                }

                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync()
                        .GetAwaiter().GetResult();
                }

                int statusCode = (int)response.StatusCode;

                // Do not retry auth/quota errors. GridStatus.io returns 403 both
                // for an invalid key and for an exhausted plan limit, so inspect
                // the body to report the right cause.
                if (statusCode == 401 || statusCode == 403)
                {
                    string errBody = response.Content.ReadAsStringAsync()
                        .GetAwaiter().GetResult();
                    bool limitReached = statusCode == 403
                        && errBody.IndexOf("limit", StringComparison.OrdinalIgnoreCase) >= 0;
                    string message = limitReached
                        ? $"GridStatus.io plan limit reached (HTTP 403): {errBody} "
                          + "Reduce the schedule frequency or upgrade the plan."
                        : $"Authentication failed (HTTP {statusCode}). "
                          + "Verify the API key in configuration.";
                    throw new GridStatusApiException(message, response.StatusCode);
                }

                // Retry on 429 (rate limited) with exponential backoff
                if (statusCode == 429 && attempt < MaxRetries)
                {
                    Thread.Sleep(backoffMs);
                    backoffMs *= 2;
                    continue;
                }

                // Other errors: throw without retry
                string body = response.Content.ReadAsStringAsync()
                    .GetAwaiter().GetResult();
                throw new GridStatusApiException(
                    $"HTTP {statusCode}: {body}", response.StatusCode);
            }

            throw new GridStatusApiException(
                "Max retries exceeded for rate-limited request.",
                (HttpStatusCode)429);
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }

    /// <summary>
    /// Exception specific to GridStatus.io API errors, carrying the HTTP
    /// status code for upstream classification.
    /// </summary>
    public sealed class GridStatusApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public GridStatusApiException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
