using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartConnector.WeatherExtension
{
    /// <summary>
    /// Aggregates the 3-hour forecast steps from the Forecast 2.5 API into
    /// daily summaries in the location's local timezone. Pure logic so the
    /// daily roll-up can be unit tested without any HTTP calls.
    /// </summary>
    public static class ForecastAggregator
    {
        public sealed class DailyForecast
        {
            public DateTime Date { get; set; }      // local calendar date (midnight)
            public double HighTemp { get; set; }
            public double LowTemp { get; set; }
            public double Pressure { get; set; }
            public double Humidity { get; set; }
            public string Description { get; set; }
        }

        /// <summary>
        /// Returns up to <paramref name="days"/> daily summaries for the dates
        /// strictly after the reference day (i.e. the future forecast),
        /// ordered by date. High/low use the per-step temperature extremes;
        /// pressure and humidity are daily averages; the description is taken
        /// from the step closest to local noon.
        /// </summary>
        public static List<DailyForecast> Aggregate(
            IReadOnlyList<ForecastPoint> points,
            int tzOffsetSeconds,
            DateTime referenceUtc,
            int days)
        {
            var result = new List<DailyForecast>();
            if (points == null || points.Count == 0 || days <= 0)
                return result;

            var offset = TimeSpan.FromSeconds(tzOffsetSeconds);
            var referenceLocalDate = (referenceUtc + offset).Date;

            var byDay = points
                .Where(p => p.TimestampUtc != DateTime.MinValue)
                .GroupBy(p => (p.TimestampUtc + offset).Date)
                .Where(g => g.Key > referenceLocalDate)
                .OrderBy(g => g.Key);

            foreach (var group in byDay)
            {
                if (result.Count >= days) break;

                var steps = group.ToList();
                var noon = steps
                    .OrderBy(p => Math.Abs(((p.TimestampUtc + offset).TimeOfDay
                        - TimeSpan.FromHours(12)).TotalMinutes))
                    .First();

                result.Add(new DailyForecast
                {
                    Date = group.Key,
                    HighTemp = Math.Round(steps.Max(p => p.TempMax), 1),
                    LowTemp = Math.Round(steps.Min(p => p.TempMin), 1),
                    Pressure = Math.Round(steps.Average(p => p.Pressure), 0),
                    Humidity = Math.Round(steps.Average(p => p.Humidity), 0),
                    Description = noon.Description ?? ""
                });
            }

            return result;
        }
    }
}
