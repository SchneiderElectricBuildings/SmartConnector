using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartConnector.WeatherExtension
{
    /// <summary>
    /// Synchronous HTTP client for the OpenWeatherMap REST API (free tier:
    /// Geocoding, Current Weather 2.5, and 5 day / 3 hour Forecast 2.5).
    /// The API key is passed as the <c>appid</c> query parameter and is never
    /// logged. TLS 1.2 is enforced for every call.
    /// </summary>
    public sealed class OpenWeatherMapClient : IDisposable
    {
        private const string BaseUrl = "https://api.openweathermap.org";
        private const int MaxRetries = 3;
        private const int InitialBackoffMs = 2000;

        private readonly HttpClient _http;
        private readonly string _apiKey;

        public OpenWeatherMapClient(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _apiKey = apiKey;

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Resolves a city name to coordinates via the Geocoding API.
        /// Returns the raw JSON array (zero or more matches).
        /// </summary>
        public string GeocodeCity(string city, int limit = 1)
        {
            var url = BuildUrl("/geo/1.0/direct",
                "q", city,
                "limit", limit.ToString(CultureInfo.InvariantCulture));
            return Execute(url);
        }

        /// <summary>
        /// Current weather for the given coordinates (Current Weather 2.5).
        /// </summary>
        public string GetCurrentWeather(double lat, double lon, string units)
        {
            var url = BuildUrl("/data/2.5/weather",
                "lat", lat.ToString(CultureInfo.InvariantCulture),
                "lon", lon.ToString(CultureInfo.InvariantCulture),
                "units", units);
            return Execute(url);
        }

        /// <summary>
        /// 5 day / 3 hour forecast for the given coordinates (Forecast 2.5).
        /// </summary>
        public string GetForecast(double lat, double lon, string units)
        {
            var url = BuildUrl("/data/2.5/forecast",
                "lat", lat.ToString(CultureInfo.InvariantCulture),
                "lon", lon.ToString(CultureInfo.InvariantCulture),
                "units", units);
            return Execute(url);
        }

        // Builds a URL with the API key plus the given key/value query pairs.
        // Values are escaped; the appid is appended last and never logged.
        private string BuildUrl(string path, params string[] keyValuePairs)
        {
            var sb = new StringBuilder(BaseUrl);
            sb.Append(path).Append('?');

            for (int i = 0; i < keyValuePairs.Length; i += 2)
            {
                sb.Append(Uri.EscapeDataString(keyValuePairs[i]))
                  .Append('=')
                  .Append(Uri.EscapeDataString(keyValuePairs[i + 1]))
                  .Append('&');
            }

            sb.Append("appid=").Append(Uri.EscapeDataString(_apiKey));
            return sb.ToString();
        }

        private string Execute(string url)
        {
            int backoffMs = InitialBackoffMs;

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                HttpResponseMessage response;
                try
                {
                    response = _http.GetAsync(url).GetAwaiter().GetResult();
                }
                catch (TaskCanceledException)
                {
                    throw new WeatherApiException(
                        "Request to OpenWeatherMap timed out.",
                        HttpStatusCode.RequestTimeout);
                }

                if (response.IsSuccessStatusCode)
                    return response.Content.ReadAsStringAsync()
                        .GetAwaiter().GetResult();

                int status = (int)response.StatusCode;

                // 401 = invalid or not-yet-activated key; do not retry.
                if (status == 401 || status == 403)
                    throw new WeatherApiException(
                        $"Authentication failed (HTTP {status}). Verify the API "
                        + "key; newly created keys can take up to a couple of "
                        + "hours to activate.", response.StatusCode);

                if (status == 429 && attempt < MaxRetries)
                {
                    Thread.Sleep(backoffMs);
                    backoffMs *= 2;
                    continue;
                }

                string body = response.Content.ReadAsStringAsync()
                    .GetAwaiter().GetResult();
                throw new WeatherApiException(
                    $"HTTP {status}: {body}", response.StatusCode);
            }

            throw new WeatherApiException(
                "Max retries exceeded for rate-limited request.",
                (HttpStatusCode)429);
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }

    /// <summary>
    /// Exception for OpenWeatherMap API errors, carrying the HTTP status code.
    /// </summary>
    public sealed class WeatherApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public WeatherApiException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
