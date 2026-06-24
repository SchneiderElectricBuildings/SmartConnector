using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Mongoose.Common;
using Mongoose.Common.Attributes;
using Mongoose.Ews.Server.Data;
using Mongoose.Process;
using SxL.Common;

namespace SmartConnector.EntsoeExtension
{
    public abstract class EntsoeProcessorBase : Processor
    {
        public override bool IsLicensed => false;

        // -- EWS Server Properties ------------------------------------------

        [Required]
        [DefaultValue("ENTSOE Energy Data")]
        [Tooltip("Name of the EWS Server to create or connect to.")]
        public string ServerName { get; set; }

        [Required]
        [Tooltip("Username for the EWS Server.")]
        public string UserName { get; set; }

        [Required]
        [EncryptedString]
        [Tooltip("Password for the EWS Server. Stored encrypted in the database.")]
        public string Password { get; set; }

        // -- ENTSO-E Configuration ------------------------------------------

        [Required]
        [DefaultValue(SupportedCountry.Netherlands)]
        [Tooltip("Select the country to fetch energy data for. "
            + "Countries with multiple bidding zones (e.g. Sweden, Norway) "
            + "automatically include all zones.")]
        public SupportedCountry Country { get; set; }

        [Required]
        [EncryptedString]
        [Tooltip("ENTSO-E Transparency Platform API security token. "
            + "Register at https://transparency.entsoe.eu to obtain one.")]
        public string ApiToken { get; set; }

        // -- Load-Shifting Configuration ------------------------------------

        [Range(1, 49)]
        [DefaultValue(25)]
        [Tooltip("Percentile (1–49) below which day-ahead prices are classified as Cheap. "
            + "Example: 25 means the cheapest 25 % of hours are tagged Cheap.")]
        public int CheapPercentile { get; set; }

        [Range(51, 99)]
        [DefaultValue(75)]
        [Tooltip("Percentile (51–99) above which day-ahead prices are classified as Expensive. "
            + "Example: 75 means the most expensive 25 % of hours are tagged Expensive.")]
        public int ExpensivePercentile { get; set; }

        // -- Trend Backfill -------------------------------------------------

        [Range(0, 90)]
        [DefaultValue(30)]
        [Tooltip("Number of days of historical data to backfill into the trend "
            + "logs on first run (and after reconnection gaps). Set to 0 to "
            + "disable backfill and rely on live auto-sampling only.")]
        public int BackfillDays { get; set; }

        // -- Zone Resolution ------------------------------------------------

        protected string[] GetZoneCodes()
        {
            return AreaMapping.GetZoneCodes(Country);
        }

        protected string GetCountryDisplayName()
        {
            return AreaMapping.GetCountryDisplayName(Country);
        }

        // -- EwsServerDataAdapter Lifecycle ---------------------------------

        protected EwsServerDataAdapter _dataAdapter;

        protected EwsServerDataAdapter DataAdapter
        {
            get
            {
                if (_dataAdapter == null)
                {
                    try
                    {
                        _dataAdapter = EwsServerDataAdapter.ConnectExisting(
                            ServerName, UserName, Password);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(LogCategory.Processor,
                            $"ConnectExisting failed: {ex.GetType().Name}: {ex.Message}");
                    }
                }
                return _dataAdapter;
            }
        }

        // -- Validation -----------------------------------------------------

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

            var zoneCodes = GetZoneCodes();
            if (zoneCodes.Length == 0)
                yield return new ValidationResult(
                    $"No bidding zones configured for {Country}.");
        }

        // -- Dispose --------------------------------------------------------

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
