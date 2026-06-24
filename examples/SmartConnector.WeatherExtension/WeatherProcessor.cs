using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using Ews.Common;
using Mongoose.Common;
using Mongoose.Common.Attributes;
using Mongoose.Ews.Server.Data;
using Mongoose.Ews.Server.Data.Shared;
using SxL.Common;

namespace SmartConnector.WeatherExtension
{
    [ConfigurationDefaults("Weather Processor",
        "Provisions and updates current conditions and a 3-day forecast for a "
        + "configured city from OpenWeatherMap. Creates the EWS Server on first run.")]
    public class WeatherProcessor : WeatherProcessorBase
    {
        // ── Setup Properties ───────────────────────────────────────────

        [DefaultValue("SmartConnectorRealm")]
        [Tooltip("HTTP Digest authentication realm value for the EWS Server.")]
        public string Realm { get; set; }

        [Required]
        [DefaultValue("http://localhost:5300/WeatherService")]
        [Tooltip("HTTP endpoint address for the EWS Server.")]
        public string EwsAddress { get; set; }

        // ── Observability State ────────────────────────────────────────

        private long _cycleCount;
        private long _apiCallsTotal;
        private long _apiCallsThisCycle;

        // ── Re-entrancy Guard ──────────────────────────────────────────

        private static readonly object ExecutionLock = new object();
        private static readonly Dictionary<string, DateTime> ExecutionsInProgress =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private const double MaxExecutionMinutes = 30;

        private static bool TryEnterExecution(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "Weather";
            lock (ExecutionLock)
            {
                if (ExecutionsInProgress.TryGetValue(key, out var startedUtc)
                    && (DateTime.UtcNow - startedUtc).TotalMinutes < MaxExecutionMinutes)
                {
                    return false;
                }
                ExecutionsInProgress[key] = DateTime.UtcNow;
                return true;
            }
        }

        private static void ExitExecution(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "Weather";
            lock (ExecutionLock) { ExecutionsInProgress.Remove(key); }
        }

        // ── Execute ────────────────────────────────────────────────────

        // The cycle runs in four explicit phases:
        //   1. Provision all EWS objects (folders, points, history items)
        //   2. Update all point values (geocode, current, forecast)
        //   3. Add the current value to all history items
        //   4. Update all metadata
        // There is no backfill phase: OpenWeatherMap's free tier has no
        // historical endpoint, so the trends populate from per-cycle samples.
        protected override IEnumerable<Prompt> Execute_Subclass()
        {
            var prompts = new List<Prompt>();
            var guardKey = ServerName;

            if (!TryEnterExecution(guardKey))
            {
                Logger.LogInfo(LogCategory.Processor,
                    "A previous Weather execution is still running; skipping this "
                    + "trigger to avoid overlap.");
                return prompts;
            }

            try
            {
                var adapter = DataAdapter ?? ConnectOrCreateServer();
                if (adapter == null)
                {
                    prompts.Add(new Prompt
                    {
                        Message = "Failed to connect to or create the EWS Server.",
                        Severity = PromptSeverity.MayNotContinue
                    });
                    return prompts;
                }

                var now = DateTime.UtcNow;
                _apiCallsThisCycle = 0;

                // ── Phase 1: Provision ──
                EnsureProvisioned(adapter);
                if (IsCancellationRequested) return prompts;

                // ── Phase 2: Update point values ──
                bool healthy;
                string lastError;
                UpdateValues(adapter, now, out healthy, out lastError, prompts);

                // ── Phase 3: Add current value to all history items ──
                if (!IsCancellationRequested)
                    AddCurrentHistorySamples(adapter, now);

                // ── Phase 4: Update metadata ──
                UpdateMeta(adapter, now, healthy, lastError);

                Logger.LogInfo(LogCategory.Processor,
                    $"Cycle {_cycleCount}: {City}, apiCalls={_apiCallsThisCycle}, "
                    + $"healthy={healthy}.");
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Logger.LogError(LogCategory.Processor,
                    $"Processor failed: {ex.Message}");

                try
                {
                    if (_dataAdapter != null)
                    {
                        _dataAdapter.ModifyValueItemValue(
                            EwsStructure.MetaDataHealthy, false,
                            EwsValueStateEnum.Uncertain);
                        _dataAdapter.ModifyValueItemValue(
                            EwsStructure.MetaLastError, ex.Message,
                            EwsValueStateEnum.Good);
                    }
                }
                catch { }

                prompts.Add(new Prompt
                {
                    Message = $"Processor failed: {ex.Message}",
                    Severity = PromptSeverity.MayNotContinue
                });
            }
            finally
            {
                ExitExecution(guardKey);
            }

            return prompts;
        }

