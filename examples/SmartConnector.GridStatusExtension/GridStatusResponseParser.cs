// ---------------------------------------------------------------------------
// GridStatus.io JSON Response Parser
//
// Parses the array-of-objects JSON response format from the GridStatus.io
// API into typed data rows. Uses System.Web.Script.Serialization (available
// in .NET Framework 4.8) to avoid external JSON dependencies.
//
// Response format reference:
// https://docs.gridstatus.io/developers/api-reference/query-data
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace SmartConnector.GridStatusExtension
{
    /// <summary>
    /// A single row returned from a GridStatus.io dataset query.
    /// </summary>
    public sealed class DataRow
    {
        public DateTime IntervalStartUtc { get; set; }
        public DateTime IntervalEndUtc { get; set; }
        public Dictionary<string, object> Columns { get; set; }

        public double GetDouble(string columnName, double defaultValue = 0.0)
        {
            if (Columns == null || !Columns.TryGetValue(columnName, out var val))
                return defaultValue;

            if (val == null)
                return defaultValue;

            if (val is double d) return d;
            if (val is int i) return i;
            if (val is long l) return l;
            if (val is decimal m) return (double)m;

            if (double.TryParse(
                val.ToString(),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out double parsed))
                return parsed;

            return defaultValue;
        }

        public string GetString(string columnName, string defaultValue = "")
        {
            if (Columns == null || !Columns.TryGetValue(columnName, out var val))
                return defaultValue;

            return val?.ToString() ?? defaultValue;
        }
    }

    /// <summary>
    /// Parsed response from a GridStatus.io dataset query.
    /// </summary>
    public sealed class DatasetResponse
    {
        public List<DataRow> Rows { get; set; } = new List<DataRow>();
        public int StatusCode { get; set; }
        public bool HasNextPage { get; set; }
        public int RowCount => Rows.Count;
    }

    /// <summary>
    /// Parses GridStatus.io JSON responses into typed objects.
    /// </summary>
    public static class GridStatusResponseParser
    {
        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer { MaxJsonLength = 10 * 1024 * 1024 };

        /// <summary>
        /// Parses a JSON response from the query endpoint.
        /// </summary>
        public static DatasetResponse Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new DatasetResponse();

            var raw = Serializer.Deserialize<Dictionary<string, object>>(json);
            var response = new DatasetResponse();

            if (raw.TryGetValue("status_code", out var sc))
                response.StatusCode = Convert.ToInt32(sc);

            if (raw.TryGetValue("meta", out var metaObj)
                && metaObj is Dictionary<string, object> meta
                && meta.TryGetValue("hasNextPage", out var hnp))
            {
                response.HasNextPage = Convert.ToBoolean(hnp);
            }

            if (!raw.TryGetValue("data", out var dataObj)
                || !(dataObj is System.Collections.IList dataList))
                return response;

            foreach (var item in dataList)
            {
                if (!(item is Dictionary<string, object> rowDict))
                    continue;

                var row = new DataRow
                {
                    Columns = new Dictionary<string, object>(
                        rowDict, StringComparer.OrdinalIgnoreCase)
                };

                // Parse interval timestamps (UTC preferred, fall back to local)
                row.IntervalStartUtc = ParseTimestamp(rowDict,
                    "interval_start_utc", "interval_start_local");
                row.IntervalEndUtc = ParseTimestamp(rowDict,
                    "interval_end_utc", "interval_end_local");

                response.Rows.Add(row);
            }

            return response;
        }

        /// <summary>
        /// Parses the API usage response into a typed object.
        /// </summary>
        public static ApiUsageInfo ParseApiUsage(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new ApiUsageInfo();

            var raw = Serializer.Deserialize<Dictionary<string, object>>(json);
            var info = new ApiUsageInfo();

            if (raw.TryGetValue("plan_name", out var pn))
                info.PlanName = pn?.ToString() ?? "";

            if (raw.TryGetValue("limits", out var limObj)
                && limObj is Dictionary<string, object> limits)
            {
                if (limits.TryGetValue("api_rows_returned_limit", out var rl))
                    info.RowLimit = Convert.ToInt64(rl);
                if (limits.TryGetValue("api_requests_limit", out var rql))
                    info.RequestLimit = Convert.ToInt64(rql);
                if (limits.TryGetValue("api_rows_per_response_limit", out var rpr))
                    info.RowsPerResponseLimit = Convert.ToInt32(rpr);
                if (limits.TryGetValue("per_second_api_rate_limit", out var psr))
                    info.PerSecondRateLimit = Convert.ToInt32(psr);
                if (limits.TryGetValue("per_minute_api_rate_limit", out var pmr))
                    info.PerMinuteRateLimit = Convert.ToInt32(pmr);
            }

            if (raw.TryGetValue("current_period_usage", out var usageObj)
                && usageObj is Dictionary<string, object> usage)
            {
                if (usage.TryGetValue("total_requests", out var tr))
                    info.TotalRequests = Convert.ToInt64(tr);
                if (usage.TryGetValue("total_api_rows_returned", out var trr))
                    info.TotalRowsReturned = Convert.ToInt64(trr);
            }

            return info;
        }

        private static DateTime ParseTimestamp(
            Dictionary<string, object> row,
            string utcKey,
            string localKey)
        {
            string raw = null;

            if (row.TryGetValue(utcKey, out var utcVal) && utcVal != null)
                raw = utcVal.ToString();
            else if (row.TryGetValue(localKey, out var localVal) && localVal != null)
                raw = localVal.ToString();

            if (string.IsNullOrEmpty(raw))
                return DateTime.MinValue;

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var dt))
                return dt;

            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// API usage and plan limit information.
    /// </summary>
    public sealed class ApiUsageInfo
    {
        public string PlanName { get; set; } = "";
        public long RowLimit { get; set; }
        public long RequestLimit { get; set; }
        public int RowsPerResponseLimit { get; set; }
        public int PerSecondRateLimit { get; set; }
        public int PerMinuteRateLimit { get; set; }
        public long TotalRequests { get; set; }
        public long TotalRowsReturned { get; set; }

        /// <summary>
        /// Returns the percentage of the row budget consumed so far.
        /// </summary>
        public double RowBudgetUsedPercent =>
            RowLimit > 0
                ? (double)TotalRowsReturned / RowLimit * 100.0
                : 0.0;
    }
}
