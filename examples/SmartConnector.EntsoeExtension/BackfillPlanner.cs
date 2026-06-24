using System;
using System.Collections.Generic;

namespace SmartConnector.EntsoeExtension
{
    /// <summary>
    /// Decides whether trend-log history should be backfilled and over what
    /// window. Pure logic so the decision can be unit tested without an EWS
    /// Server. The processor uses the result to query ENTSO-E and write
    /// history records via AddHistoryRecordsManually.
    /// </summary>
    public static class BackfillPlanner
    {
        /// <summary>
        /// The outcome of a backfill decision.
        /// </summary>
        public struct Plan
        {
            public bool ShouldBackfill { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }

            /// <summary>
            /// When set, only history points strictly after this timestamp
            /// should be written. Prevents duplicate samples on gap backfill.
            /// </summary>
            public DateTime? FilterAfter { get; set; }
        }

        /// <summary>
        /// Plans a backfill window.
        /// </summary>
        /// <param name="lastUpdate">
        /// The persisted last-update timestamp for the zone, or null if the
        /// zone has never been updated (initial setup).
        /// </param>
        /// <param name="now">Current UTC time.</param>
        /// <param name="backfillDays">Maximum days of history to backfill.</param>
        /// <param name="gapThresholdHours">
        /// Minimum gap (hours) before a reconnection backfill is performed.
        /// </param>
        public static Plan Create(
            DateTime? lastUpdate,
            DateTime now,
            int backfillDays,
            double gapThresholdHours = 1.1)
        {
            if (backfillDays <= 0)
                return new Plan { ShouldBackfill = false };

            // Initial setup — no prior update timestamp.
            if (!lastUpdate.HasValue || lastUpdate.Value == DateTime.MinValue)
            {
                return new Plan
                {
                    ShouldBackfill = true,
                    Start = now.AddDays(-backfillDays),
                    End = now,
                    FilterAfter = null
                };
            }

            // Reconnection — only backfill if the gap is meaningful.
            var gapHours = (now - lastUpdate.Value).TotalHours;
            if (gapHours < gapThresholdHours)
                return new Plan { ShouldBackfill = false };

            var cap = now.AddDays(-backfillDays);
            var start = lastUpdate.Value > cap ? lastUpdate.Value : cap;

            return new Plan
            {
                ShouldBackfill = true,
                Start = start,
                End = now,
                FilterAfter = lastUpdate.Value
            };
        }

        /// <summary>
        /// Returns the points eligible for backfill: those with a valid
        /// timestamp and (when <paramref name="filterAfter"/> is set) strictly
        /// later than it. Output is ordered by timestamp.
        /// </summary>
        public static List<EntsoeDataPoint> FilterPoints(
            IReadOnlyList<EntsoeDataPoint> points,
            DateTime? filterAfter)
        {
            var result = new List<EntsoeDataPoint>();
            if (points == null) return result;

            foreach (var point in points)
            {
                if (point.Timestamp == DateTime.MinValue) continue;
                if (filterAfter.HasValue && point.Timestamp <= filterAfter.Value)
                    continue;
                result.Add(point);
            }

            result.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            return result;
        }

        /// <summary>
        /// Reduces a sorted point list to one sample per hour (the first point
        /// in each hour). Keeps the volume of history records written during
        /// backfill manageable — sub-hourly series (e.g. 15-minute load data)
        /// would otherwise write four times as many records as the hourly
        /// granularity a trend viewer needs. Hourly series are unaffected.
        /// </summary>
        public static List<EntsoeDataPoint> ResampleHourly(
            IReadOnlyList<EntsoeDataPoint> points)
        {
            var result = new List<EntsoeDataPoint>();
            if (points == null) return result;

            var seenHours = new HashSet<DateTime>();
            foreach (var point in points)
            {
                var hourKey = point.Timestamp.Date.AddHours(point.Timestamp.Hour);
                if (seenHours.Add(hourKey))
                    result.Add(point);
            }

            return result;
        }
    }
}
