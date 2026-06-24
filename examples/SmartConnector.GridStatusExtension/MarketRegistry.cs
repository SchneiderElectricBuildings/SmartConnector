// ---------------------------------------------------------------------------
// ISO Market Configuration
//
// Maps ISO/market identifiers to their GridStatus.io dataset IDs and
// configuration. Each market has different dataset naming conventions
// (e.g. ERCOT uses "spp" for settlement point prices while most ISOs
// use "lmp" for locational marginal prices).
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SmartConnector.GridStatusExtension
{
    /// <summary>
    /// Supported ISO/RTO market codes.
    /// The Portal renders this as a dropdown list.
    /// </summary>
    public enum SupportedMarket
    {
        CAISO,
        ERCOT,
        PJM,
        MISO,
        NYISO,
        ISONE,
        SPP,
        IESO,
        AESO
    }

    /// <summary>
    /// Configuration for a single ISO/RTO market, including dataset IDs
    /// and the default timezone.
    /// </summary>
    public sealed class MarketConfig
    {
        public string IsoId { get; }
        public string DisplayName { get; }
        public string TimeZoneId { get; }
        public string DayAheadPriceDataset { get; }
        public string RealTimePriceDataset { get; }
        public string LoadDataset { get; }
        public string LoadForecastDataset { get; }
        public string FuelMixDataset { get; }
        public string PriceFilterColumn { get; }
        public string PriceColumn { get; }
        public string DefaultPriceLocation { get; }

        public MarketConfig(
            string isoId,
            string displayName,
            string timeZoneId,
            string dayAheadPriceDataset,
            string realTimePriceDataset,
            string loadDataset,
            string loadForecastDataset,
            string fuelMixDataset,
            string priceFilterColumn,
            string priceColumn = "lmp",
            string defaultPriceLocation = null)
        {
            IsoId = isoId;
            DisplayName = displayName;
            TimeZoneId = timeZoneId;
            DayAheadPriceDataset = dayAheadPriceDataset;
            RealTimePriceDataset = realTimePriceDataset;
            LoadDataset = loadDataset;
            LoadForecastDataset = loadForecastDataset;
            FuelMixDataset = fuelMixDataset;
            PriceFilterColumn = priceFilterColumn;
            PriceColumn = priceColumn;
            DefaultPriceLocation = defaultPriceLocation;
        }

        public TimeZoneInfo GetTimeZone()
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
    }

    /// <summary>
    /// Registry of supported ISO/RTO markets and their GridStatus.io
    /// dataset mappings. Add new markets here without code changes to
    /// the processor.
    /// </summary>
    public static class MarketRegistry
    {
        private static readonly Dictionary<string, MarketConfig> Markets =
            new Dictionary<string, MarketConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["CAISO"] = new MarketConfig(
                    isoId: "CAISO",
                    displayName: "California ISO",
                    timeZoneId: "Pacific Standard Time",
                    dayAheadPriceDataset: "caiso_lmp_day_ahead_hourly",
                    realTimePriceDataset: "caiso_lmp_real_time_5_min",
                    loadDataset: "caiso_load",
                    loadForecastDataset: "caiso_load_forecast",
                    fuelMixDataset: "caiso_fuel_mix",
                    priceFilterColumn: "location",
                    defaultPriceLocation: "TH_NP15_GEN-APND"),

                ["ERCOT"] = new MarketConfig(
                    isoId: "ERCOT",
                    displayName: "Electric Reliability Council of Texas",
                    timeZoneId: "Central Standard Time",
                    dayAheadPriceDataset: "ercot_spp_day_ahead_hourly",
                    realTimePriceDataset: "ercot_spp_real_time_15_min",
                    loadDataset: "ercot_load",
                    loadForecastDataset: "ercot_load_forecast",
                    fuelMixDataset: "ercot_fuel_mix",
                    priceFilterColumn: "location",
                    priceColumn: "spp",
                    defaultPriceLocation: "HB_HOUSTON"),

                ["PJM"] = new MarketConfig(
                    isoId: "PJM",
                    displayName: "PJM Interconnection",
                    timeZoneId: "Eastern Standard Time",
                    dayAheadPriceDataset: "pjm_lmp_day_ahead_hourly",
                    realTimePriceDataset: "pjm_lmp_real_time_5_min",
                    loadDataset: "pjm_load",
                    loadForecastDataset: "pjm_load_forecast",
                    fuelMixDataset: "pjm_fuel_mix",
                    priceFilterColumn: "location",
                    defaultPriceLocation: "PJM_WESTERN_HUB"),

                ["MISO"] = new MarketConfig(
                    isoId: "MISO",
                    displayName: "Midcontinent ISO",
                    timeZoneId: "Central Standard Time",
                    dayAheadPriceDataset: "miso_lmp_day_ahead_hourly",
                    realTimePriceDataset: "miso_lmp_real_time_5_min",
                    loadDataset: "miso_load",
                    loadForecastDataset: "miso_load_forecast",
                    fuelMixDataset: "miso_fuel_mix",
                    priceFilterColumn: "location",
                    defaultPriceLocation: "MISO.INDIANA_HUB"),

                ["NYISO"] = new MarketConfig(
                    isoId: "NYISO",
                    displayName: "New York ISO",
                    timeZoneId: "Eastern Standard Time",
                    dayAheadPriceDataset: "nyiso_lmp_day_ahead_5_min",
                    realTimePriceDataset: "nyiso_lmp_real_time_5_min",
                    loadDataset: "nyiso_load",
                    loadForecastDataset: "nyiso_load_forecast",
                    fuelMixDataset: "nyiso_fuel_mix",
                    priceFilterColumn: "location",
                    defaultPriceLocation: "N.Y.C."),

                ["ISONE"] = new MarketConfig(
                    isoId: "ISONE",
                    displayName: "ISO New England",
                    timeZoneId: "Eastern Standard Time",
                    dayAheadPriceDataset: "isone_lmp_day_ahead_hourly",
                    realTimePriceDataset: "isone_lmp_real_time_5_min",
                    loadDataset: "isone_load",
                    loadForecastDataset: "isone_load_forecast",
                    fuelMixDataset: "isone_fuel_mix",
                    priceFilterColumn: "location",
                    defaultPriceLocation: ".H.INTERNAL_HUB"),

                ["SPP"] = new MarketConfig(
                    isoId: "SPP",
                    displayName: "Southwest Power Pool",
                    timeZoneId: "Central Standard Time",
                    dayAheadPriceDataset: "spp_lmp_day_ahead_hourly",
                    realTimePriceDataset: "spp_lmp_real_time_5_min",
                    loadDataset: "spp_load",
                    loadForecastDataset: "spp_load_forecast",
                    fuelMixDataset: "spp_fuel_mix",
                    priceFilterColumn: "location",
                    defaultPriceLocation: "SPS"),

                ["IESO"] = new MarketConfig(
                    isoId: "IESO",
                    displayName: "Independent Electricity System Operator (Ontario)",
                    timeZoneId: "Eastern Standard Time",
                    dayAheadPriceDataset: "ieso_lmp_day_ahead_hourly",
                    realTimePriceDataset: "ieso_lmp_real_time_5_min",
                    loadDataset: "ieso_load",
                    loadForecastDataset: "ieso_load_forecast",
                    fuelMixDataset: "ieso_fuel_mix",
                    priceFilterColumn: "location",
                    defaultPriceLocation: "ONTARIO"),

                ["AESO"] = new MarketConfig(
                    isoId: "AESO",
                    displayName: "Alberta Electric System Operator",
                    timeZoneId: "Mountain Standard Time",
                    dayAheadPriceDataset: "aeso_lmp_day_ahead_hourly",
                    realTimePriceDataset: "aeso_lmp_real_time_5_min",
                    loadDataset: "aeso_load",
                    loadForecastDataset: "aeso_load_forecast",
                    fuelMixDataset: "aeso_fuel_mix",
                    priceFilterColumn: "location",
                    defaultPriceLocation: "ALBERTA"),
            };

        /// <summary>
        /// Returns the market configuration for the given ISO code,
        /// or null if the code is not recognised.
        /// </summary>
        public static MarketConfig Lookup(string isoCode)
        {
            if (string.IsNullOrWhiteSpace(isoCode))
                return null;

            Markets.TryGetValue(isoCode.Trim(), out var config);
            return config;
        }

        /// <summary>
        /// Returns the market configuration for the given enum value.
        /// </summary>
        public static MarketConfig Lookup(SupportedMarket market)
        {
            return Lookup(market.ToString());
        }

        /// <summary>
        /// Returns all supported ISO codes.
        /// </summary>
        public static IEnumerable<string> SupportedCodes => Markets.Keys;
    }
}