        // ── EWS Server Connection ──────────────────────────────────────

        private EwsServerDataAdapter ConnectOrCreateServer()
        {
            Logger.LogInfo(LogCategory.Processor,
                $"Starting EWS Server '{ServerName}' at {EwsAddress}.");

            try
            {
                using (EwsServerDataAdapter.ConnectNew(
                    name: ServerName, address: EwsAddress, realm: Realm,
                    username: UserName, password: Password, autoStart: true))
                {
                }
            }
            catch (DbUpdateException dbEx) when (
                dbEx.InnerException?.InnerException?.Message?
                    .Contains("duplicate key") == true)
            {
                Logger.LogDebug(LogCategory.Processor,
                    $"Server '{ServerName}' row already exists; reusing.");
            }

            _dataAdapter = EwsServerDataAdapter.ConnectExisting(
                ServerName, UserName, Password);
            ConfigureServer(_dataAdapter);
            Logger.LogInfo(LogCategory.Processor, "Connected to EWS Server.");
            return _dataAdapter;
        }

        private static void ConfigureServer(EwsServerDataAdapter adapter)
        {
            adapter.ModifyServerIsAutoStart(true);
            adapter.ModifyServerAllowCookies(true);
            adapter.ModifyServerPageSize(1000);
            adapter.ModifyServerSupportedMethods(new EwsServerMethods
            {
                GetContainerItems = true,
                GetItems = true,
                GetValues = true,
                SetValues = true,
                GetHistory = true,
                GetHistoricalDataAggregation = true,
                GetHierarchicalInformation = true,
                GetNotification = true,
                GetEnums = true,
                GetWebServiceInformation = true,
                // Advertise the alarm operations even though this extension
                // publishes no alarms. EWS clients such as EcoStruxure Building
                // Operation poll for alarms; with these enabled the server
                // returns an empty alarm list, instead of the OPERATION_NOT_SUPPORTED
                // fault EBO would otherwise log as "CWS Alarm Poll failed".
                GetAlarmEvents = true,
                GetAlarmEventTypes = true,
                GetAlarmHistory = true,
                GetUpdatedAlarmEvents = true,
                AcknowledgeAlarmEvents = true,
                Subscribe = true,
                Unsubscribe = true,
                Renew = true,
                ForceValues = false,
                UnforceValues = false
            });
        }

        // ── Provisioning Helpers ───────────────────────────────────────

        private static EwsContainerItem EnsureFolder(
            EwsServerDataAdapter adapter, string altId, string name,
            EwsContainerItem parent)
        {
            var existing = adapter.ContainerItems
                .FirstOrDefault(c => c.AlternateId == altId);
            return existing ?? adapter.AddContainerItem(
                altId, name, name, EwsContainerTypeEnum.Folder, parent);
        }

        private static EwsValueItem EnsureValue(
            EwsServerDataAdapter adapter, string altId, string name,
            EwsValueTypeEnum type, EwsContainerItem parent, string unit = null)
        {
            var existing = adapter.ValueItems
                .FirstOrDefault(v => v.AlternateId == altId);
            return existing ?? adapter.AddValueItem(
                altId, name, name, type,
                EwsValueWriteableEnum.Writeable, EwsValueForceableEnum.NotForceable,
                EwsValueStateEnum.Uncertain, unit, parent);
        }

