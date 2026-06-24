// ---------------------------------------------------------------------------
// Signal Derivation Logic
//
// Pure functions that compute derived signals from raw GridStatus.io data.
// All functions are stateless and unit-testable. No EWS or network
// dependencies.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartConnector.GridStatusExtension
{
    /// <summary>
    /// Results of day-ahead price analysis for a single market.
    /// </summary>
    public sealed class PriceSignals
    {
        public double RealTimeLmp { get; set; }
        public double CurrentPrice { get; set; }
        public double NextHour { get; set; }
        public int Level { get; set; }
        public string PriceCategory { get; set; }
        public int RankToday { get; set; }
        public double PercentileToday { get; set; }
        public bool NegativeActive { get; set; }
        public double CheapThreshold { get; set; }
        public double ExpensiveThreshold { get; set; }
        public string Currency { get; set; }
        public string Unit { get; set; }
        public double DayMinimum { get; set; }
        public double DayMaximum { get; set; }
        public double DayAverage { get; set; }
    }

    /// <summary>
    /// Load-shifting signals derived from day-ahead prices and fuel mix.
    /// </summary>
    public sealed class LoadShiftingSignals
    {
        public bool IsCurrentlyCheap { get; set; }
        public bool IsCurrentlyExpensive { get; set; }
        public int HoursUntilCheap { get; set; }
        public int HoursUntilExpensive { get; set; }
        public string CheapHoursToday { get; set; }
        public string ExpensiveHoursToday { get; set; }
        public double RenewablePercent { get; set; }
    }

    /// <summary>
    /// Results of load analysis for a single market.
    /// </summary>
    public sealed class LoadSignals
    {
        public double Current { get; set; }
        public double ForecastPeakToday { get; set; }
        public DateTime ForecastPeakHour { get; set; }
        public int PeakRiskLevel { get; set; }
    }

    /// <summary>
    /// Results of fuel mix and carbon analysis for a single market.
    /// </summary>
    public sealed class FuelMixSignals
    {
        public double RenewablePct { get; set; }
        public double GridIntensity { get; set; }
        public int CarbonLevel { get; set; }
        public double SolarMw { get; set; }
        public double WindMw { get; set; }
        public double GasMw { get; set; }
    }

    /// <summary>
    /// Pure derivation functions for grid signals.
    /// </summary>
    public static class SignalDerivation
    {
        // -- Price signals ----------------------------------------------------

        /// <summary>
        /// Computes the price level (0=cheap, 1=normal, 2=expensive) based
        /// on the current hour's day-ahead price relative to today's prices.
        /// </summary>
        /// <param name="currentPrice">Day-ahead price for the current hour.</param>
        /// <param name="dayAheadPrices">All day-ahead prices for today, sorted by interval.</param>
        /// <param name="cheapPercentile">Percentile below which prices are cheap (1-49).</param>
        /// <param name="expensivePercentile">Percentile above which prices are expensive (51-99).</param>
        public static int ComputePriceLevel(
            double currentPrice,
            IList<double> dayAheadPrices,
            int cheapPercentile,
            int expensivePercentile)
        {
            if (dayAheadPrices == null || dayAheadPrices.Count == 0)
                return 1;

            double cheapThreshold = ComputePercentileValue(dayAheadPrices, cheapPercentile);
            double expensiveThreshold = ComputePercentileValue(dayAheadPrices, expensivePercentile);

            if (currentPrice <= cheapThreshold) return 0;
            if (currentPrice >= expensiveThreshold) return 2;
            return 1;
        }

        /// <summary>
        /// Computes the rank of the current price among today's day-ahead
        /// prices (1 = cheapest, N = most expensive).
        /// </summary>
        public static int ComputeRankToday(
            double currentPrice, IList<double> dayAheadPrices)
        {
            if (dayAheadPrices == null || dayAheadPrices.Count == 0)
                return 1;

            var sorted = dayAheadPrices.OrderBy(p => p).ToList();
            int rank = 1;
            foreach (double p in sorted)
            {
                if (currentPrice <= p) break;
                rank++;
            }

            return Math.Min(rank, sorted.Count);
        }

        /// <summary>
        /// Computes the percentile of the current price among today's prices
        /// (0 = cheapest, 100 = most expensive).
        /// </summary>
        public static double ComputePercentileToday(
            double currentPrice, IList<double> dayAheadPrices)
        {
            if (dayAheadPrices == null || dayAheadPrices.Count == 0)
                return 50.0;

            int belowCount = dayAheadPrices.Count(p => p < currentPrice);
            return (double)belowCount / dayAheadPrices.Count * 100.0;
        }

        // -- Load signals -----------------------------------------------------

        /// <summary>
        /// Computes peak risk level (0=low, 1=medium, 2=high) based on
        /// current load as a fraction of the forecast peak.
        /// </summary>
        /// <param name="currentLoad">Current actual load in MW.</param>
        /// <param name="forecastPeak">Forecast peak for today in MW.</param>
        /// <param name="mediumThreshold">Fraction of peak for medium risk (e.g. 0.85).</param>
        /// <param name="highThreshold">Fraction of peak for high risk (e.g. 0.95).</param>
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

        /// <summary>
        /// Finds the forecast peak and the hour it occurs from load
        /// forecast data rows.
        /// </summary>
        public static (double PeakMw, DateTime PeakHour) FindForecastPeak(
            IList<DataRow> forecastRows, string loadColumn = "load")
        {
            if (forecastRows == null || forecastRows.Count == 0)
                return (0.0, DateTime.MinValue);

            double maxLoad = double.MinValue;
            DateTime peakHour = DateTime.MinValue;

            foreach (var row in forecastRows)
            {
                double load = row.GetDouble(loadColumn);
                if (load > maxLoad)
                {
                    maxLoad = load;
                    peakHour = row.IntervalStartUtc;
                }
            }

            return (maxLoad == double.MinValue ? 0.0 : maxLoad, peakHour);
        }

        // -- Carbon and fuel mix signals --------------------------------------

        /// <summary>
        /// Default emission factors in gCO2/kWh by fuel type.
        /// Source: EPA eGRID, IEA. These are approximations.
        /// </summary>
        public static readonly Dictionary<string, double> DefaultEmissionFactors =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["coal"] = 980,
                ["gas"] = 410,
                ["natural_gas"] = 410,
                ["oil"] = 650,
                ["petroleum"] = 650,
                ["nuclear"] = 12,
                ["hydro"] = 24,
                ["hydroelectric"] = 24,
                ["wind"] = 11,
                ["solar"] = 45,
                ["geothermal"] = 38,
                ["biomass"] = 230,
                ["other"] = 500,
            };

        /// <summary>
        /// Computes the weighted average carbon intensity from a fuel mix.
        /// </summary>
        /// <param name="fuelMix">Dictionary of fuel type to MW generation.</param>
        /// <param name="emissionFactors">Per-fuel gCO2/kWh factors. Defaults used if null.</param>
        public static double ComputeGridIntensity(
            IDictionary<string, double> fuelMix,
            IDictionary<string, double> emissionFactors = null)
        {
            if (fuelMix == null || fuelMix.Count == 0)
                return 0.0;

            var factors = emissionFactors ?? DefaultEmissionFactors;

            double totalMw = 0;
            double weightedSum = 0;

            foreach (var kvp in fuelMix)
            {
                double mw = kvp.Value;
                if (mw <= 0) continue;

                totalMw += mw;

                double factor = factors.TryGetValue(kvp.Key, out var f)
                    ? f
                    : DefaultEmissionFactors.TryGetValue("other", out var d) ? d : 500;

                weightedSum += mw * factor;
            }

            return totalMw > 0 ? weightedSum / totalMw : 0.0;
        }

        /// <summary>
        /// Computes the renewable percentage from a fuel mix.
        /// Renewable fuels: solar, wind, hydro, hydroelectric, geothermal.
        /// </summary>
        public static double ComputeRenewablePct(
            IDictionary<string, double> fuelMix)
        {
            if (fuelMix == null || fuelMix.Count == 0)
                return 0.0;

            var renewableKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "solar", "wind", "hydro", "hydroelectric", "geothermal"
            };

            double totalMw = 0;
            double renewableMw = 0;

            foreach (var kvp in fuelMix)
            {
                double mw = kvp.Value;
                if (mw <= 0) continue;

                totalMw += mw;
                if (renewableKeys.Contains(kvp.Key))
                    renewableMw += mw;
            }

            return totalMw > 0 ? renewableMw / totalMw * 100.0 : 0.0;
        }

        /// <summary>
        /// Computes carbon level (0=clean, 1=normal, 2=dirty) from grid
        /// intensity in gCO2/kWh.
        /// </summary>
        /// <param name="intensity">Grid intensity in gCO2/kWh.</param>
        /// <param name="cleanThreshold">Below this is clean (default 200).</param>
        /// <param name="dirtyThreshold">Above this is dirty (default 500).</param>
        public static int ComputeCarbonLevel(
            double intensity,
            double cleanThreshold = 200.0,
            double dirtyThreshold = 500.0)
        {
            if (intensity <= cleanThreshold) return 0;
            if (intensity >= dirtyThreshold) return 2;
            return 1;
        }

        // -- Load shifting signals --------------------------------------------

        /// <summary>
        /// Computes load-shifting signals from the full set of day-ahead
        /// prices, the current hour, and the renewable percentage.
        /// </summary>
        public static LoadShiftingSignals ComputeLoadShifting(
            IList<double> dayAheadPrices,
            int currentHour,
            int cheapPercentile,
            int expensivePercentile,
            double renewablePct)
        {
            var result = new LoadShiftingSignals
            {
                RenewablePercent = Math.Round(renewablePct, 1),
                HoursUntilCheap = -1,
                HoursUntilExpensive = -1,
                CheapHoursToday = "",
                ExpensiveHoursToday = ""
            };

            if (dayAheadPrices == null || dayAheadPrices.Count == 0)
                return result;

            double cheapThreshold = ComputePercentileValue(dayAheadPrices, cheapPercentile);
            double expensiveThreshold = ComputePercentileValue(dayAheadPrices, expensivePercentile);

            var cheapHours = new List<string>();
            var expensiveHours = new List<string>();

            for (int h = 0; h < dayAheadPrices.Count && h < 24; h++)
            {
                double price = dayAheadPrices[h];
                if (price <= cheapThreshold)
                    cheapHours.Add(h.ToString("D2"));
                else if (price >= expensiveThreshold)
                    expensiveHours.Add(h.ToString("D2"));
            }

            result.CheapHoursToday = string.Join(",", cheapHours);
            result.ExpensiveHoursToday = string.Join(",", expensiveHours);

            int currentLevel = ComputePriceLevel(
                dayAheadPrices.Count > currentHour ? dayAheadPrices[currentHour] : 1,
                dayAheadPrices, cheapPercentile, expensivePercentile);

            result.IsCurrentlyCheap = currentLevel == 0;
            result.IsCurrentlyExpensive = currentLevel == 2;

            result.HoursUntilCheap = FindHoursUntilCategory(
                dayAheadPrices, currentHour, cheapThreshold, isCheap: true);
            result.HoursUntilExpensive = FindHoursUntilCategory(
                dayAheadPrices, currentHour, expensiveThreshold, isCheap: false);

            return result;
        }

        /// <summary>
        /// Computes day-ahead price summary statistics.
        /// </summary>
        public static (double Min, double Max, double Avg) ComputeDaySummary(
            IList<double> dayAheadPrices)
        {
            if (dayAheadPrices == null || dayAheadPrices.Count == 0)
                return (0, 0, 0);

            return (
                dayAheadPrices.Min(),
                dayAheadPrices.Max(),
                Math.Round(dayAheadPrices.Average(), 2));
        }

        private static int FindHoursUntilCategory(
            IList<double> prices, int currentHour,
            double threshold, bool isCheap)
        {
            for (int offset = 1; offset < 24; offset++)
            {
                int h = currentHour + offset;
                if (h >= prices.Count) break;

                bool match = isCheap
                    ? prices[h] <= threshold
                    : prices[h] >= threshold;

                if (match) return offset;
            }
            return -1;
        }

        // -- Utility ----------------------------------------------------------

        /// <summary>
        /// Computes the value at the given percentile using linear interpolation.
        /// </summary>
        public static double ComputePercentileValue(
            IList<double> values, int percentile)
        {
            if (values == null || values.Count == 0)
                return 0.0;

            var sorted = values.OrderBy(v => v).ToList();
            double index = (percentile / 100.0) * (sorted.Count - 1);
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);

            if (lower == upper || upper >= sorted.Count)
                return sorted[lower];

            double fraction = index - lower;
            return sorted[lower] + fraction * (sorted[upper] - sorted[lower]);
        }
    }
}
