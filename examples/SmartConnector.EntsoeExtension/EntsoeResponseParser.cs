// ---------------------------------------------------------------------------
// ENTSO-E XML Response Parser
//
// Parses ENTSO-E Transparency Platform XML responses into strongly-typed
// data structures. Handles both Publication_MarketDocument (prices) and
// GL_MarketDocument (load/generation) namespaces.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace SmartConnector.EntsoeExtension
{
    /// <summary>
    /// A single data point from an ENTSO-E TimeSeries period.
    /// </summary>
    public sealed class EntsoeDataPoint
    {
        public int Position { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// A parsed period from an ENTSO-E TimeSeries.
    /// </summary>
    public sealed class EntsoePeriod
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public TimeSpan Resolution { get; set; }
        public List<EntsoeDataPoint> Points { get; set; } = new List<EntsoeDataPoint>();
    }

    /// <summary>
    /// A parsed TimeSeries from an ENTSO-E response document.
    /// </summary>
    public sealed class EntsoeTimeSeries
    {
        public string Currency { get; set; }
        public string MeasureUnit { get; set; }
        public string PsrType { get; set; }
        public List<EntsoePeriod> Periods { get; set; } = new List<EntsoePeriod>();
    }

    /// <summary>
    /// Parsed result from an ENTSO-E API response.
    /// </summary>
    public sealed class EntsoeParseResult
    {
        public string DocumentType { get; set; }
        public List<EntsoeTimeSeries> TimeSeries { get; set; } = new List<EntsoeTimeSeries>();

        /// <summary>
        /// Returns all data points across all TimeSeries and Periods, ordered by timestamp.
        /// </summary>
        public IReadOnlyList<EntsoeDataPoint> GetAllPoints()
        {
            return TimeSeries
                .SelectMany(ts => ts.Periods)
                .SelectMany(p => p.Points)
                .OrderBy(pt => pt.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Returns the most recent data point, or null if no data.
        /// </summary>
        public EntsoeDataPoint GetLatestPoint()
        {
            return TimeSeries
                .SelectMany(ts => ts.Periods)
                .SelectMany(p => p.Points)
                .OrderByDescending(pt => pt.Timestamp)
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns the most recent data point from TimeSeries matching
        /// any of the given PSR type codes (e.g. "B16" for Solar,
        /// "B18"/"B19" for Wind Offshore/Onshore).
        /// </summary>
        public EntsoeDataPoint GetLatestPointByPsrType(params string[] psrTypes)
        {
            var set = new System.Collections.Generic.HashSet<string>(
                psrTypes, System.StringComparer.OrdinalIgnoreCase);
            return TimeSeries
                .Where(ts => ts.PsrType != null && set.Contains(ts.PsrType))
                .SelectMany(ts => ts.Periods)
                .SelectMany(p => p.Points)
                .OrderByDescending(pt => pt.Timestamp)
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns the sum of the latest data points across all TimeSeries
        /// matching any of the given PSR type codes.
        /// </summary>
        public double? GetLatestSumByPsrType(params string[] psrTypes)
        {
            var set = new System.Collections.Generic.HashSet<string>(
                psrTypes, System.StringComparer.OrdinalIgnoreCase);
            var matching = TimeSeries
                .Where(ts => ts.PsrType != null && set.Contains(ts.PsrType))
                .ToList();

            if (matching.Count == 0) return null;

            double sum = 0;
            foreach (var ts in matching)
            {
                var latest = ts.Periods
                    .SelectMany(p => p.Points)
                    .OrderByDescending(pt => pt.Timestamp)
                    .FirstOrDefault();
                if (latest != null) sum += latest.Value;
            }
            return sum;
        }
    }

    /// <summary>
    /// Parses ENTSO-E Transparency Platform XML responses.
    /// </summary>
    public static class EntsoeResponseParser
    {
        /// <summary>
        /// Parses an ENTSO-E XML response string into an <see cref="EntsoeParseResult"/>.
        /// </summary>
        /// <param name="xml">Raw XML response from the ENTSO-E API.</param>
        /// <returns>Parsed result containing all TimeSeries and data points.</returns>
        /// <exception cref="ArgumentException">Thrown when XML is null or empty.</exception>
        /// <exception cref="FormatException">Thrown when XML cannot be parsed.</exception>
        public static EntsoeParseResult Parse(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("XML response must not be empty.", nameof(xml));

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                throw new FormatException("Failed to parse ENTSO-E XML response.", ex);
            }

            if (doc.Root == null)
                throw new FormatException("ENTSO-E XML response has no root element.");

            // Check for acknowledgement errors
            var rootName = doc.Root.Name.LocalName;
            if (rootName == "Acknowledgement_MarketDocument")
            {
                var reason = FindDescendant(doc.Root, "Reason");
                var reasonText = reason != null ? FindDescendant(reason, "text")?.Value : "Unknown error";
                throw new FormatException($"ENTSO-E API error: {reasonText}");
            }

            var result = new EntsoeParseResult
            {
                DocumentType = FindDescendant(doc.Root, "type")?.Value
            };

            foreach (var tsElement in doc.Root.Elements()
                .Where(e => e.Name.LocalName == "TimeSeries"))
            {
                result.TimeSeries.Add(ParseTimeSeries(tsElement));
            }

            return result;
        }

        private static EntsoeTimeSeries ParseTimeSeries(XElement tsElement)
        {
            var ts = new EntsoeTimeSeries
            {
                Currency = FindDescendant(tsElement, "currency_Unit.name")?.Value,
                MeasureUnit = FindDescendant(tsElement, "price_Measure_Unit.name")?.Value
                    ?? FindDescendant(tsElement, "quantity_Measure_Unit.name")?.Value,
                PsrType = FindDescendant(tsElement, "psrType")?.Value
            };

            foreach (var periodElement in tsElement.Elements()
                .Where(e => e.Name.LocalName == "Period"))
            {
                ts.Periods.Add(ParsePeriod(periodElement));
            }

            return ts;
        }

        private static EntsoePeriod ParsePeriod(XElement periodElement)
        {
            var timeInterval = FindDescendant(periodElement, "timeInterval");
            var startStr = FindDescendant(timeInterval, "start")?.Value;
            var endStr = FindDescendant(timeInterval, "end")?.Value;
            var resolutionStr = FindDescendant(periodElement, "resolution")?.Value;

            var period = new EntsoePeriod
            {
                Start = ParseUtcDateTime(startStr),
                End = ParseUtcDateTime(endStr),
                Resolution = ParseResolution(resolutionStr)
            };

            foreach (var pointElement in periodElement.Elements()
                .Where(e => e.Name.LocalName == "Point"))
            {
                var position = ParseInt(FindDescendant(pointElement, "position")?.Value);
                var value = ParseDouble(
                    FindDescendant(pointElement, "price.amount")?.Value
                    ?? FindDescendant(pointElement, "quantity")?.Value);

                period.Points.Add(new EntsoeDataPoint
                {
                    Position = position,
                    Value = value,
                    Timestamp = period.Start + TimeSpan.FromTicks(
                        period.Resolution.Ticks * (position - 1))
                });
            }

            return period;
        }

        /// <summary>
        /// Finds a direct or nested descendant element by local name,
        /// ignoring XML namespaces.
        /// </summary>
        internal static XElement FindDescendant(XElement parent, string localName)
        {
            if (parent == null) return null;
            return parent.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == localName);
        }

        internal static DateTime ParseUtcDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.MinValue;

            return DateTime.Parse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        }

        public static TimeSpan ParseResolution(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return TimeSpan.FromHours(1);

            // Common ENTSO-E resolutions: PT15M, PT30M, PT60M, P1Y
            switch (value.ToUpperInvariant())
            {
                case "PT15M": return TimeSpan.FromMinutes(15);
                case "PT30M": return TimeSpan.FromMinutes(30);
                case "PT60M": return TimeSpan.FromHours(1);
                case "P1D": return TimeSpan.FromDays(1);
                default: return TimeSpan.FromHours(1);
            }
        }

        private static int ParseInt(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            return 0;
        }

        private static double ParseDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out var result))
                return result;
            return 0.0;
        }
    }
}
