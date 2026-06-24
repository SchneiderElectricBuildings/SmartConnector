using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Mongoose.Common;
using Mongoose.Common.Attributes;
using Mongoose.Ews.Server.Data;
using Mongoose.Process;
using SxL.Common;

namespace SmartConnector.WeatherExtension
{
    /// <summary>
    /// The OpenWeatherMap measurement system. Maps to the API's `units`
    /// query parameter and determines the unit labels on EWS value items.
    /// </summary>
    public enum WeatherUnits
    {
        Metric,    // °C, m/s
        Imperial,  // °F, mph
        Standard   // K, m/s
    }

    public abstract class WeatherProcessorBase : Processor
    {
        // Disable licensing for the sample extension.
        // See Developer Guide Section 10 to enable licensing.
        public override bool IsLicensed => false;

        // -- EWS Server Properties ------------------------------------------

        [Required]
        [DefaultValue("Weather Service")]
        [Tooltip("Name of the EWS Server to create or connect to.")]
        public string ServerName { get; set; }

        [Required]
        [Tooltip("Username for the EWS Server.")]
        public string UserName { get; set; }

        [Required]
        [EncryptedString]
        [Tooltip("Password for the EWS Server. Stored encrypted in the database.")]
        public string Password { get; set; }

        // -- OpenWeatherMap Configuration -----------------------------------

        [Required]
        [EncryptedString]
        [Tooltip("OpenWeatherMap API key. Obtain a free key at "
            + "https://home.openweathermap.org/api_keys. Stored encrypted.")]
        public string ApiKey { get; set; }

        [Required]
        [DefaultValue("London,GB")]
        [Tooltip("City to report on, as \"City\", \"City,CountryCode\", or "
            + "\"City,StateCode,CountryCode\" (US). Resolved to coordinates via "
            + "the OpenWeatherMap Geocoding API.")]
        public string City { get; set; }

        [Required]
        [DefaultValue(WeatherUnits.Metric)]
        [Tooltip("Measurement system: Metric (°C, m/s), Imperial (°F, mph), "
            + "or Standard (Kelvin, m/s).")]
        public WeatherUnits Units { get; set; }

        [DefaultValue(true)]
        [Tooltip("Enable fetching and publishing the 3-day forecast.")]
        public bool UpdateForecast { get; set; }

        // -- Unit Labels ----------------------------------------------------

        protected string TemperatureUnit
        {
            get
            {
                switch (Units)
                {
                    case WeatherUnits.Imperial: return "°F";
                    case WeatherUnits.Standard: return "K";
                    default: return "°C";
                }
            }
        }

        protected string WindSpeedUnit =>
            Units == WeatherUnits.Imperial ? "mph" : "m/s";

        protected string UnitsApiValue
        {
            get
            {
                switch (Units)
                {
                    case WeatherUnits.Imperial: return "imperial";
                    case WeatherUnits.Standard: return "standard";
                    default: return "metric";
                }
            }
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

            if (string.IsNullOrWhiteSpace(City))
                yield return new ValidationResult("City is required.");
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