        private static void EnsureHistoryItem(
            EwsServerDataAdapter adapter, string trendId, string name, string valueId)
        {
            var existing = adapter.HistoryItems
                .FirstOrDefault(h => h.AlternateId == trendId);
            if (existing != null)
            {
                if (!existing.IsActive)
                    adapter.ModifyHistoryItemIsActive(existing, true);
                return;
            }

            var linkedValue = adapter.ValueItems
                .FirstOrDefault(v => v.AlternateId == valueId);
            if (linkedValue == null) return;
            adapter.AddHistoryItem(trendId, name, name, linkedValue, true, null);
        }

        // ── Phase 1: Provisioning ──────────────────────────────────────

        private void EnsureProvisioned(EwsServerDataAdapter adapter)
        {
            var weatherRoot = EnsureFolder(adapter, EwsStructure.Root, "Weather", null);

            // Root observability
            var meta = EnsureFolder(adapter, EwsStructure.Meta, "Meta", weatherRoot);
            EnsureValue(adapter, EwsStructure.MetaLastUpdate, "LastUpdate",
                EwsValueTypeEnum.DateTime, meta);
            EnsureValue(adapter, EwsStructure.MetaNextUpdate, "NextUpdate",
                EwsValueTypeEnum.DateTime, meta);
            EnsureValue(adapter, EwsStructure.MetaDataHealthy, "DataHealthy",
                EwsValueTypeEnum.Boolean, meta);
            EnsureValue(adapter, EwsStructure.MetaLastError, "LastError",
                EwsValueTypeEnum.String, meta);
            EnsureValue(adapter, EwsStructure.MetaCycleCount, "CycleCount",
                EwsValueTypeEnum.Long, meta);
            EnsureValue(adapter, EwsStructure.MetaApiCalls, "ApiCallsTotal",
                EwsValueTypeEnum.Long, meta);
            EnsureValue(adapter, EwsStructure.MetaTimeZone, "TimeZone",
                EwsValueTypeEnum.String, meta);

            // Location
            var location = EnsureFolder(adapter, EwsStructure.Location, "Location", weatherRoot);
            EnsureValue(adapter, EwsStructure.LocationConfiguredCity, "ConfiguredCity",
                EwsValueTypeEnum.String, location);
            EnsureValue(adapter, EwsStructure.LocationResolvedName, "ResolvedName",
                EwsValueTypeEnum.String, location);
            EnsureValue(adapter, EwsStructure.LocationCountry, "Country",
                EwsValueTypeEnum.String, location);
            EnsureValue(adapter, EwsStructure.LocationLatitude, "Latitude",
                EwsValueTypeEnum.Double, location);
            EnsureValue(adapter, EwsStructure.LocationLongitude, "Longitude",
                EwsValueTypeEnum.Double, location);

            // Current conditions
            var current = EnsureFolder(adapter, EwsStructure.Current, "Current", weatherRoot);
            EnsureValue(adapter, EwsStructure.CurrentTemperature, "Temperature",
                EwsValueTypeEnum.Double, current, TemperatureUnit);
            EnsureValue(adapter, EwsStructure.CurrentFeelsLike, "FeelsLike",
                EwsValueTypeEnum.Double, current, TemperatureUnit);
            EnsureValue(adapter, EwsStructure.CurrentPressure, "Pressure",
                EwsValueTypeEnum.Double, current, "hPa");
            EnsureValue(adapter, EwsStructure.CurrentHumidity, "Humidity",
                EwsValueTypeEnum.Double, current, "%");
            EnsureValue(adapter, EwsStructure.CurrentWindSpeed, "WindSpeed",
                EwsValueTypeEnum.Double, current, WindSpeedUnit);
            EnsureValue(adapter, EwsStructure.CurrentDescription, "Description",
                EwsValueTypeEnum.String, current);
            EnsureValue(adapter, EwsStructure.CurrentObservedAt, "ObservedAt",
                EwsValueTypeEnum.DateTime, current);

            // Forecast Day1..DayN
            var forecast = EnsureFolder(adapter, EwsStructure.Forecast, "Forecast", weatherRoot);
            for (int day = 1; day <= EwsStructure.ForecastDays; day++)
            {
                var dayFolder = EnsureFolder(adapter,
                    EwsStructure.ForecastDay(day), $"Day{day}", forecast);
                EnsureValue(adapter, EwsStructure.ForecastDate(day), "Date",
                    EwsValueTypeEnum.DateTime, dayFolder);
                EnsureValue(adapter, EwsStructure.ForecastHighTemp(day), "HighTemp",
                    EwsValueTypeEnum.Double, dayFolder, TemperatureUnit);
                EnsureValue(adapter, EwsStructure.ForecastLowTemp(day), "LowTemp",
                    EwsValueTypeEnum.Double, dayFolder, TemperatureUnit);
                EnsureValue(adapter, EwsStructure.ForecastPressure(day), "Pressure",
                    EwsValueTypeEnum.Double, dayFolder, "hPa");
                EnsureValue(adapter, EwsStructure.ForecastHumidity(day), "Humidity",
                    EwsValueTypeEnum.Double, dayFolder, "%");
                EnsureValue(adapter, EwsStructure.ForecastDescription(day), "Description",
                    EwsValueTypeEnum.String, dayFolder);
            }

            // Trend logs
            foreach (var trend in EwsStructure.TrendDefinitions)
                EnsureHistoryItem(adapter, trend.TrendId, trend.Name, trend.ValueId);
        }

