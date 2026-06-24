// ---------------------------------------------------------------------------
// Grid Status Processor Base Class
//
// Shared properties, validation, and EwsServerDataAdapter lifecycle.
// Follows the same pattern as EntsoeProcessorBase.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Mongoose.Common;
using Mongoose.Common.Attributes;
using Mongoose.Ews.Server.Data;
using Mongoose.Process;
using SxL.Common;

namespace SmartConnector.GridStatusExtension
{
    /// <summary>
    /// Base class for Grid Status processors. Provides shared configurable
    /// properties, EWS server adapter lifecycle, and validation.
    /// </summary>
    public abstract class GridStatusProcessorBase : Processor
    {
        // Disable licensing for the sample extension.
        // See Developer Guide Section 10 to enable licensing.
        public override bool IsLicensed => false;

        // -- EWS Server Properties -------------------------------------------

        [Required]
        [DefaultValue("Grid Status")]
        [Tooltip("Name of the EWS Server to create or connect to.")]
        public string ServerName { get; set; }

        [Required]
        [Tooltip("Username for the EWS Server.")]
        public string UserName { get; set; }

        [Required]
        [EncryptedString]
        [Tooltip("Password for the EWS Server. Stored encrypted in the database.")]
        public string Password { get; set; }

        // -- GridStatus.io Configuration -------------------------------------

        [Required]
        [Tooltip("ISO/RTO market to query. Determines datasets and default "
            + "pricing location.")]
        public SupportedMarket Market { get; set; }

        [Required]
        [EncryptedString]
        [Tooltip("GridStatus.io API key. Obtain one at "
            + "https://www.gridstatus.io/settings/api. "
            + "Stored encrypted in the database.")]
        public string ApiKey { get; set; }

        [DefaultValue("https://api.gridstatus.io/v1")]
        [Tooltip("GridStatus.io API base URL.")]
        public string ApiBaseUrl { get; set; }

        [Tooltip("Optional pricing node override. Leave empty to use the "
            + "market's default trading hub. Examples: HB_HOUSTON (ERCOT), "
            + "TH_NP15_GEN-APND (CAISO).")]
        public string PriceLocation { get; set; }

        // -- Row Budget Configuration ----------------------------------------

        [Range(10, 10000)]
        [DefaultValue(100)]
        [Tooltip("Maximum rows per API request. Lower values conserve row budget "
            + "on free-tier plans. Minimum: 10.")]
        public int RowLimit { get; set; }

        // -- Trend Backfill --------------------------------------------------

        [Range(0, 90)]
        [DefaultValue(30)]
        [Tooltip("Number of days of historical data to backfill into the trend "
            + "logs on first run (and after reconnection gaps). Set to 0 to "
            + "disable backfill and rely on live per-cycle sampling only.")]
        public int BackfillDays { get; set; }

        // -- Threshold Configuration -----------------------------------------

        [Range(1, 49)]
        [DefaultValue(25)]
        [Tooltip("Percentile (1-49) below which day-ahead prices are Cheap.")]
        public int CheapPercentile { get; set; }

        [Range(51, 99)]
        [DefaultValue(75)]
        [Tooltip("Percentile (51-99) above which day-ahead prices are Expensive.")]
        public int ExpensivePercentile { get; set; }

        [Range(0.5, 1.0)]
        [DefaultValue(0.85)]
        [Tooltip("Load fraction of forecast peak for medium peak-risk (0.5-1.0).")]
        public double PeakRiskMediumThreshold { get; set; }

        [Range(0.5, 1.0)]
        [DefaultValue(0.95)]
        [Tooltip("Load fraction of forecast peak for high peak-risk (0.5-1.0).")]
        public double PeakRiskHighThreshold { get; set; }

        [Range(0, 500)]
        [DefaultValue(200)]
        [Tooltip("Grid intensity (gCO2/kWh) below which carbon level is Clean.")]
        public double CarbonCleanThreshold { get; set; }

        [Range(100, 1500)]
        [DefaultValue(500)]
        [Tooltip("Grid intensity (gCO2/kWh) above which carbon level is Dirty.")]
        public double CarbonDirtyThreshold { get; set; }

        // -- Feature Flags ---------------------------------------------------

        [DefaultValue(true)]
        [Tooltip("Enable fetching and publishing real-time and day-ahead price data.")]
        public bool UpdatePrices { get; set; }

        [DefaultValue(true)]
        [Tooltip("Enable fetching and publishing load data.")]
        public bool UpdateLoad { get; set; }

        [DefaultValue(true)]
        [Tooltip("Enable fetching and publishing fuel mix and carbon data.")]
        public bool UpdateFuelMix { get; set; }

        // -- EwsServerDataAdapter Lifecycle ----------------------------------

        protected EwsServerDataAdapter _dataAdapter;

        /// <summary>
        /// Server name with market suffix for uniqueness when multiple
        /// instances target different ISOs on the same SmartConnector.
        /// </summary>
        protected string EffectiveServerName => $"{ServerName} {Market}";

        /// <summary>
        /// Lazily connects to the existing EWS Server.
        /// Returns null if the server does not exist.
        /// </summary>
        protected EwsServerDataAdapter DataAdapter
        {
            get
            {
                if (_dataAdapter == null)
                {
                    try
                    {
                        _dataAdapter = EwsServerDataAdapter.ConnectExisting(
                            EffectiveServerName, UserName, Password);
                    }
                    catch (Exception ex)
                    {
                        // Server does not exist yet — return null so the
                        // processor can fall through to ConnectNew.
                        Logger.LogDebug(LogCategory.Processor,
                            $"ConnectExisting failed: {ex.GetType().Name}: {ex.Message}");
                    }
                }
                return _dataAdapter;
            }
        }

        // -- Validation ------------------------------------------------------

        public override IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            foreach (var result in base.Validate(validationContext))
                yield return result;

            if (string.Equals(UserName, Password, StringComparison.Ordinal))
                yield return new ValidationResult(
                    "UserName and Password must not be identical.");

            if (CheapPercentile >= ExpensivePercentile)
                yield return new ValidationResult(
                    "CheapPercentile must be less than ExpensivePercentile.");

            if (PeakRiskMediumThreshold >= PeakRiskHighThreshold)
                yield return new ValidationResult(
                    "PeakRiskMediumThreshold must be less than PeakRiskHighThreshold.");

            if (CarbonCleanThreshold >= CarbonDirtyThreshold)
                yield return new ValidationResult(
                    "CarbonCleanThreshold must be less than CarbonDirtyThreshold.");
        }

        /// <summary>
        /// Returns the effective price location: the user-specified override,
        /// or the market's default trading hub if left empty.
        /// </summary>
        protected string GetEffectivePriceLocation(MarketConfig market)
        {
            if (!string.IsNullOrWhiteSpace(PriceLocation))
                return PriceLocation.Trim();

            return market.DefaultPriceLocation;
        }

        // -- Dispose ---------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dataAdapter?.Dispose();
                _dataAdapter = null;
            }
            base.Dispose(disposing);
        }
    }
}
