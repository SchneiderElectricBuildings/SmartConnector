// ---------------------------------------------------------------------------
// Response Parser Tests
//
// Unit tests for GridStatusResponseParser. Verifies JSON parsing of the
// array-of-objects response format from the GridStatus.io API.
// ---------------------------------------------------------------------------

using System;
using NUnit.Framework;

namespace SmartConnector.GridStatusExtension.Tests
{
    [TestFixture]
    public class ResponseParserFixture
    {
        // -- Basic parsing ----------------------------------------------------

        [Test]
        public void Parse_ValidResponse_ReturnsRows()
        {
            string json = @"{
                ""status_code"": 200,
                ""data"": [
                    {""interval_start_utc"": ""2026-06-01T10:00:00Z"", ""lmp"": 45.5},
                    {""interval_start_utc"": ""2026-06-01T11:00:00Z"", ""lmp"": 52.3}
                ]
            }";

            var result = GridStatusResponseParser.Parse(json);

            Assert.That(result.Rows.Count, Is.EqualTo(2));
            Assert.That(result.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void Parse_ExtractsTimestamp()
        {
            string json = @"{
                ""data"": [
                    {""interval_start_utc"": ""2026-06-01T14:00:00Z"", ""lmp"": 30.0}
                ]
            }";

            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].IntervalStartUtc,
                Is.EqualTo(new DateTime(2026, 6, 1, 14, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void Parse_ExtractsNumericValue()
        {
            string json = @"{
                ""data"": [
                    {""interval_start_utc"": ""2026-06-01T14:00:00Z"", ""lmp"": 42.75}
                ]
            }";

            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].GetDouble("lmp"), Is.EqualTo(42.75));
        }

        // -- Edge cases -------------------------------------------------------