        // ── Phase 2: Update point values ───────────────────────────────

        private void UpdateValues(
            EwsServerDataAdapter adapter, DateTime now,
            out bool healthy, out string lastError, List<Prompt> prompts)
        {
            healthy = false;
            lastError = "";

            using (var client = new OpenWeatherMapClient(ApiKey))
            {
                adapter.AutoCommit = false;
                try
                {
                    var location = ResolveLocation(adapter, client);
                    if (location == null)
                    {
                        lastError = $"City '{City}' could not be resolved.";
                        prompts.Add(new Prompt
                        {
                            Message = lastError,
                            Severity = PromptSeverity.MayNotContinue
                        });
                        adapter.CommitChanges();
                        return;
                    }

                    FetchCurrent(adapter, client, location);
                    if (UpdateForecast && !IsCancellationRequested)
                        FetchForecast(adapter, client, location, now);

                    healthy = true;
                    adapter.CommitChanges();
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    adapter.DiscardChanges();
                    lastError = ex.Message;
                    Logger.LogError(LogCategory.Processor,
                        $"Update failed for {City}: {ex.Message}");
                    prompts.Add(new Prompt
                    {
                        Message = $"Weather update failed: {ex.Message}",
                        Severity = PromptSeverity.MayContinue
                    });
                }
                finally
                {
                    adapter.AutoCommit = true;
                }
            }
        }

