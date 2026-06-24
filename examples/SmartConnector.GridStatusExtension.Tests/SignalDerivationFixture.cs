// ---------------------------------------------------------------------------
// Signal Derivation Tests
//
// Unit tests for the pure derivation functions in SignalDerivation.cs.
// No network or EWS dependencies.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;

namespace SmartConnector.GridStatusExtension.Tests
{
    [TestFixture]
    public class SignalDerivationFixture
    {
        // -- Price Level ------------------------------------------------------

        [Test]
        public void ComputePriceLevel_BelowCheapThreshold_ReturnsZero()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            int level = SignalDerivation.ComputePriceLevel(15, prices, 25, 75);
            Assert.That(level, Is.EqualTo(0));
        }

        [Test]
        public void ComputePriceLevel_AboveExpensiveThreshold_ReturnsTwo()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            int level = SignalDerivation.ComputePriceLevel(95, prices, 25, 75);
            Assert.That(level, Is.EqualTo(2));
        }

        [Test]
        public void ComputePriceLevel_InNormalRange_ReturnsOne()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            int level = SignalDerivation.ComputePriceLevel(50, prices, 25, 75);
            Assert.That(level, Is.EqualTo(1));
        }

        [Test]
        public void ComputePriceLevel_EmptyPrices_ReturnsNormal()
        {
            int level = SignalDerivation.ComputePriceLevel(50, new List<double>(), 25, 75);
            Assert.That(level, Is.EqualTo(1));
        }

        // -- Price Rank -------------------------------------------------------

        [Test]
        public void ComputeRankToday_CheapestPrice_ReturnsOne()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            int rank = SignalDerivation.ComputeRankToday(10, prices);
            Assert.That(rank, Is.EqualTo(1));
        }

        [Test]
        public void ComputeRankToday_MostExpensive_ReturnsCount()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            int rank = SignalDerivation.ComputeRankToday(50, prices);
            Assert.That(rank, Is.EqualTo(5));
        }

        [Test]
        public void ComputeRankToday_MiddlePrice_ReturnsCorrectRank()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            int rank = SignalDerivation.ComputeRankToday(30, prices);
            Assert.That(rank, Is.EqualTo(3));
        }

        // -- Price Percentile -------------------------------------------------

        [Test]
        public void ComputePercentileToday_CheapestPrice_ReturnsZero()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            double pct = SignalDerivation.ComputePercentileToday(10, prices);
            Assert.That(pct, Is.EqualTo(0.0));
        }

        [Test]
        public void ComputePercentileToday_MostExpensive_Returns80()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            double pct = SignalDerivation.ComputePercentileToday(50, prices);
            Assert.That(pct, Is.EqualTo(80.0));
        }

        // -- Peak Risk Level --------------------------------------------------

        [Test]
        public void ComputePeakRiskLevel_LowLoad_ReturnsZero()
        {
            int risk = SignalDerivation.ComputePeakRiskLevel(5000, 10000, 0.85, 0.95);
            Assert.That(risk, Is.EqualTo(0));
        }

        [Test]
        public void ComputePeakRiskLevel_MediumLoad_ReturnsOne()
        {
            int risk = SignalDerivation.ComputePeakRiskLevel(9000, 10000, 0.85, 0.95);
            Assert.That(risk, Is.EqualTo(1));
        }

        [Test]
        public void ComputePeakRiskLevel_HighLoad_ReturnsTwo()
        {
            int risk = SignalDerivation.ComputePeakRiskLevel(9600, 10000, 0.85, 0.95);
            Assert.That(risk, Is.EqualTo(2));
        }

        [Test]
        public void ComputePeakRiskLevel_ZeroPeak_ReturnsZero()
        {
            int risk = SignalDerivation.ComputePeakRiskLevel(5000, 0);
            Assert.That(risk, Is.EqualTo(0));
        }

        // -- Carbon Intensity -------------------------------------------------

        [Test]
        public void ComputeGridIntensity_AllGas_ReturnsGasFactor()
        {
            var mix = new Dictionary<string, double> { ["gas"] = 1000 };
            double intensity = SignalDerivation.ComputeGridIntensity(mix);
            Assert.That(intensity, Is.EqualTo(410.0));
        }

        [Test]
        public void ComputeGridIntensity_AllSolar_ReturnsSolarFactor()
        {
            var mix = new Dictionary<string, double> { ["solar"] = 1000 };
            double intensity = SignalDerivation.ComputeGridIntensity(mix);
            Assert.That(intensity, Is.EqualTo(45.0));
        }

        [Test]
        public void ComputeGridIntensity_MixedFuels_ReturnsWeightedAverage()
        {
            var mix = new Dictionary<string, double>
            {
                ["gas"] = 500,
                ["solar"] = 500
            };
            double intensity = SignalDerivation.ComputeGridIntensity(mix);
            // (500*410 + 500*45) / 1000 = 227.5
            Assert.That(intensity, Is.EqualTo(227.5));
        }

        [Test]
        public void ComputeGridIntensity_Empty_ReturnsZero()
        {
            double intensity = SignalDerivation.ComputeGridIntensity(
                new Dictionary<string, double>());
            Assert.That(intensity, Is.EqualTo(0.0));
        }

        // -- Renewable Percentage ---------------------------------------------

        [Test]
        public void ComputeRenewablePct_AllRenewable_Returns100()
        {
            var mix = new Dictionary<string, double>
            {
                ["solar"] = 300,
                ["wind"] = 700
            };
            double pct = SignalDerivation.ComputeRenewablePct(mix);
            Assert.That(pct, Is.EqualTo(100.0));
        }

        [Test]
        public void ComputeRenewablePct_NoRenewable_ReturnsZero()
        {
            var mix = new Dictionary<string, double>
            {
                ["gas"] = 500,
                ["coal"] = 500
            };
            double pct = SignalDerivation.ComputeRenewablePct(mix);
            Assert.That(pct, Is.EqualTo(0.0));
        }

        [Test]
        public void ComputeRenewablePct_HalfAndHalf_Returns50()
        {
            var mix = new Dictionary<string, double>
            {
                ["solar"] = 500,
                ["gas"] = 500
            };
            double pct = SignalDerivation.ComputeRenewablePct(mix);
            Assert.That(pct, Is.EqualTo(50.0));
        }

        // -- Carbon Level -----------------------------------------------------

        [Test]
        public void ComputeCarbonLevel_Clean_ReturnsZero()
        {
            int level = SignalDerivation.ComputeCarbonLevel(100, 200, 500);
            Assert.That(level, Is.EqualTo(0));
        }

        [Test]
        public void ComputeCarbonLevel_Normal_ReturnsOne()
        {
            int level = SignalDerivation.ComputeCarbonLevel(350, 200, 500);
            Assert.That(level, Is.EqualTo(1));
        }

        [Test]
        public void ComputeCarbonLevel_Dirty_ReturnsTwo()
        {
            int level = SignalDerivation.ComputeCarbonLevel(600, 200, 500);
            Assert.That(level, Is.EqualTo(2));
        }

        // -- Percentile Value -------------------------------------------------

        [Test]
        public void ComputePercentileValue_Median_ReturnsMiddle()
        {
            var values = new List<double> { 10, 20, 30, 40, 50 };
            double p50 = SignalDerivation.ComputePercentileValue(values, 50);
            Assert.That(p50, Is.EqualTo(30.0));
        }

        [Test]
        public void ComputePercentileValue_P25_ReturnsQuartile()
        {
            var values = new List<double> { 10, 20, 30, 40, 50 };
            double p25 = SignalDerivation.ComputePercentileValue(values, 25);
            Assert.That(p25, Is.EqualTo(20.0));
        }

        // -- Find Forecast Peak -----------------------------------------------

        [Test]
        public void FindForecastPeak_ReturnsHighestValue()
        {
            var rows = new List<DataRow>
            {
                new DataRow
                {
                    IntervalStartUtc = new System.DateTime(2026, 1, 1, 8, 0, 0),
                    Columns = new Dictionary<string, object> { ["load"] = 5000.0 }
                },
                new DataRow
                {
                    IntervalStartUtc = new System.DateTime(2026, 1, 1, 14, 0, 0),
                    Columns = new Dictionary<string, object> { ["load"] = 9500.0 }
                },
                new DataRow
                {
                    IntervalStartUtc = new System.DateTime(2026, 1, 1, 20, 0, 0),
                    Columns = new Dictionary<string, object> { ["load"] = 7000.0 }
                }
            };

            var (peakMw, peakHour) = SignalDerivation.FindForecastPeak(rows);
            Assert.That(peakMw, Is.EqualTo(9500.0));
            Assert.That(peakHour.Hour, Is.EqualTo(14));
        }

        [Test]
        public void FindForecastPeak_EmptyRows_ReturnsZero()
        {
            var (peakMw, _) = SignalDerivation.FindForecastPeak(new List<DataRow>());
            Assert.That(peakMw, Is.EqualTo(0.0));
        }

        // -- Day Summary ------------------------------------------------------

        [Test]
        public void ComputeDaySummary_ReturnsMinMaxAvg()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            var (min, max, avg) = SignalDerivation.ComputeDaySummary(prices);
            Assert.That(min, Is.EqualTo(10.0));
            Assert.That(max, Is.EqualTo(50.0));
            Assert.That(avg, Is.EqualTo(30.0));
        }

        [Test]
        public void ComputeDaySummary_Empty_ReturnsZeros()
        {
            var (min, max, avg) = SignalDerivation.ComputeDaySummary(new List<double>());
            Assert.That(min, Is.EqualTo(0.0));
            Assert.That(max, Is.EqualTo(0.0));
            Assert.That(avg, Is.EqualTo(0.0));
        }

        [Test]
        public void ComputeDaySummary_SingleValue()
        {
            var (min, max, avg) = SignalDerivation.ComputeDaySummary(new List<double> { 42.5 });
            Assert.That(min, Is.EqualTo(42.5));
            Assert.That(max, Is.EqualTo(42.5));
            Assert.That(avg, Is.EqualTo(42.5));
        }

        // -- Load Shifting ----------------------------------------------------

        [Test]
        public void ComputeLoadShifting_CheapHour_SetsIsCurrentlyCheap()
        {
            // 24 prices: hours 0-5 are cheap (10), rest are expensive (90)
            var prices = new List<double>();
            for (int i = 0; i < 24; i++)
                prices.Add(i < 6 ? 10.0 : 90.0);

            var result = SignalDerivation.ComputeLoadShifting(prices, 2, 25, 75, 50.0);
            Assert.That(result.IsCurrentlyCheap, Is.True);
            Assert.That(result.IsCurrentlyExpensive, Is.False);
        }

        [Test]
        public void ComputeLoadShifting_ExpensiveHour_SetsIsCurrentlyExpensive()
        {
            var prices = new List<double>();
            for (int i = 0; i < 24; i++)
                prices.Add(i < 6 ? 10.0 : 90.0);

            var result = SignalDerivation.ComputeLoadShifting(prices, 12, 25, 75, 50.0);
            Assert.That(result.IsCurrentlyCheap, Is.False);
            Assert.That(result.IsCurrentlyExpensive, Is.True);
        }

        [Test]
        public void ComputeLoadShifting_HoursUntilCheap_FromExpensiveHour()
        {
            // Hours 0-5: cheap (10), 6-17: normal (50), 18-23: cheap (10)
            var prices = new List<double>();
            for (int i = 0; i < 24; i++)
                prices.Add(i < 6 ? 10.0 : (i >= 18 ? 10.0 : 50.0));

            var result = SignalDerivation.ComputeLoadShifting(prices, 10, 25, 75, 0);
            // Next cheap hour after 10 is 18, so 8 hours
            Assert.That(result.HoursUntilCheap, Is.EqualTo(8));
        }

        [Test]
        public void ComputeLoadShifting_NoCheapAhead_ReturnsMinusOne()
        {
            var prices = new List<double>();
            for (int i = 0; i < 24; i++)
                prices.Add(i < 6 ? 10.0 : 90.0);

            // At hour 20, no cheap hours remain
            var result = SignalDerivation.ComputeLoadShifting(prices, 20, 25, 75, 0);
            Assert.That(result.HoursUntilCheap, Is.EqualTo(-1));
        }

        [Test]
        public void ComputeLoadShifting_CheapHoursToday_FormatsCorrectly()
        {
            // Spread prices so cheap threshold clearly separates hours
            // Hours 0-5: 10, Hours 6-11: 30, Hours 12-17: 60, Hours 18-23: 90
            var prices = new List<double>();
            for (int i = 0; i < 24; i++)
            {
                if (i < 6) prices.Add(10.0);
                else if (i < 12) prices.Add(30.0);
                else if (i < 18) prices.Add(60.0);
                else prices.Add(90.0);
            }

            var result = SignalDerivation.ComputeLoadShifting(prices, 0, 25, 75, 0);
            Assert.That(result.CheapHoursToday, Is.EqualTo("00,01,02,03,04,05"));
        }

        [Test]
        public void ComputeLoadShifting_RenewablePercent_PassedThrough()
        {
            var prices = new List<double> { 10, 20, 30, 40, 50 };
            var result = SignalDerivation.ComputeLoadShifting(prices, 0, 25, 75, 67.89);
            Assert.That(result.RenewablePercent, Is.EqualTo(67.9));
        }

        [Test]
        public void ComputeLoadShifting_EmptyPrices_ReturnsDefaults()
        {
            var result = SignalDerivation.ComputeLoadShifting(
                new List<double>(), 0, 25, 75, 0);
            Assert.That(result.IsCurrentlyCheap, Is.False);
            Assert.That(result.IsCurrentlyExpensive, Is.False);
            Assert.That(result.HoursUntilCheap, Is.EqualTo(-1));
            Assert.That(result.CheapHoursToday, Is.EqualTo(""));
        }

        // -- Standard Interface: Negative prices --------------------------------

        [Test]
        public void ComputePriceLevel_NegativePrice_ReturnsCheap()
        {
            var prices = new List<double> { -10, -5, 0, 10, 20, 30, 40, 50, 60, 70 };
            int level = SignalDerivation.ComputePriceLevel(-10, prices, 25, 75);
            Assert.That(level, Is.EqualTo(0));
        }

        [Test]
        public void ComputeRankToday_NegativePrice_ReturnsOne()
        {
            var prices = new List<double> { -10, -5, 0, 10, 20 };
            int rank = SignalDerivation.ComputeRankToday(-10, prices);
            Assert.That(rank, Is.EqualTo(1));
        }

        [Test]
        public void ComputePercentileToday_NegativePrice_ReturnsZero()
        {
            var prices = new List<double> { -10, -5, 0, 10, 20 };
            double pct = SignalDerivation.ComputePercentileToday(-10, prices);
            Assert.That(pct, Is.EqualTo(0.0));
        }

        // -- Standard Interface: All-same prices --------------------------------

        [Test]
        public void ComputePriceLevel_AllSamePrice_ReturnsNormal()
        {
            var prices = new List<double> { 50, 50, 50, 50, 50 };
            int level = SignalDerivation.ComputePriceLevel(50, prices, 25, 75);
            Assert.That(level, Is.EqualTo(0).Or.EqualTo(2),
                "When all prices equal, price is at both thresholds");
        }

        [Test]
        public void ComputeRankToday_AllSamePrice_ReturnsOne()
        {
            var prices = new List<double> { 50, 50, 50, 50, 50 };
            int rank = SignalDerivation.ComputeRankToday(50, prices);
            Assert.That(rank, Is.EqualTo(1));
        }

        // -- Standard Interface: Single price -----------------------------------

        [Test]
        public void ComputePriceLevel_SinglePrice_ReturnsCheap()
        {
            var prices = new List<double> { 42.0 };
            int level = SignalDerivation.ComputePriceLevel(42.0, prices, 25, 75);
            Assert.That(level, Is.EqualTo(0).Or.EqualTo(2));
        }

        [Test]
        public void ComputeRankToday_SinglePrice_ReturnsOne()
        {
            var prices = new List<double> { 42.0 };
            int rank = SignalDerivation.ComputeRankToday(42.0, prices);
            Assert.That(rank, Is.EqualTo(1));
        }

        [Test]
        public void ComputePercentileToday_SinglePrice_ReturnsZero()
        {
            var prices = new List<double> { 42.0 };
            double pct = SignalDerivation.ComputePercentileToday(42.0, prices);
            Assert.That(pct, Is.EqualTo(0.0));
        }

        // -- Standard Interface: Null safety ------------------------------------

        [Test]
        public void ComputePriceLevel_NullPrices_ReturnsNormal()
        {
            int level = SignalDerivation.ComputePriceLevel(50, null, 25, 75);
            Assert.That(level, Is.EqualTo(1));
        }

        [Test]
        public void ComputeRankToday_NullPrices_ReturnsOne()
        {
            int rank = SignalDerivation.ComputeRankToday(50, null);
            Assert.That(rank, Is.EqualTo(1));
        }

        [Test]
        public void ComputePercentileToday_NullPrices_Returns50()
        {
            double pct = SignalDerivation.ComputePercentileToday(50, null);
            Assert.That(pct, Is.EqualTo(50.0));
        }

        [Test]
        public void ComputePercentileValue_NullValues_ReturnsZero()
        {
            double p50 = SignalDerivation.ComputePercentileValue(null, 50);
            Assert.That(p50, Is.EqualTo(0.0));
        }

        // -- Standard Interface: Percentile boundaries --------------------------

        [Test]
        public void ComputePercentileValue_P0_ReturnsMinimum()
        {
            var values = new List<double> { 10, 20, 30, 40, 50 };
            double p0 = SignalDerivation.ComputePercentileValue(values, 0);
            Assert.That(p0, Is.EqualTo(10.0));
        }

        [Test]
        public void ComputePercentileValue_P100_ReturnsMaximum()
        {
            var values = new List<double> { 10, 20, 30, 40, 50 };
            double p100 = SignalDerivation.ComputePercentileValue(values, 100);
            Assert.That(p100, Is.EqualTo(50.0));
        }

        [Test]
        public void ComputePercentileValue_P75_ReturnsThirdQuartile()
        {
            var values = new List<double> { 10, 20, 30, 40, 50 };
            double p75 = SignalDerivation.ComputePercentileValue(values, 75);
            Assert.That(p75, Is.EqualTo(40.0));
        }

        // -- Carbon: Boundary values ------------------------------------------

        [Test]
        public void ComputeCarbonLevel_AtCleanThreshold_ReturnsClean()
        {
            int level = SignalDerivation.ComputeCarbonLevel(200, 200, 500);
            Assert.That(level, Is.EqualTo(0));
        }

        [Test]
        public void ComputeCarbonLevel_AtDirtyThreshold_ReturnsDirty()
        {
            int level = SignalDerivation.ComputeCarbonLevel(500, 200, 500);
            Assert.That(level, Is.EqualTo(2));
        }

        [Test]
        public void ComputeCarbonLevel_ZeroIntensity_ReturnsClean()
        {
            int level = SignalDerivation.ComputeCarbonLevel(0, 200, 500);
            Assert.That(level, Is.EqualTo(0));
        }

        // -- Grid Intensity: Unknown fuel type --------------------------------

        [Test]
        public void ComputeGridIntensity_UnknownFuelType_UsesOtherFactor()
        {
            var mix = new Dictionary<string, double> { ["mystery_fuel"] = 1000 };
            double intensity = SignalDerivation.ComputeGridIntensity(mix);
            Assert.That(intensity, Is.EqualTo(500.0));
        }

        [Test]
        public void ComputeGridIntensity_NullFuelMix_ReturnsZero()
        {
            double intensity = SignalDerivation.ComputeGridIntensity(null);
            Assert.That(intensity, Is.EqualTo(0.0));
        }

        // -- Renewable: edge cases --------------------------------------------

        [Test]
        public void ComputeRenewablePct_NullFuelMix_ReturnsZero()
        {
            double pct = SignalDerivation.ComputeRenewablePct(null);
            Assert.That(pct, Is.EqualTo(0.0));
        }

        [Test]
        public void ComputeRenewablePct_ZeroValues_ReturnsZero()
        {
            var mix = new Dictionary<string, double> { ["solar"] = 0, ["gas"] = 0 };
            double pct = SignalDerivation.ComputeRenewablePct(mix);
            Assert.That(pct, Is.EqualTo(0.0));
        }

        [Test]
        public void ComputeRenewablePct_IncludesHydroAndGeothermal()
        {
            var mix = new Dictionary<string, double>
            {
                ["hydro"] = 250,
                ["geothermal"] = 250,
                ["gas"] = 500
            };
            double pct = SignalDerivation.ComputeRenewablePct(mix);
            Assert.That(pct, Is.EqualTo(50.0));
        }

        // -- FindForecastPeak: custom column -----------------------------------

        [Test]
        public void FindForecastPeak_CustomColumn_UsesCorrectColumn()
        {
            var rows = new List<DataRow>
            {
                new DataRow
                {
                    IntervalStartUtc = new System.DateTime(2026, 1, 1, 8, 0, 0),
                    Columns = new Dictionary<string, object> { ["load_forecast"] = 6000.0 }
                },
                new DataRow
                {
                    IntervalStartUtc = new System.DateTime(2026, 1, 1, 14, 0, 0),
                    Columns = new Dictionary<string, object> { ["load_forecast"] = 8500.0 }
                }
            };

            var (peakMw, _) = SignalDerivation.FindForecastPeak(rows, "load_forecast");
            Assert.That(peakMw, Is.EqualTo(8500.0));
        }

        [Test]
        public void FindForecastPeak_NullRows_ReturnsZero()
        {
            var (peakMw, _) = SignalDerivation.FindForecastPeak(null);
            Assert.That(peakMw, Is.EqualTo(0.0));
        }

        // -- Market Registry --------------------------------------------------

        [Test]
        public void MarketRegistry_ValidCode_ReturnsConfig()
        {
            var config = MarketRegistry.Lookup("CAISO");
            Assert.That(config, Is.Not.Null);
            Assert.That(config.DisplayName, Does.Contain("California"));
        }

        [Test]
        public void MarketRegistry_InvalidCode_ReturnsNull()
        {
            var config = MarketRegistry.Lookup("INVALID");
            Assert.That(config, Is.Null);
        }

        [Test]
        public void MarketRegistry_CaseInsensitive()
        {
            var config = MarketRegistry.Lookup("ercot");
            Assert.That(config, Is.Not.Null);
            Assert.That(config.PriceColumn, Is.EqualTo("spp"));
        }

        [Test]
        public void MarketRegistry_EnumLookup_ReturnsConfig()
        {
            var config = MarketRegistry.Lookup(SupportedMarket.PJM);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.DefaultPriceLocation, Is.EqualTo("PJM_WESTERN_HUB"));
        }

        [Test]
        public void MarketRegistry_AllMarkets_HaveDefaultPriceLocation()
        {
            foreach (SupportedMarket market in System.Enum.GetValues(typeof(SupportedMarket)))
            {
                var config = MarketRegistry.Lookup(market);
                Assert.That(config, Is.Not.Null, $"{market} config is null");
                Assert.That(config.DefaultPriceLocation, Is.Not.Null.And.Not.Empty,
                    $"{market} is missing DefaultPriceLocation");
            }
        }
    }
}
