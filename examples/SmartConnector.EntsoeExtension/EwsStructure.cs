namespace SmartConnector.EntsoeExtension
{
    public static class EwsStructure
    {
        public const string Root = "/ENTSOE";
        public const string CountriesContainer = "/ENTSOE/Countries";

        // Root-level observability (not per-zone) — mirrors GridStatus /Grid/Meta
        public const string MetaLastUpdate = "/ENTSOE/Meta/LastUpdate";
        public const string MetaNextUpdate = "/ENTSOE/Meta/NextUpdate";
        public const string MetaDataHealthy = "/ENTSOE/Meta/DataHealthy";
        public const string MetaLastError = "/ENTSOE/Meta/LastError";
        public const string MetaCycleCount = "/ENTSOE/Meta/CycleCount";
        public const string MetaRowsConsumed = "/ENTSOE/Meta/RowsConsumed";
        public const string MetaTimeZone = "/ENTSOE/Meta/TimeZone";

        public static string CountryFolder(string countryName) =>
            $"{CountriesContainer}/{countryName}";

        public static ZoneStructure ForZone(string countryName, string zoneCode) =>
            new ZoneStructure(countryName, zoneCode);
    }

    public sealed class ZoneStructure
    {
        private readonly string _prefix;

        public ZoneStructure(string countryName, string zoneCode)
        {
            CountryName = countryName;
            ZoneCode = zoneCode;
            _prefix = $"{EwsStructure.CountriesContainer}/{countryName}/{zoneCode}";
        }

        public string CountryName { get; }
        public string ZoneCode { get; }

        // ── Root ────────────────────────────────────────────────────────
        public string CountryFolder => $"{EwsStructure.CountriesContainer}/{CountryName}";
        public string Root => _prefix;

        // ── Meta (per-zone observability) ───────────────────────────────
        public string Meta => $"{_prefix}/Meta";
        public string MetaLastUpdate => $"{_prefix}/Meta/LastUpdate";
        public string MetaCountry => $"{_prefix}/Meta/Country";
        public string MetaBiddingZone => $"{_prefix}/Meta/BiddingZone";
        public string MetaDataHealthy => $"{_prefix}/Meta/DataHealthy";
        public string MetaTimeZone => $"{_prefix}/Meta/TimeZone";

        // ── Pricing ─────────────────────────────────────────────────────
        public string Price => $"{_prefix}/Price";
        public string PriceCurrentPrice => $"{_prefix}/Price/CurrentPrice";
        public string PriceNextHour => $"{_prefix}/Price/NextHour";
        public string PriceCurrency => $"{_prefix}/Price/Currency";
        public string PriceUnit => $"{_prefix}/Price/Unit";
        public string PriceDayMinimum => $"{_prefix}/Price/DayMinimum";
        public string PriceDayMaximum => $"{_prefix}/Price/DayMaximum";
        public string PriceDayAverage => $"{_prefix}/Price/DayAverage";
        public string PriceLevel => $"{_prefix}/Price/Level";
        public string PricePriceCategory => $"{_prefix}/Price/PriceCategory";
        public string PriceCheapThreshold => $"{_prefix}/Price/CheapThreshold";
        public string PriceExpensiveThreshold => $"{_prefix}/Price/ExpensiveThreshold";
        public string PriceRankToday => $"{_prefix}/Price/RankToday";
        public string PricePercentileToday => $"{_prefix}/Price/PercentileToday";
        public string PriceNegativeActive => $"{_prefix}/Price/NegativeActive";

        // ── Hourly Prices ───────────────────────────────────────────────
        public string HourlyPrices => $"{_prefix}/Price/Hourly";

        public string HourlyPriceId(int hour) =>
            $"{_prefix}/Price/Hourly/Hour{hour:D2}";

        // ── Load ────────────────────────────────────────────────────────
        public string Load => $"{_prefix}/Load";
        public string LoadCurrent => $"{_prefix}/Load/Current";
        public string LoadForecastPeakToday => $"{_prefix}/Load/ForecastPeakToday";
        public string LoadForecastPeakHour => $"{_prefix}/Load/ForecastPeakHour";
        public string LoadPeakRiskLevel => $"{_prefix}/Load/PeakRiskLevel";

        // ── Generation ──────────────────────────────────────────────────
        public string Generation => $"{_prefix}/Generation";
        public string GenerationForecastTotal => $"{_prefix}/Generation/ForecastTotal";
        public string GenerationWind => $"{_prefix}/Generation/Wind";
        public string GenerationSolar => $"{_prefix}/Generation/Solar";

        // ── Load-Shifting Signals ───────────────────────────────────────
        public string LoadShifting => $"{_prefix}/LoadShifting";
        public string ShiftIsCurrentlyCheap => $"{_prefix}/LoadShifting/IsCurrentlyCheap";
        public string ShiftIsCurrentlyExpensive => $"{_prefix}/LoadShifting/IsCurrentlyExpensive";
        public string ShiftHoursUntilCheap => $"{_prefix}/LoadShifting/HoursUntilCheap";
        public string ShiftHoursUntilExpensive => $"{_prefix}/LoadShifting/HoursUntilExpensive";
        public string ShiftCheapHoursToday => $"{_prefix}/LoadShifting/CheapHoursToday";
        public string ShiftExpensiveHoursToday => $"{_prefix}/LoadShifting/ExpensiveHoursToday";
        public string ShiftRenewablePercent => $"{_prefix}/LoadShifting/RenewablePercent";

        // ── Trend Logs ──────────────────────────────────────────────────
        public string TrendCurrentPrice => $"{_prefix}/Price/CurrentPriceHistory";
        public string TrendDayMinimum => $"{_prefix}/Price/DayMinimumHistory";
        public string TrendDayMaximum => $"{_prefix}/Price/DayMaximumHistory";
        public string TrendDayAverage => $"{_prefix}/Price/DayAverageHistory";
        public string TrendLoadCurrent => $"{_prefix}/Load/CurrentHistory";
        public string TrendForecastTotal => $"{_prefix}/Generation/ForecastTotalHistory";
        public string TrendWind => $"{_prefix}/Generation/WindHistory";
        public string TrendSolar => $"{_prefix}/Generation/SolarHistory";
        public string TrendIsCurrentlyCheap => $"{_prefix}/LoadShifting/IsCurrentlyCheapHistory";
        public string TrendIsCurrentlyExpensive => $"{_prefix}/LoadShifting/IsCurrentlyExpensiveHistory";
        public string TrendRenewablePercent => $"{_prefix}/LoadShifting/RenewablePercentHistory";

        // Trend names must be unique across the whole server. Because one
        // EWS Server hosts multiple zones, the country and zone code are
        // appended to each name (e.g. "Current Price History Sweden SE_1").
        private string TrendName(string baseName) =>
            $"{baseName} {CountryName} {ZoneCode}";

        public (string TrendId, string ValueId, string Name)[] TrendDefinitions =>
            new[]
            {
                (TrendCurrentPrice,         PriceCurrentPrice,         TrendName("Current Price History")),
                (TrendDayMinimum,           PriceDayMinimum,           TrendName("Day Minimum History")),
                (TrendDayMaximum,           PriceDayMaximum,           TrendName("Day Maximum History")),
                (TrendDayAverage,           PriceDayAverage,           TrendName("Day Average History")),
                (TrendLoadCurrent,          LoadCurrent,               TrendName("Current Load History")),
                (TrendForecastTotal,        GenerationForecastTotal,   TrendName("Forecast Total History")),
                (TrendWind,                 GenerationWind,            TrendName("Wind MW History")),
                (TrendSolar,                GenerationSolar,           TrendName("Solar MW History")),
                (TrendIsCurrentlyCheap,     ShiftIsCurrentlyCheap,     TrendName("Is Currently Cheap History")),
                (TrendIsCurrentlyExpensive, ShiftIsCurrentlyExpensive, TrendName("Is Currently Expensive History")),
                (TrendRenewablePercent,     ShiftRenewablePercent,     TrendName("Renewable Percent History")),
            };
    }
}