        // Resolves the configured city to coordinates, reusing the cached
        // resolution stored in Location/* when the City has not changed so a
        // geocoding call is not spent on every cycle.
        private GeocodeResult ResolveLocation(
            EwsServerDataAdapter adapter, OpenWeatherMapClient client)
        {
            var cachedCity = ReadString(adapter, EwsStructure.LocationConfiguredCity);
            if (string.Equals(cachedCity, City, StringComparison.OrdinalIgnoreCase))
            {
                var lat = ReadDouble(adapter, EwsStructure.LocationLatitude);
                var lon = ReadDouble(adapter, EwsStructure.LocationLongitude);
                if (lat.HasValue && lon.HasValue)
                {
                    return new GeocodeResult
                    {
                        Name = ReadString(adapter, EwsStructure.LocationResolvedName),
                        Country = ReadString(adapter, EwsStructure.LocationCountry),
                        Lat = lat.Value,
                        Lon = lon.Value
                    };
                }
            }

            _apiCallsThisCycle++;
            var geo = OpenWeatherMapResponseParser.ParseGeocode(client.GeocodeCity(City));
            if (geo == null) return null;

            adapter.ModifyValueItemValue(EwsStructure.LocationConfiguredCity, City,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.LocationResolvedName, geo.Name,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.LocationCountry, geo.Country ?? "",
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.LocationLatitude, geo.Lat,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.LocationLongitude, geo.Lon,
                EwsValueStateEnum.Good);
            return geo;
        }

        private void FetchCurrent(
            EwsServerDataAdapter adapter, OpenWeatherMapClient client, GeocodeResult location)
        {
            _apiCallsThisCycle++;
            var current = OpenWeatherMapResponseParser.ParseCurrent(
                client.GetCurrentWeather(location.Lat, location.Lon, UnitsApiValue));

            adapter.ModifyValueItemValue(EwsStructure.CurrentTemperature,
                current.Temperature, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.CurrentFeelsLike,
                current.FeelsLike, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.CurrentPressure,
                current.Pressure, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.CurrentHumidity,
                current.Humidity, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.CurrentWindSpeed,
                current.WindSpeed, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.CurrentDescription,
                current.Description ?? "", EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.CurrentObservedAt,
                (DateTime?)current.ObservedAtUtc, EwsValueStateEnum.Good);

            Logger.LogInfo(LogCategory.Processor,
                $"Current for {location.Name}: {current.Temperature}{TemperatureUnit}, "
                + $"{current.Humidity}% RH, \"{current.Description}\".");
        }

