using System;
using System.Collections.Generic;

namespace SmartConnector.EntsoeExtension
{
    /// <summary>
    /// Derives peak-demand signals from ENTSO-E load forecast data.
    /// Mirrors the load logic in the GridStatus extension so consumers
    /// can rely on the same Load points across both integrations.
    /// </summary>
    public static class LoadAnalyzer
    {
        /// <summary>
        /// Finds the forecast peak (MW) and the timestamp at which it occurs.
        /// </summary>
        public static (double PeakMw, DateTime PeakHour) FindForecastPeak(
            IReadOnlyList<EntsoeDataPoint> points)
        {
            if (points == null || points.Count == 0)
                return (0.0, DateTime.MinValue);

            double maxLoad = double.MinValue;
            DateTime peakHour = DateTime.MinValue;

            foreach (var point in points)
            {
                if (point.Value > maxLoad)
                {
                    maxLoad = point.Value;
                    peakHour = point.Timestamp;
                }
            }

            return (maxLoad == double.MinValue ? 0.0 : maxLoad, peakHour);
        }

        /// <summary>
        /// Computes peak risk level (0 = low, 1 = medium, 2 = high) based on
        /// current load as a fraction of the forecast peak.
        /// </summary>
        public static int ComputePeakRiskLevel(
            double currentLoad,
            double forecastPeak,
            double mediumThreshold = 0.85,
            double highThreshold = 0.95)
        {
            if (forecastPeak <= 0) return 0;

            double fraction = currentLoad / forecastPeak;
            if (fraction >= highThreshold) return 2;
            if (fraction >= mediumThreshold) return 1;
            return 0;
        }
    }
}
