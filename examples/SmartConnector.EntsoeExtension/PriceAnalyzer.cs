// ---------------------------------------------------------------------------
// Price Analyzer — Percentile Calculation and Classification
//
// Classifies hourly electricity prices into Cheap/Normal/Expensive categories
// based on user-configured percentile thresholds. Provides load-shifting
// signals for building management system automation.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartConnector.EntsoeExtension
{
    /// <summary>
    /// Price classification category for load-shifting decisions.
    /// </summary>
    public enum PriceCategory
    {
        Cheap,
        Normal,
        Expensive
    }

    /// <summary>
    /// An hourly price data point with its classification.
    /// </summary>
    public sealed class ClassifiedPrice
    {
        public int Hour { get; set; }
        public double Price { get; set; }
        public PriceCategory Category { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Complete analysis of day-ahead prices for load-shifting decisions.
    /// </summary>
    public sealed class PriceAnalysisResult
    {
        public double CurrentPrice { get; set; }
        public double NextHourPrice { get; set; }
        public PriceCategory CurrentCategory { get; set; }
        public int Level { get; set; }
        public int RankToday { get; set; }
        public double PercentileToday { get; set; }
        public bool NegativeActive { get; set; }
        public double DayMinimum { get; set; }
        public double DayMaximum { get; set; }
        public double DayAverage { get; set; }
        public double CheapThreshold { get; set; }
        public double ExpensiveThreshold { get; set; }
        public List<ClassifiedPrice> HourlyPrices { get; set; } = new List<ClassifiedPrice>();
        public bool IsCurrentlyCheap { get; set; }
        public bool IsCurrentlyExpensive { get; set; }
        public int HoursUntilCheap { get; set; }
        public int HoursUntilExpensive { get; set; }
        public string CheapHoursToday { get; set; }
        public string ExpensiveHoursToday { get; set; }
    }

    /// <summary>
    /// Analyzes electricity price data and produces load-shifting signals.
    /// </summary>
    public static class PriceAnalyzer
    {
        /// <summary>
        /// Analyzes a set of hourly price data points and produces load-shifting signals.
        /// </summary>
        /// <param name="dataPoints">Hourly price data points from the ENTSO-E API.</param>
        /// <param name="cheapPercentile">Percentile (0–100) below which prices are classified as Cheap.</param>
        /// <param name="expensivePercentile">Percentile (0–100) above which prices are classified as Expensive.</param>
        /// <param name="currentUtcHour">The current UTC hour for determining current-hour signals.</param>
        /// <returns>Complete price analysis with load-shifting signals.</returns>
        public static PriceAnalysisResult Analyze(
            IReadOnlyList<EntsoeDataPoint> dataPoints,
            int cheapPercentile,
            int expensivePercentile,
            DateTime currentUtcHour)
        {
            if (dataPoints == null || dataPoints.Count == 0)
            {
                return new PriceAnalysisResult();
            }

            var prices = dataPoints.Select(dp => dp.Value).ToList();

            double cheapThreshold = CalculatePercentile(prices, cheapPercentile);
            double expensiveThreshold = CalculatePercentile(prices, expensivePercentile);

            var classifiedPrices = new List<ClassifiedPrice>();
            foreach (var dp in dataPoints)
            {
                classifiedPrices.Add(new ClassifiedPrice
                {
                    Hour = dp.Timestamp.Hour,
                    Price = dp.Value,
                    Category = Classify(dp.Value, cheapThreshold, expensiveThreshold),
                    Timestamp = dp.Timestamp
                });
            }

            // Find the data point closest to the current hour
            var currentPoint = classifiedPrices
                .OrderBy(cp => Math.Abs((cp.Timestamp - currentUtcHour).TotalMinutes))
                .FirstOrDefault();

            // Calculate hours until next cheap/expensive window from current time
            int hoursUntilCheap = CalculateHoursUntil(
                classifiedPrices, currentUtcHour, PriceCategory.Cheap);
            int hoursUntilExpensive = CalculateHoursUntil(
                classifiedPrices, currentUtcHour, PriceCategory.Expensive);

            var cheapHours = classifiedPrices
                .Where(cp => cp.Category == PriceCategory.Cheap)
                .Select(cp => cp.Hour.ToString("D2"))
                .ToList();

            var expensiveHours = classifiedPrices
                .Where(cp => cp.Category == PriceCategory.Expensive)
                .Select(cp => cp.Hour.ToString("D2"))
                .ToList();

            // Standard interface: Level (0=cheap, 1=normal, 2=expensive)
            var category = currentPoint?.Category ?? PriceCategory.Normal;
            int level = category == PriceCategory.Cheap ? 0
                : category == PriceCategory.Expensive ? 2 : 1;

            // Standard interface: RankToday and PercentileToday
            double currentPrice = currentPoint?.Price ?? 0.0;
            int rank = 1;
            int belowCount = 0;
            foreach (double p in prices)
            {
                if (p < currentPrice) { rank++; belowCount++; }
            }
            rank = Math.Min(prices.Count - belowCount, prices.Count);
            double percentile = prices.Count > 0
                ? (double)belowCount / prices.Count * 100.0 : 50.0;

            // Standard interface: NextHour
            int currentHourIndex = currentPoint?.Hour ?? 0;
            int nextHourIndex = (currentHourIndex + 1) % 24;
            var nextHourPoint = classifiedPrices
                .FirstOrDefault(cp => cp.Hour == nextHourIndex);
            double nextHourPrice = nextHourPoint?.Price ?? currentPrice;

            return new PriceAnalysisResult
            {
                CurrentPrice = currentPrice,
                NextHourPrice = nextHourPrice,
                CurrentCategory = category,
                Level = level,
                RankToday = rank,
                PercentileToday = percentile,
                NegativeActive = currentPrice < 0,
                DayMinimum = prices.Min(),
                DayMaximum = prices.Max(),
                DayAverage = Math.Round(prices.Average(), 2),
                CheapThreshold = Math.Round(cheapThreshold, 2),
                ExpensiveThreshold = Math.Round(expensiveThreshold, 2),
                HourlyPrices = classifiedPrices,
                IsCurrentlyCheap = category == PriceCategory.Cheap,
                IsCurrentlyExpensive = category == PriceCategory.Expensive,
                HoursUntilCheap = hoursUntilCheap,
                HoursUntilExpensive = hoursUntilExpensive,
                CheapHoursToday = string.Join(",", cheapHours),
                ExpensiveHoursToday = string.Join(",", expensiveHours)
            };
        }

        /// <summary>
        /// Classifies a price value relative to the cheap and expensive thresholds.
        /// </summary>
        public static PriceCategory Classify(
            double price, double cheapThreshold, double expensiveThreshold)
        {
            if (price <= cheapThreshold) return PriceCategory.Cheap;
            if (price >= expensiveThreshold) return PriceCategory.Expensive;
            return PriceCategory.Normal;
        }

        /// <summary>
        /// Calculates the percentile value from a list of prices using linear interpolation.
        /// </summary>
        /// <param name="prices">The price values.</param>
        /// <param name="percentile">The percentile (0–100).</param>
        /// <returns>The value at the given percentile.</returns>
        public static double CalculatePercentile(IReadOnlyList<double> prices, int percentile)
        {
            if (prices == null || prices.Count == 0) return 0.0;
            if (percentile <= 0) return prices.Min();
            if (percentile >= 100) return prices.Max();

            var sorted = prices.OrderBy(p => p).ToList();
            double rank = (percentile / 100.0) * (sorted.Count - 1);
            int lowerIndex = (int)Math.Floor(rank);
            int upperIndex = (int)Math.Ceiling(rank);

            if (lowerIndex == upperIndex)
                return sorted[lowerIndex];

            double fraction = rank - lowerIndex;
            return sorted[lowerIndex] + fraction * (sorted[upperIndex] - sorted[lowerIndex]);
        }

        private static int CalculateHoursUntil(
            List<ClassifiedPrice> prices, DateTime currentUtcHour, PriceCategory targetCategory)
        {
            var futurePrices = prices
                .Where(cp => cp.Timestamp > currentUtcHour && cp.Category == targetCategory)
                .OrderBy(cp => cp.Timestamp)
                .ToList();

            if (!futurePrices.Any())
                return -1; // No future window of this category today

            var nextWindow = futurePrices.First();
            return (int)Math.Ceiling((nextWindow.Timestamp - currentUtcHour).TotalHours);
        }
    }
}