        [Test]
        public void Parse_NullJson_ReturnsEmptyResponse()
        {
            var result = GridStatusResponseParser.Parse(null);
            Assert.That(result.Rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void Parse_EmptyJson_ReturnsEmptyResponse()
        {
            var result = GridStatusResponseParser.Parse("");
            Assert.That(result.Rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void Parse_NoDataKey_ReturnsEmptyResponse()
        {
            string json = @"{""status_code"": 200}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void Parse_EmptyDataArray_ReturnsEmptyResponse()
        {
            string json = @"{""data"": []}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows.Count, Is.EqualTo(0));
        }

        // -- DataRow.GetDouble ------------------------------------------------

        [Test]
        public void GetDouble_MissingColumn_ReturnsDefault()
        {
            string json = @"{""data"": [{""interval_start_utc"": ""2026-06-01T00:00:00Z"", ""lmp"": 10}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].GetDouble("nonexistent"), Is.EqualTo(0.0));
        }

        [Test]
        public void GetDouble_NullValue_ReturnsDefault()
        {
            string json = @"{""data"": [{""interval_start_utc"": ""2026-06-01T00:00:00Z"", ""lmp"": null}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].GetDouble("lmp"), Is.EqualTo(0.0));
        }

        [Test]
        public void GetDouble_IntegerValue_ReturnsAsDouble()
        {
            string json = @"{""data"": [{""interval_start_utc"": ""2026-06-01T00:00:00Z"", ""load"": 5000}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].GetDouble("load"), Is.EqualTo(5000.0));
        }

        [Test]
        public void GetDouble_StringNumeric_ParsesCorrectly()
        {
            string json = @"{""data"": [{""interval_start_utc"": ""2026-06-01T00:00:00Z"", ""value"": ""123.45""}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].GetDouble("value"), Is.EqualTo(123.45));
        }

        [Test]
        public void GetDouble_NegativePrice_ParsesCorrectly()
        {
            string json = @"{""data"": [{""interval_start_utc"": ""2026-06-01T00:00:00Z"", ""lmp"": -15.5}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].GetDouble("lmp"), Is.EqualTo(-15.5));
        }

        // -- DataRow.GetString ------------------------------------------------

        [Test]
        public void GetString_ExistingColumn_ReturnsValue()
        {
            string json = @"{""data"": [{""interval_start_utc"": ""2026-06-01T00:00:00Z"", ""location"": ""HB_HOUSTON""}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].GetString("location"), Is.EqualTo("HB_HOUSTON"));
        }

        [Test]
        public void GetString_MissingColumn_ReturnsDefault()
        {
            string json = @"{""data"": [{""interval_start_utc"": ""2026-06-01T00:00:00Z""}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].GetString("missing"), Is.EqualTo(""));
        }

        // -- Timestamp parsing ------------------------------------------------

        [Test]
        public void Parse_MissingTimestamp_SetsMinValue()
        {
            string json = @"{""data"": [{""lmp"": 42.0}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].IntervalStartUtc, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void Parse_LocalTimestampFallback_WhenUtcMissing()
        {
            string json = @"{""data"": [{""interval_start_local"": ""2026-06-01T10:00:00"", ""lmp"": 42.0}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].IntervalStartUtc, Is.Not.EqualTo(DateTime.MinValue));
        }

        // -- HasNextPage metadata ---------------------------------------------

        [Test]
        public void Parse_HasNextPage_True()
        {
            string json = @"{""data"": [], ""meta"": {""hasNextPage"": true}}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.HasNextPage, Is.True);
        }

        [Test]
        public void Parse_HasNextPage_False()
        {
            string json = @"{""data"": [], ""meta"": {""hasNextPage"": false}}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.HasNextPage, Is.False);
        }

        // -- API Usage parsing ------------------------------------------------

        [Test]
        public void ParseApiUsage_ValidResponse_ParsesFields()
        {
            string json = @"{
                ""plan_name"": ""Free"",
                ""limits"": {
                    ""api_rows_returned_limit"": 1000000,
                    ""api_requests_limit"": 10000,
                    ""api_rows_per_response_limit"": 10000,
                    ""per_second_api_rate_limit"": 1,
                    ""per_minute_api_rate_limit"": 30
                },
                ""current_period_usage"": {
                    ""total_requests"": 150,
                    ""total_api_rows_returned"": 25000
                }
            }";

            var usage = GridStatusResponseParser.ParseApiUsage(json);
            Assert.That(usage.PlanName, Is.EqualTo("Free"));
            Assert.That(usage.RowLimit, Is.EqualTo(1000000));
            Assert.That(usage.TotalRowsReturned, Is.EqualTo(25000));
        }

        [Test]
        public void ParseApiUsage_NullJson_ReturnsDefaults()
        {
            var usage = GridStatusResponseParser.ParseApiUsage(null);
            Assert.That(usage.PlanName, Is.EqualTo(""));
            Assert.That(usage.RowLimit, Is.EqualTo(0));
        }

        [Test]
        public void ApiUsageInfo_RowBudgetUsedPercent_Calculates()
        {
            var usage = new ApiUsageInfo
            {
                RowLimit = 1000000,
                TotalRowsReturned = 250000
            };
            Assert.That(usage.RowBudgetUsedPercent, Is.EqualTo(25.0));
        }

        [Test]
        public void ApiUsageInfo_RowBudgetUsedPercent_ZeroLimit_ReturnsZero()
        {
            var usage = new ApiUsageInfo { RowLimit = 0, TotalRowsReturned = 100 };
            Assert.That(usage.RowBudgetUsedPercent, Is.EqualTo(0.0));
        }

        // -- Column name case insensitivity -----------------------------------

        [Test]
        public void Parse_ColumnLookup_CaseInsensitive()
        {
            string json = @"{""data"": [{""Interval_Start_UTC"": ""2026-06-01T00:00:00Z"", ""LMP"": 42.0}]}";
            var result = GridStatusResponseParser.Parse(json);
            Assert.That(result.Rows[0].GetDouble("lmp"), Is.EqualTo(42.0));
        }
    }
}
