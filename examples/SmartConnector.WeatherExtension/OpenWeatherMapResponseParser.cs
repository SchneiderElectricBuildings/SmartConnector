using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace SmartConnector.WeatherExtension
{
    /// <summary>A resolved location from the Geocoding API.</summary>
    public sealed class GeocodeResult
    {
        public string Name { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    /// <summary>Current conditions from the Current Weather API.</summary>
    public sealed class CurrentWeather
    {
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double Pressure { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; }
        public DateTime ObservedAtUtc { get; set; }
    }

    /// <summary>A single 3-hour forecast step.</summary>
    public sealed class ForecastPoint
    {
        public DateTime TimestampUtc { get; set; }
        public double Temp { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public double Pressure { get; set; }
        public double Humidity { get; set; }
        public string Description { get; set; }
    }

    /// <summary>Parsed 5 day / 3 hour forecast.</summary>
    public sealed class ForecastResponse
    {
        public List<ForecastPoint> Points { get; set; } = new List<ForecastPoint>();
        public int TimezoneOffsetSeconds { get; set; }
    }

    /// <summary>
    /// Parses OpenWeatherMap JSON responses using JavaScriptSerializer
    /// (available in .NET Framework 4.8, no external dependency).
    /// </summary>
    public static class OpenWeatherMapResponseParser
    {
        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 };

        /// <summary>
        /// Parses a Geocoding response array and returns the first match,
        /// or null if the array is empty.
        /// </summary>
        public static GeocodeResult ParseGeocode(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Geocoding response is empty.", nameof(json));

            var array = Serializer.Deserialize<object>(json) as IEnumerable;
            if (array == null) return null;

            foreach (var item in array)
            {
                var dict = item as IDictionary<string, object>;
                if (dict == null) continue;

                return new GeocodeResult
                {
                    Name = GetString(dict, "name"),
                    Country = GetString(dict, "country"),
                    State = GetString(dict, "state"),
                    Lat = GetDouble(dict, "lat"),
                    Lon = GetDouble(dict, "lon")
                };
            }

            return null;
        }

        /// <summary>Parses a Current Weather response.</summary>
        public static CurrentWeather ParseCurrent(string json)
        {
            var root = Deserialize(json);

            var main = GetDict(root, "main");
            var wind = GetDict(root, "wind");

            return new CurrentWeather
            {
                Temperature = GetDouble(main, "temp"),
                FeelsLike = GetDouble(main, "feels_like"),
                Pressure = GetDouble(main, "pressure"),
                Humidity = GetDouble(main, "humidity"),
                WindSpeed = GetDouble(wind, "speed"),
                Description = FirstWeatherDescription(root),
                ObservedAtUtc = FromUnix(GetDouble(root, "dt"))
            };
        }

        /// <summary>Parses a 5 day / 3 hour Forecast response.</summary>
        public static ForecastResponse ParseForecast(string json)
        {
            var root = Deserialize(json);
            var result = new ForecastResponse();

            var city = GetDict(root, "city");
            if (city != null)
                result.TimezoneOffsetSeconds = (int)GetDouble(city, "timezone");

            var list = GetEnumerable(root, "list");
            if (list != null)
            {
                foreach (var item in list)
                {
                    var dict = item as IDictionary<string, object>;
                    if (dict == null) continue;

                    var main = GetDict(dict, "main");
                    result.Points.Add(new ForecastPoint
                    {
                        TimestampUtc = FromUnix(GetDouble(dict, "dt")),
                        Temp = GetDouble(main, "temp"),
                        TempMin = GetDouble(main, "temp_min"),
                        TempMax = GetDouble(main, "temp_max"),
                        Pressure = GetDouble(main, "pressure"),
                        Humidity = GetDouble(main, "humidity"),
                        Description = FirstWeatherDescription(dict)
                    });
                }
            }

            return result;
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static IDictionary<string, object> Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Response is empty.", nameof(json));

            var root = Serializer.Deserialize<object>(json)
                as IDictionary<string, object>;
            if (root == null)
                throw new FormatException("Unexpected OpenWeatherMap response shape.");
            return root;
        }

        private static string FirstWeatherDescription(IDictionary<string, object> dict)
        {
            var weather = GetEnumerable(dict, "weather");
            if (weather == null) return "";

            foreach (var item in weather)
            {
                var w = item as IDictionary<string, object>;
                if (w != null) return GetString(w, "description");
            }
            return "";
        }

        private static IDictionary<string, object> GetDict(
            IDictionary<string, object> dict, string key)
        {
            if (dict != null && dict.TryGetValue(key, out var val))
                return val as IDictionary<string, object>;
            return null;
        }

        private static IEnumerable GetEnumerable(
            IDictionary<string, object> dict, string key)
        {
            if (dict != null && dict.TryGetValue(key, out var val))
                return val as IEnumerable;
            return null;
        }

        private static double GetDouble(IDictionary<string, object> dict, string key)
        {
            if (dict == null || !dict.TryGetValue(key, out var val) || val == null)
                return 0.0;

            if (val is double d) return d;
            if (val is int i) return i;
            if (val is long l) return l;
            if (val is decimal m) return (double)m;

            return double.TryParse(val.ToString(),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out var parsed)
                ? parsed : 0.0;
        }

        private static string GetString(IDictionary<string, object> dict, string key)
        {
            if (dict != null && dict.TryGetValue(key, out var val) && val != null)
                return val.ToString();
            return "";
        }

        private static DateTime FromUnix(double seconds)
        {
            if (seconds <= 0) return DateTime.MinValue;
            return DateTimeOffset.FromUnixTimeSeconds((long)seconds).UtcDateTime;
        }
    }
}
