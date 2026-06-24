// ---------------------------------------------------------------------------
// EWS Data Structure -- Per-ISO AlternateId Helpers
//
// Provides per-ISO EWS path generation for the Grid Status extension.
// Path structure: /Grid/{ISO}/...
//
// Includes a root-level /Grid/Meta container for observability metrics
// so consumers can detect integration health without polling each ISO.
// ---------------------------------------------------------------------------

namespace SmartConnector.GridStatusExtension
{
    /// <summary>
    /// Root-level EWS path constants and factory for per-ISO structures.
    /// </summary>
    public static class EwsStructure
    {
        public const string Root = "/Grid";

        // Root-level observability (not per-ISO)
        public const string MetaLastUpdate = "/Grid/Meta/LastUpdate";
        public const string MetaNextUpdate = "/Grid/Meta/NextUpdate";
        public const string MetaDataHealthy = "/Grid/Meta/DataHealthy";
        public const string MetaLastError = "/Grid/Meta/LastError";
        public const string MetaCycleCount = "/Grid/Meta/CycleCount";
        public const string MetaRowsConsumed = "/Grid/Meta/RowsConsumed";
        public const string MetaTimeZone = "/Grid/Meta/TimeZone";

        /// <summary>
        /// Returns an <see cref="IsoStructure"/> with all AlternateIds
        /// scoped to the given ISO code.
        /// </summary>
        public static IsoStructure ForIso(string isoCode) =>
            new IsoStructure(isoCode);
    }

    /// <summary>
    /// AlternateId paths for a single ISO's data hierarchy.
    /// Path format: /Grid/{ISO}/...
    /// </summary>
    public sealed class IsoStructure
    {
        private readonly string _prefix;

        public IsoStructure(string isoCode)
        {
            IsoCode = isoCode;
            _prefix = $"{EwsStructure.Root}/{isoCode}";
        }

        public string IsoCode { get; }
        public string IsoRoot => _prefix;

        // -- Pricing ----------------------------------------------------------
        public string PriceRealTimeLmp => $"{_prefix}/Price/RealTimeLMP";
        public string PriceCurrentPrice => $"{_prefix}/Price/CurrentPrice";
        public string PriceNextHour => $"{_prefix}/Price/NextHour";
        public string PriceLevel => $"{_prefix}/Price/Level";
        public string PricePriceCategory => $"{_prefix}/Price/PriceCategory";
        public string PriceRankToday => $"{_prefix}/Price/RankToday";
        public string PricePercentileToday => $"{_prefix}/Price/PercentileToday";
        public string PriceNegativeActive => $"{_prefix}/Price/NegativeActive";
        public string PriceCheapThreshold => $"{_prefix}/Price/CheapThreshold";
        public string PriceExpensiveThreshold => $"{_prefix}/Price/ExpensiveThreshold";
        public string PriceCurrency => $"{_prefix}/Price/Currency";
        public string PriceUnit => $"{_prefix}/Price/Unit";
        public string PriceDayMinimum => $"{_prefix}/Price/DayMinimum";
        public string PriceDayMaximum => $"{_prefix}/Price/DayMaximum";
        public string PriceDayAverage => $"{_prefix}/Price/DayAverage";

        // -- Hourly Prices ----------------------------------------------------
        public string HourlyPrices => $"{_prefix}/Price/Hourly";

        public string HourlyPriceId(int hour) =>
            $"{_prefix}/Price/Hourly/Hour{hour:D2}";

        // -- Load shifting ----------------------------------------------------
        public string LoadShiftingRoot => $"{_prefix}/LoadShifting";
        public string ShiftIsCurrentlyCheap => $"{_prefix}/LoadShifting/IsCurrentlyCheap";
        public string ShiftIsCurrentlyExpensive => $"{_prefix}/LoadShifting/IsCurrentlyExpensive";
        public string ShiftHoursUntilCheap => $"{_prefix}/LoadShifting/HoursUntilCheap";
        public string ShiftHoursUntilExpensive => $"{_prefix}/LoadShifting/HoursUntilExpensive";
        public string ShiftCheapHoursToday => $"{_prefix}/LoadShifting/CheapHoursToday";
        public string ShiftExpensiveHoursToday => $"{_prefix}/LoadShifting/ExpensiveHoursToday";
        public string ShiftRenewablePercent => $"{_prefix}/LoadShifting/RenewablePercent";

