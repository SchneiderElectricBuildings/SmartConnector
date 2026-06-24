using System;
using System.Collections.Generic;

namespace SmartConnector.GridStatusExtension
{
    /// <summary>
    /// Decides whether trend-log history should be backfilled and over what
    /// window. Pure logic so the decision can be unit tested without an EWS
    /// Server. Mirrors the planner in the ENTSO-E extension so the two
    /// integrations share an identical backfill strategy.
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
            /// When set, only history rows strictly after this timestamp
            /// should be written. Prevents duplicate samples on gap backfill.
            /// </summary>
            public DateTime? FilterAfter { get; set; }
        }

        /// <summary>
        /// Plans a backfill window.
        /// </summary>
        /// <param name="lastUpdate">
        /// The persisted last-update timestamp for the ISO, or null if it has
        /// never been updated (initial setup).
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
        /// Returns the rows eligible for backfill: those with a valid timestamp
        /// and (when <paramref name="filterAfter"/> is set) strictly later than
        /// it. Output is ordered by timestamp.
        /// </summary>
        public static List<DataRow> FilterRows(
            IReadOnlyList<DataRow> rows,
            DateTime? filterAfter)
        {
            var result = new List<DataRow>();
            if (rows == null) return result;

            foreach (var row in rows)
            {
                if (row.IntervalStartUtc == DateTime.MinValue) continue;
                if (filterAfter.HasValue && row.IntervalStartUtc <= filterAfter.Value)
                    continue;
                result.Add(row);
            }

            result.Sort((a, b) => a.IntervalStartUtc.CompareTo(b.IntervalStartUtc));
            return result;
        }
    }
}