        private void FetchForecast(
            EwsServerDataAdapter adapter, OpenWeatherMapClient client,
            GeocodeResult location, DateTime now)
        {
            _apiCallsThisCycle++;
            var parsed = OpenWeatherMapResponseParser.ParseForecast(
                client.GetForecast(location.Lat, location.Lon, UnitsApiValue));

            var days = ForecastAggregator.Aggregate(
                parsed.Points, parsed.TimezoneOffsetSeconds, now, EwsStructure.ForecastDays);

            for (int i = 0; i < EwsStructure.ForecastDays && i < days.Count; i++)
            {
                int day = i + 1;
                var d = days[i];
                adapter.ModifyValueItemValue(EwsStructure.ForecastDate(day),
                    (DateTime?)d.Date, EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(EwsStructure.ForecastHighTemp(day),
                    d.HighTemp, EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(EwsStructure.ForecastLowTemp(day),
                    d.LowTemp, EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(EwsStructure.ForecastPressure(day),
                    d.Pressure, EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(EwsStructure.ForecastHumidity(day),
                    d.Humidity, EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(EwsStructure.ForecastDescription(day),
                    d.Description ?? "", EwsValueStateEnum.Good);
            }
        }

        // ── Phase 3: Per-cycle history ─────────────────────────────────

        private void AddCurrentHistorySamples(EwsServerDataAdapter adapter, DateTime now)
        {
            foreach (var trend in EwsStructure.TrendDefinitions)
            {
                if (IsCancellationRequested) return;

                var valueItem = adapter.ValueItems
                    .FirstOrDefault(v => v.AlternateId == trend.ValueId);
                if (valueItem == null || string.IsNullOrEmpty(valueItem.Value))
                    continue;

                // The EWS server records a history sample automatically whenever a
                // value changes (see ModifyValueItemValue). Adding one here too
                // would create a duplicate with the same value and timestamp, so
                // only add a manual sample when the server has not already sampled
                // this point during the current cycle.
                if (adapter.HistoryRecords.Any(r =>
                        r.EwsHistoryItem.AlternateId == trend.TrendId
                        && r.Value == valueItem.Value
                        && r.Timestamp >= now))
                    continue;

                try
                {
                    adapter.AddHistoryRecordsManually(trend.TrendId,
                        new List<Tuple<EwsValueStateEnum, DateTime, string>>
                        {
                            Tuple.Create(EwsValueStateEnum.Good, now, valueItem.Value)
                        });
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Logger.LogDebug(LogCategory.Processor,
                        $"History add failed for {trend.TrendId}: {ex.Message}");
                }
            }
        }

        // ── Phase 4: Metadata ──────────────────────────────────────────

        private void UpdateMeta(
            EwsServerDataAdapter adapter, DateTime now, bool healthy, string lastError)
        {
            _cycleCount++;
            _apiCallsTotal += _apiCallsThisCycle;
            var nextUpdate = DeriveNextUpdate(adapter, now);

            adapter.AutoCommit = false;
            adapter.ModifyValueItemValue(EwsStructure.MetaLastUpdate,
                (DateTime?)now, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.MetaNextUpdate,
                (DateTime?)nextUpdate, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.MetaDataHealthy, healthy,
                healthy ? EwsValueStateEnum.Good : EwsValueStateEnum.Uncertain);
            adapter.ModifyValueItemValue(EwsStructure.MetaLastError, lastError ?? "",
                string.IsNullOrEmpty(lastError)
                    ? EwsValueStateEnum.Good : EwsValueStateEnum.Uncertain);
            adapter.ModifyValueItemValue(EwsStructure.MetaCycleCount, _cycleCount,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.MetaApiCalls, _apiCallsTotal,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(EwsStructure.MetaTimeZone, TimeZoneInfo.Local.Id,
                EwsValueStateEnum.Good);
            adapter.CommitChanges();
            adapter.AutoCommit = true;
        }

        private static DateTime DeriveNextUpdate(EwsServerDataAdapter adapter, DateTime now)
        {
            DateTime? lastUpdate = null;
            var previous = adapter.ValueItems
                .FirstOrDefault(v => v.AlternateId == EwsStructure.MetaLastUpdate);
            if (previous != null
                && DateTime.TryParse(previous.Value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var prevDt)
                && prevDt > DateTime.MinValue)
            {
                lastUpdate = prevDt;
            }
            return ComputeNextUpdate(lastUpdate, now);
        }

        // Estimates the next run time from the interval since the last run,
        // clamped to a sane range so a long idle gap cannot project an absurd
        // NextUpdate far into the future. Public static for unit testing.
        public static DateTime ComputeNextUpdate(DateTime? lastUpdate, DateTime now)
        {
            var defaultInterval = TimeSpan.FromMinutes(5);
            var minInterval = TimeSpan.FromMinutes(1);
            var maxInterval = TimeSpan.FromHours(24);

            if (!lastUpdate.HasValue || lastUpdate.Value == DateTime.MinValue
                || lastUpdate.Value > now)
                return now + defaultInterval;

            var interval = now - lastUpdate.Value;
            if (interval < minInterval) interval = minInterval;
            if (interval > maxInterval) interval = maxInterval;
            return now + interval;
        }

        // ── Value-item readers (for location caching) ──────────────────

        private static string ReadString(EwsServerDataAdapter adapter, string altId)
        {
            var item = adapter.ValueItems.FirstOrDefault(v => v.AlternateId == altId);
            return item?.Value ?? "";
        }

        private static double? ReadDouble(EwsServerDataAdapter adapter, string altId)
        {
            var item = adapter.ValueItems.FirstOrDefault(v => v.AlternateId == altId);
            if (item == null || string.IsNullOrEmpty(item.Value)) return null;
            return double.TryParse(item.Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var d) ? d : (double?)null;
        }
    }
}