        // -- Demand -----------------------------------------------------------
        public string LoadCurrent => $"{_prefix}/Load/Current";
        public string LoadForecastPeakToday => $"{_prefix}/Load/ForecastPeakToday";
        public string LoadForecastPeakHour => $"{_prefix}/Load/ForecastPeakHour";
        public string LoadPeakRiskLevel => $"{_prefix}/Load/PeakRiskLevel";

        // -- Carbon and fuel mix ----------------------------------------------
        public string FuelMixRenewablePct => $"{_prefix}/FuelMix/RenewablePct";
        public string CarbonGridIntensity => $"{_prefix}/Carbon/GridIntensity";
        public string CarbonLevel => $"{_prefix}/Carbon/Level";
        public string FuelMixSolar => $"{_prefix}/FuelMix/Solar";
        public string FuelMixWind => $"{_prefix}/FuelMix/Wind";
        public string FuelMixGas => $"{_prefix}/FuelMix/Gas";

        // -- Per-ISO metadata -------------------------------------------------
        public string MetaLastUpdate => $"{_prefix}/Meta/LastUpdate";
        public string MetaNextUpdate => $"{_prefix}/Meta/NextUpdate";
        public string MetaDataHealthy => $"{_prefix}/Meta/DataHealthy";
        public string MetaTimeZone => $"{_prefix}/Meta/TimeZone";

        // -- Trend logs -------------------------------------------------------
        public string TrendRealTimeLmp => $"{_prefix}/Price/RealTimeLMPHistory";
        public string TrendCurrentPrice => $"{_prefix}/Price/CurrentPriceHistory";
        public string TrendLoadCurrent => $"{_prefix}/Load/CurrentHistory";
        public string TrendFuelMixRenewablePct => $"{_prefix}/FuelMix/RenewablePctHistory";
        public string TrendCarbonGridIntensity => $"{_prefix}/Carbon/GridIntensityHistory";
        public string TrendFuelMixSolar => $"{_prefix}/FuelMix/SolarHistory";
        public string TrendFuelMixWind => $"{_prefix}/FuelMix/WindHistory";
        public string TrendFuelMixGas => $"{_prefix}/FuelMix/GasHistory";
        public string TrendDayMinimum => $"{_prefix}/Price/DayMinimumHistory";
        public string TrendDayMaximum => $"{_prefix}/Price/DayMaximumHistory";
        public string TrendDayAverage => $"{_prefix}/Price/DayAverageHistory";
        public string TrendIsCurrentlyCheap => $"{_prefix}/LoadShifting/IsCurrentlyCheapHistory";
        public string TrendIsCurrentlyExpensive => $"{_prefix}/LoadShifting/IsCurrentlyExpensiveHistory";
        public string TrendShiftRenewablePercent => $"{_prefix}/LoadShifting/RenewablePercentHistory";

        /// <summary>
        /// All trend log definitions paired with the value they track.
        /// </summary>
        public (string TrendId, string ValueId, string Name)[] TrendDefinitions =>
            new[]
            {
                (TrendRealTimeLmp,              PriceRealTimeLmp,           "Real-Time LMP History"),
                (TrendCurrentPrice,             PriceCurrentPrice,          "Current Price History"),
                (TrendDayMinimum,               PriceDayMinimum,            "Day Minimum History"),
                (TrendDayMaximum,               PriceDayMaximum,            "Day Maximum History"),
                (TrendDayAverage,               PriceDayAverage,            "Day Average History"),
                (TrendLoadCurrent,              LoadCurrent,                "Current Load History"),
                (TrendFuelMixRenewablePct,      FuelMixRenewablePct,        "Renewable Pct History"),
                (TrendCarbonGridIntensity,      CarbonGridIntensity,        "Grid Intensity History"),
                (TrendFuelMixSolar,             FuelMixSolar,               "Solar MW History"),
                (TrendFuelMixWind,              FuelMixWind,                "Wind MW History"),
                (TrendFuelMixGas,               FuelMixGas,                 "Gas MW History"),
                (TrendIsCurrentlyCheap,         ShiftIsCurrentlyCheap,      "Is Currently Cheap History"),
                (TrendIsCurrentlyExpensive,     ShiftIsCurrentlyExpensive,  "Is Currently Expensive History"),
                (TrendShiftRenewablePercent,    ShiftRenewablePercent,      "Renewable Percent History"),
            };
    }
}
