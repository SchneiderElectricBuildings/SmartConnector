namespace SmartConnector.WeatherExtension
{
    /// <summary>
    /// EWS AlternateId paths for the single-city weather tree, plus the
    /// trend-log definitions. The extension serves one configured city, so
    /// the structure is a flat set of constants under /Weather.
    /// </summary>
    public static class EwsStructure
    {
        public const string Root = "/Weather";

        // ── Root observability ──────────────────────────────────────────
        public const string Meta = "/Weather/Meta";
        public const string MetaLastUpdate = "/Weather/Meta/LastUpdate";
        public const string MetaNextUpdate = "/Weather/Meta/NextUpdate";
        public const string MetaDataHealthy = "/Weather/Meta/DataHealthy";
        public const string MetaLastError = "/Weather/Meta/LastError";
        public const string MetaCycleCount = "/Weather/Meta/CycleCount";
        public const string MetaApiCalls = "/Weather/Meta/ApiCallsTotal";
        public const string MetaTimeZone = "/Weather/Meta/TimeZone";

        // ── Location ────────────────────────────────────────────────────
        public const string Location = "/Weather/Location";
        public const string LocationConfiguredCity = "/Weather/Location/ConfiguredCity";
        public const string LocationResolvedName = "/Weather/Location/ResolvedName";
        public const string LocationCountry = "/Weather/Location/Country";
        public const string LocationLatitude = "/Weather/Location/Latitude";
        public const string LocationLongitude = "/Weather/Location/Longitude";

        // ── Current conditions ──────────────────────────────────────────
        public const string Current = "/Weather/Current";
        public const string CurrentTemperature = "/Weather/Current/Temperature";
        public const string CurrentTemperatureHistory = "/Weather/Current/TemperatureHistory";
        public const string CurrentFeelsLike = "/Weather/Current/FeelsLike";
        public const string CurrentPressure = "/Weather/Current/Pressure";
        public const string CurrentPressureHistory = "/Weather/Current/PressureHistory";
        public const string CurrentHumidity = "/Weather/Current/Humidity";
        public const string CurrentHumidityHistory = "/Weather/Current/HumidityHistory";
        public const string CurrentWindSpeed = "/Weather/Current/WindSpeed";
        public const string CurrentDescription = "/Weather/Current/Description";
        public const string CurrentObservedAt = "/Weather/Current/ObservedAt";

        // ── Forecast (Day1..Day3) ───────────────────────────────────────
        public const string Forecast = "/Weather/Forecast";

        public static string ForecastDay(int day) => $"/Weather/Forecast/Day{day}";
        public static string ForecastDate(int day) => $"/Weather/Forecast/Day{day}/Date";
        public static string ForecastHighTemp(int day) => $"/Weather/Forecast/Day{day}/HighTemp";
        public static string ForecastLowTemp(int day) => $"/Weather/Forecast/Day{day}/LowTemp";
        public static string ForecastPressure(int day) => $"/Weather/Forecast/Day{day}/Pressure";
        public static string ForecastHumidity(int day) => $"/Weather/Forecast/Day{day}/Humidity";
        public static string ForecastDescription(int day) => $"/Weather/Forecast/Day{day}/Description";

        /// <summary>Number of forecast days provisioned and published.</summary>
        public const int ForecastDays = 3;

        // ── Trend logs ──────────────────────────────────────────────────
        // Current conditions are trended via per-cycle sampling. OpenWeatherMap's
        // free tier has no historical endpoint, so there is no backfill — trends
        // accumulate from the first run forward.
        public static (string TrendId, string ValueId, string Name)[] TrendDefinitions =>
            new[]
            {
                (CurrentTemperatureHistory, CurrentTemperature, "Temperature History"),
                (CurrentPressureHistory,    CurrentPressure,    "Pressure History"),
                (CurrentHumidityHistory,    CurrentHumidity,    "Humidity History"),
            };
    }
}
