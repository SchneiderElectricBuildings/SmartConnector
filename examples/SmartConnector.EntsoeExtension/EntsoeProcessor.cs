using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using Ews.Common;
using Mongoose.Common;
using Mongoose.Common.Attributes;
using Mongoose.Ews.Server.Data;
using Mongoose.Ews.Server.Data.Shared;
using SxL.Common;

namespace SmartConnector.EntsoeExtension
{
    [ConfigurationDefaults("ENTSO-E Processor",
        "Provisions and updates ENTSO-E day-ahead prices, load, and generation "
        + "data for the selected country. Creates the EWS Server on first run.")]
    public class EntsoeProcessor : EntsoeProcessorBase
    {
        // ── Setup Properties ───────────────────────────────────────────

        [DefaultValue("SmartConnectorRealm")]
        [Tooltip("HTTP Digest authentication realm value for the EWS Server.")]
        public string Realm { get; set; }

        [Required]
        [DefaultValue("http://localhost:5300/EntsoeEnergyData")]
        [Tooltip("HTTP endpoint address for the EWS Server.")]
        public string EwsAddress { get; set; }

        // ── Feature Flags ──────────────────────────────────────────────

        [DefaultValue(true)]
        [Tooltip("Enable fetching and publishing day-ahead price data.")]
        public bool UpdatePrices { get; set; }

        [DefaultValue(true)]
        [Tooltip("Enable fetching and publishing actual and forecast load data.")]
        public bool UpdateLoad { get; set; }

        [DefaultValue(true)]
        [Tooltip("Enable fetching and publishing generation forecast data.")]
        public bool UpdateGeneration { get; set; }

        // ── Provisioning State ─────────────────────────────────────────

        private bool _rootProvisioned;
        private readonly HashSet<string> _provisionedZones =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ── Observability State ────────────────────────────────────────

        private long _cycleCount;
        private long _totalRowsConsumed;
        private long _rowsThisCycle;

        // ── Re-entrancy Guard ──────────────────────────────────────────

        // A full cycle ends with a backfill that can run for several minutes.
        // If the schedule re-triggers before it finishes, this static guard
        // makes the new trigger return immediately instead of overlapping.
        // Keyed by server name so independent processors do not block each
        // other; the timeout releases a stale lock if a run never completes.
        private static readonly object ExecutionLock = new object();
        private static readonly Dictionary<string, DateTime> ExecutionsInProgress =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private const double MaxExecutionMinutes = 30;

        private static bool TryEnterExecution(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "ENTSOE";
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
            if (string.IsNullOrEmpty(key)) key = "ENTSOE";
            lock (ExecutionLock)
            {
                ExecutionsInProgress.Remove(key);
            }
        }

        // Resolved per-zone state carried across the five phases of a cycle.
        private sealed class ZoneContext
        {
            public string Code;
            public AreaMapping.AreaInfo Area;
            public ZoneStructure Structure;
            public BackfillPlanner.Plan Plan;
            public bool Healthy;
        }

        // ── Execute ────────────────────────────────────────────────────

        // The cycle runs in five explicit phases so that the long-running
        // backfill always happens last, after the live data and metadata are
        // already committed:
        //   1. Provision all EWS objects (folders, points, history items)
        //   2. Update all point values
        //   3. Add the current value to all history items
        //   4. Update all metadata
        //   5. Backfill trend history (may take minutes)
        protected override IEnumerable<Prompt> Execute_Subclass()
        {
            var prompts = new List<Prompt>();
            var guardKey = string.IsNullOrEmpty(ServerName) ? EwsAddress : ServerName;

            if (!TryEnterExecution(guardKey))
            {
                Logger.LogInfo(LogCategory.Processor,
                    "A previous ENTSO-E execution is still running (likely a "
                    + "long backfill). Skipping this trigger to avoid overlap.");
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
                var countryName = GetCountryDisplayName();
                var zones = ResolveZones(countryName);

                Logger.LogInfo(LogCategory.Processor,
                    $"Processing {countryName} ({zones.Count} zone(s)).");

                // ── Phase 1: Provision all EWS objects ──
                EnsureRootProvisioned(adapter);
                foreach (var zone in zones)
                {
                    if (IsCancellationRequested) return prompts;
                    EnsureZoneProvisioned(
                        adapter, zone.Code, zone.Area, zone.Structure);

                    // Capture the backfill plan now, before Phase 4 stamps
                    // Meta/LastUpdate — the decision must be based on the
                    // previous cycle's timestamp, not this one's.
                    zone.Plan = BackfillPlanner.Create(
                        ReadZoneLastUpdate(adapter, zone.Structure),
                        now, BackfillDays);
                }

                // ── Phase 2: Update all point values ──
                _rowsThisCycle = 0;
                bool anySuccess = false;
                using (var apiClient = new EntsoeApiClient(ApiToken))
                {
                    foreach (var zone in zones)
                    {
                        if (IsCancellationRequested) return prompts;
                        zone.Healthy = UpdateZoneValues(
                            adapter, apiClient, zone, now, prompts);
                        if (zone.Healthy)
                        {
                            anySuccess = true;
                        }
                        else
                        {
                            // A failed zone calls DiscardChanges, which recreates
                            // the DbContext but leaves the adapter's entity caches
                            // (Server, history items) bound to the disposed context.
                            // The next zone's value write auto-samples history
                            // against those stale entities and throws a cross-
                            // context IEntityChangeTracker error. Reconnect to a
                            // fresh adapter (empty caches) before the next zone.
                            adapter = RefreshAdapter();
                        }
                    }
                }

                // ── Phase 3: Add the current value to all history items ──
                foreach (var zone in zones)
                {
                    if (IsCancellationRequested) break;
                    AddCurrentHistorySamples(adapter, zone.Structure, now);
                }

                // ── Phase 4: Update all metadata ──
                foreach (var zone in zones)
                    UpdateZoneMeta(adapter, zone, now);
                UpdateRootMeta(adapter, now, anySuccess);

                Logger.LogInfo(LogCategory.Processor,
                    $"Cycle {_cycleCount}: {countryName}, "
                    + $"rows={_rowsThisCycle}, healthy={anySuccess}.");

                // ── Phase 5: Backfill trend history (long-running, last) ──
                RunBackfill(adapter, zones);
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
                            EwsStructure.MetaDataHealthy,
                            false,
                            EwsValueStateEnum.Uncertain);
                        _dataAdapter.ModifyValueItemValue(
                            EwsStructure.MetaLastError,
                            ex.Message,
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

        private List<ZoneContext> ResolveZones(string countryName)
        {
            var zones = new List<ZoneContext>();
            foreach (var zoneCode in GetZoneCodes())
            {
                var area = AreaMapping.Lookup(zoneCode);
                if (area == null)
                {
                    Logger.LogInfo(LogCategory.Processor,
                        $"Skipping unknown zone code: {zoneCode}");
                    continue;
                }

                zones.Add(new ZoneContext
                {
                    Code = zoneCode,
                    Area = area,
                    Structure = EwsStructure.ForZone(countryName, zoneCode)
                });
            }
            return zones;
        }

        // ── EWS Server Connection ──────────────────────────────────────

        private EwsServerDataAdapter ConnectOrCreateServer()
        {
            Logger.LogInfo(LogCategory.Processor,
                $"Starting EWS Server '{ServerName}' at {EwsAddress}.");

            try
            {
                using (EwsServerDataAdapter.ConnectNew(
                    name: ServerName,
                    address: EwsAddress,
                    realm: Realm,
                    username: UserName,
                    password: Password,
                    autoStart: true))
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

            Logger.LogInfo(LogCategory.Processor,
                "Connected to EWS Server.");
            return _dataAdapter;
        }

        // Reconnects the shared adapter, returning a fresh instance with empty
        // entity caches. Used after a per-zone transaction fails: DiscardChanges
        // alone recreates the DbContext but does not clear the adapter's cached
        // Server and history entities, which would then be attached to the new
        // context on the next write and raise a cross-context
        // IEntityChangeTracker error.
        private EwsServerDataAdapter RefreshAdapter()
        {
            try
            {
                _dataAdapter?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogDebug(LogCategory.Processor,
                    $"Adapter dispose during refresh failed: {ex.Message}");
            }

            _dataAdapter = EwsServerDataAdapter.ConnectExisting(
                ServerName, UserName, Password);
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
            EwsServerDataAdapter adapter,
            string altId, string name,
            EwsContainerItem parent)
        {
            var existing = adapter.ContainerItems
                .FirstOrDefault(c => c.AlternateId == altId);
            if (existing != null) return existing;

            return adapter.AddContainerItem(
                altId, name, name,
                EwsContainerTypeEnum.Folder, parent);
        }

        private static EwsValueItem EnsureValue(
            EwsServerDataAdapter adapter,
            string altId, string name,
            EwsValueTypeEnum type,
            EwsContainerItem parent,
            string unit = null)
        {
            var existing = adapter.ValueItems
                .FirstOrDefault(v => v.AlternateId == altId);
            if (existing != null) return existing;

            return adapter.AddValueItem(
                altId, name, name, type,
                EwsValueWriteableEnum.Writeable,
                EwsValueForceableEnum.NotForceable,
                EwsValueStateEnum.Uncertain,
                unit, parent);
        }

        private static void EnsureHistoryItem(
            EwsServerDataAdapter adapter,
            string trendId, string name, string valueId)
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

            adapter.AddHistoryItem(
                trendId, name, name,
                linkedValue,
                true, null);
        }

        // ── Root Provisioning ──────────────────────────────────────────

        private void EnsureRootProvisioned(EwsServerDataAdapter adapter)
        {
            if (_rootProvisioned) return;

            Logger.LogDebug(LogCategory.Processor,
                "Provision: creating root container hierarchy.");

            var entsoeRoot = EnsureFolder(adapter,
                EwsStructure.Root, "ENTSOE", null);

            // Root-level observability: /ENTSOE/Meta (mirrors GridStatus)
            var rootMeta = EnsureFolder(adapter,
                "/ENTSOE/Meta", "Meta", entsoeRoot);
            EnsureValue(adapter, EwsStructure.MetaLastUpdate, "LastUpdate",
                EwsValueTypeEnum.DateTime, rootMeta);
            EnsureValue(adapter, EwsStructure.MetaNextUpdate, "NextUpdate",
                EwsValueTypeEnum.DateTime, rootMeta);
            EnsureValue(adapter, EwsStructure.MetaDataHealthy, "DataHealthy",
                EwsValueTypeEnum.Boolean, rootMeta);
            EnsureValue(adapter, EwsStructure.MetaLastError, "LastError",
                EwsValueTypeEnum.String, rootMeta);
            EnsureValue(adapter, EwsStructure.MetaCycleCount, "CycleCount",
                EwsValueTypeEnum.Long, rootMeta);
            EnsureValue(adapter, EwsStructure.MetaRowsConsumed, "RowsConsumed",
                EwsValueTypeEnum.Long, rootMeta);
            EnsureValue(adapter, EwsStructure.MetaTimeZone, "TimeZone",
                EwsValueTypeEnum.String, rootMeta);

            // Countries container: /ENTSOE/Countries
            EnsureFolder(adapter,
                EwsStructure.CountriesContainer, "Countries", entsoeRoot);

            _rootProvisioned = true;
        }

        // Derives the next expected run time from the previous LastUpdate
        // timestamp. Falls back to +5 minutes on the first cycle.
        private static DateTime DeriveNextUpdate(
            EwsServerDataAdapter adapter, DateTime now)
        {
            DateTime? lastUpdate = null;

            var previous = adapter.ValueItems
                .FirstOrDefault(v => v.AlternateId == EwsStructure.MetaLastUpdate);
            if (previous != null
                && DateTime.TryParse(previous.Value,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal
                    | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var prevDt)
                && prevDt > DateTime.MinValue)
            {
                lastUpdate = prevDt;
            }

            return ComputeNextUpdate(lastUpdate, now);
        }

        // Estimates the next run time from the interval since the last run.
        // The interval is clamped to a sane range so that a long idle gap
        // (e.g. the processor was stopped during development) cannot project
        // an absurd NextUpdate years into the future. It self-corrects to the
        // real cadence on the following cycle. Public static for unit testing.
        public static DateTime ComputeNextUpdate(DateTime? lastUpdate, DateTime now)
        {
            var defaultInterval = TimeSpan.FromMinutes(5);
            var minInterval = TimeSpan.FromMinutes(1);
            var maxInterval = TimeSpan.FromHours(24);

            if (!lastUpdate.HasValue
                || lastUpdate.Value == DateTime.MinValue
                || lastUpdate.Value > now)
            {
                return now + defaultInterval;
            }

            var interval = now - lastUpdate.Value;
            if (interval < minInterval) interval = minInterval;
            if (interval > maxInterval) interval = maxInterval;
            return now + interval;
        }

        // ── Per-Zone Provisioning ──────────────────────────────────────

        private void EnsureZoneProvisioned(
            EwsServerDataAdapter adapter,
            string zoneCode,
            AreaMapping.AreaInfo area,
            ZoneStructure structure)
        {
            if (_provisionedZones.Contains(zoneCode))
                return;

            Logger.LogInfo(LogCategory.Processor,
                $"Provisioning EWS data structure for "
                + $"{area.Description} ({zoneCode}).");

            // Country folder: /ENTSOE/Countries/{CountryName}
            var countriesFolder = adapter.ContainerItems
                .FirstOrDefault(c => c.AlternateId == EwsStructure.CountriesContainer);
            var countryFolder = EnsureFolder(adapter,
                structure.CountryFolder, structure.CountryName, countriesFolder);

            // Zone root: /ENTSOE/Countries/{CountryName}/{ZoneCode}
            var zoneRoot = EnsureFolder(adapter,
                structure.Root, zoneCode, countryFolder);

            // Meta container
            Logger.LogDebug(LogCategory.Processor, "Provision: creating Meta items.");
            var metaFolder = EnsureFolder(adapter, structure.Meta, "Meta", zoneRoot);
            EnsureValue(adapter, structure.MetaLastUpdate, "LastUpdate",
                EwsValueTypeEnum.DateTime, metaFolder);
            EnsureValue(adapter, structure.MetaCountry, "Country",
                EwsValueTypeEnum.String, metaFolder);
            EnsureValue(adapter, structure.MetaBiddingZone, "BiddingZone",
                EwsValueTypeEnum.String, metaFolder);
            EnsureValue(adapter, structure.MetaDataHealthy, "DataHealthy",
                EwsValueTypeEnum.Boolean, metaFolder);
            EnsureValue(adapter, structure.MetaTimeZone, "TimeZone",
                EwsValueTypeEnum.String, metaFolder);

            // Price container
            Logger.LogDebug(LogCategory.Processor, "Provision: creating Price items.");
            var priceFolder = EnsureFolder(adapter, structure.Price, "Price", zoneRoot);
            EnsureValue(adapter, structure.PriceCurrentPrice, "CurrentPrice",
                EwsValueTypeEnum.Double, priceFolder, "EUR/MWh");
            EnsureValue(adapter, structure.PriceNextHour, "NextHour",
                EwsValueTypeEnum.Double, priceFolder, "EUR/MWh");
            EnsureValue(adapter, structure.PriceCurrency, "Currency",
                EwsValueTypeEnum.String, priceFolder);
            EnsureValue(adapter, structure.PriceUnit, "Unit",
                EwsValueTypeEnum.String, priceFolder);
            EnsureValue(adapter, structure.PriceDayMinimum, "DayMinimum",
                EwsValueTypeEnum.Double, priceFolder, "EUR/MWh");
            EnsureValue(adapter, structure.PriceDayMaximum, "DayMaximum",
                EwsValueTypeEnum.Double, priceFolder, "EUR/MWh");
            EnsureValue(adapter, structure.PriceDayAverage, "DayAverage",
                EwsValueTypeEnum.Double, priceFolder, "EUR/MWh");
            EnsureValue(adapter, structure.PriceLevel, "Level",
                EwsValueTypeEnum.Long, priceFolder);
            EnsureValue(adapter, structure.PricePriceCategory, "PriceCategory",
                EwsValueTypeEnum.String, priceFolder);
            EnsureValue(adapter, structure.PriceCheapThreshold, "CheapThreshold",
                EwsValueTypeEnum.Double, priceFolder, "EUR/MWh");
            EnsureValue(adapter, structure.PriceExpensiveThreshold, "ExpensiveThreshold",
                EwsValueTypeEnum.Double, priceFolder, "EUR/MWh");
            EnsureValue(adapter, structure.PriceRankToday, "RankToday",
                EwsValueTypeEnum.Long, priceFolder);
            EnsureValue(adapter, structure.PricePercentileToday, "PercentileToday",
                EwsValueTypeEnum.Double, priceFolder, "%");
            EnsureValue(adapter, structure.PriceNegativeActive, "NegativeActive",
                EwsValueTypeEnum.Boolean, priceFolder);

            // Hourly prices container
            var hourlyFolder = EnsureFolder(adapter,
                structure.HourlyPrices, "Hourly", priceFolder);
            for (int hour = 0; hour < 24; hour++)
            {
                EnsureValue(adapter, structure.HourlyPriceId(hour),
                    $"Hour{hour:D2}", EwsValueTypeEnum.Double, hourlyFolder, "EUR/MWh");
            }

            // Load container
            Logger.LogDebug(LogCategory.Processor, "Provision: creating Load items.");
            var loadFolder = EnsureFolder(adapter, structure.Load, "Load", zoneRoot);
            EnsureValue(adapter, structure.LoadCurrent, "Current",
                EwsValueTypeEnum.Double, loadFolder, "MW");
            EnsureValue(adapter, structure.LoadForecastPeakToday, "ForecastPeakToday",
                EwsValueTypeEnum.Double, loadFolder, "MW");
            EnsureValue(adapter, structure.LoadForecastPeakHour, "ForecastPeakHour",
                EwsValueTypeEnum.DateTime, loadFolder);
            EnsureValue(adapter, structure.LoadPeakRiskLevel, "PeakRiskLevel",
                EwsValueTypeEnum.Long, loadFolder);

            // Generation container — Wind and Solar as separate items
            Logger.LogDebug(LogCategory.Processor, "Provision: creating Generation items.");
            var generationFolder = EnsureFolder(adapter,
                structure.Generation, "Generation", zoneRoot);
            EnsureValue(adapter, structure.GenerationForecastTotal, "ForecastTotal",
                EwsValueTypeEnum.Double, generationFolder, "MW");
            EnsureValue(adapter, structure.GenerationWind, "Wind",
                EwsValueTypeEnum.Double, generationFolder, "MW");
            EnsureValue(adapter, structure.GenerationSolar, "Solar",
                EwsValueTypeEnum.Double, generationFolder, "MW");

            // LoadShifting container
            Logger.LogDebug(LogCategory.Processor, "Provision: creating LoadShifting items.");
            var loadShiftingFolder = EnsureFolder(adapter,
                structure.LoadShifting, "LoadShifting", zoneRoot);
            EnsureValue(adapter, structure.ShiftIsCurrentlyCheap, "IsCurrentlyCheap",
                EwsValueTypeEnum.Boolean, loadShiftingFolder);
            EnsureValue(adapter, structure.ShiftIsCurrentlyExpensive, "IsCurrentlyExpensive",
                EwsValueTypeEnum.Boolean, loadShiftingFolder);
            EnsureValue(adapter, structure.ShiftHoursUntilCheap, "HoursUntilCheap",
                EwsValueTypeEnum.Long, loadShiftingFolder, "h");
            EnsureValue(adapter, structure.ShiftHoursUntilExpensive, "HoursUntilExpensive",
                EwsValueTypeEnum.Long, loadShiftingFolder, "h");
            EnsureValue(adapter, structure.ShiftCheapHoursToday, "CheapHoursToday",
                EwsValueTypeEnum.String, loadShiftingFolder);
            EnsureValue(adapter, structure.ShiftExpensiveHoursToday, "ExpensiveHoursToday",
                EwsValueTypeEnum.String, loadShiftingFolder);
            EnsureValue(adapter, structure.ShiftRenewablePercent, "RenewablePercent",
                EwsValueTypeEnum.Double, loadShiftingFolder, "%");

            // Trend logs
            Logger.LogDebug(LogCategory.Processor, "Provision: creating trend logs.");
            foreach (var trend in structure.TrendDefinitions)
            {
                if (IsCancellationRequested) return;
                EnsureHistoryItem(adapter,
                    trend.TrendId, trend.Name, trend.ValueId);
            }

            // Set initial metadata
            SetInitialStatus(adapter, zoneCode, area, structure);

            _provisionedZones.Add(zoneCode);

            Logger.LogInfo(LogCategory.Processor,
                $"Provisioned {zoneCode} successfully.");
        }

        private void SetInitialStatus(
            EwsServerDataAdapter adapter,
            string zoneCode,
            AreaMapping.AreaInfo area,
            ZoneStructure structure)
        {
            adapter.AutoCommit = false;
            adapter.ModifyValueItemValue(
                structure.MetaCountry,
                area.Description,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                structure.MetaBiddingZone,
                area.EicCode,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                structure.MetaDataHealthy,
                false,
                EwsValueStateEnum.Uncertain);
            // MetaLastUpdate is intentionally left unset here. The first
            // successful update cycle sets it, and its absence is the signal
            // that this zone still needs an initial history backfill.
            var tz = AreaMapping.GetTimeZone(zoneCode);
            if (tz != null)
            {
                adapter.ModifyValueItemValue(
                    structure.MetaTimeZone,
                    tz.Id,
                    EwsValueStateEnum.Good);
            }
            adapter.CommitChanges();
            adapter.AutoCommit = true;
        }

        // ── Data Update ────────────────────────────────────────────────

        // ── Phase 2: Update point values ───────────────────────────────

        private bool UpdateZoneValues(
            EwsServerDataAdapter adapter,
            EntsoeApiClient apiClient,
            ZoneContext zone,
            DateTime now,
            List<Prompt> prompts)
        {
            var tz = AreaMapping.GetTimeZone(zone.Code);
            DateTime periodStart, periodEnd;

            if (tz != null)
            {
                var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, tz);
                var localMidnight = localNow.Date;
                periodStart = TimeZoneInfo.ConvertTimeToUtc(localMidnight, tz);
                periodEnd = TimeZoneInfo.ConvertTimeToUtc(
                    localMidnight.AddDays(1), tz);
            }
            else
            {
                periodStart = now.Date;
                periodEnd = now.Date.AddDays(1);
            }

            Logger.LogInfo(LogCategory.Processor,
                $"Querying ENTSO-E for {zone.Area.Description} "
                + $"({zone.Area.EicCode}) period {periodStart:u} to {periodEnd:u}.");

            adapter.AutoCommit = false;
            try
            {
                if (UpdatePrices)
                {
                    if (IsCancellationRequested) return false;
                    FetchAndUpdatePrices(adapter, apiClient, zone.Area,
                        zone.Structure, periodStart, periodEnd, now, prompts);
                }

                if (UpdateLoad)
                {
                    if (IsCancellationRequested) return false;
                    FetchAndUpdateLoad(adapter, apiClient, zone.Area,
                        zone.Structure, periodStart, periodEnd, prompts);
                }

                if (UpdateGeneration)
                {
                    if (IsCancellationRequested) return false;
                    FetchAndUpdateGeneration(adapter, apiClient, zone.Area,
                        zone.Structure, periodStart, periodEnd, prompts);
                }

                adapter.CommitChanges();
                Logger.LogInfo(LogCategory.Processor,
                    $"ENTSO-E values updated for {zone.Code}.");
                return true;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                adapter.DiscardChanges();
                Logger.LogError(LogCategory.Processor,
                    $"Failed to update {zone.Code}: {ex.Message}");
                prompts.Add(new Prompt
                {
                    Message = $"Update failed for {zone.Code}: {ex.Message}",
                    Severity = PromptSeverity.MayContinue
                });
                return false;
            }
            finally
            {
                adapter.AutoCommit = true;
            }
        }

        // ── Phase 3: Add current value to all history items ────────────

        // Writes the current value of each trended point as a history record
        // at the cycle timestamp. This guarantees every trend gains a sample
        // each cycle, independent of the EWS Server's change-driven auto-
        // sampling. The value is taken verbatim from the value item so the
        // string format always matches the trend's value type.
        private void AddCurrentHistorySamples(
            EwsServerDataAdapter adapter,
            ZoneStructure structure,
            DateTime now)
        {
            foreach (var trend in structure.TrendDefinitions)
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
                    adapter.AddHistoryRecordsManually(
                        trend.TrendId,
                        new List<Tuple<EwsValueStateEnum, DateTime, string>>
                        {
                            Tuple.Create(
                                EwsValueStateEnum.Good, now, valueItem.Value)
                        });
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Logger.LogDebug(LogCategory.Processor,
                        $"History add failed for {trend.TrendId}: {ex.Message}");
                }
            }
        }

        // ── Phase 4: Update metadata ───────────────────────────────────

        private void UpdateZoneMeta(
            EwsServerDataAdapter adapter, ZoneContext zone, DateTime now)
        {
            adapter.AutoCommit = false;
            adapter.ModifyValueItemValue(
                zone.Structure.MetaLastUpdate,
                (DateTime?)now,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                zone.Structure.MetaDataHealthy,
                zone.Healthy,
                zone.Healthy
                    ? EwsValueStateEnum.Good
                    : EwsValueStateEnum.Uncertain);
            adapter.CommitChanges();
            adapter.AutoCommit = true;
        }

        private void UpdateRootMeta(
            EwsServerDataAdapter adapter, DateTime now, bool anySuccess)
        {
            _cycleCount++;
            _totalRowsConsumed += _rowsThisCycle;
            var nextUpdate = DeriveNextUpdate(adapter, now);

            adapter.AutoCommit = false;
            adapter.ModifyValueItemValue(
                EwsStructure.MetaLastUpdate,
                (DateTime?)now,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaNextUpdate,
                (DateTime?)nextUpdate,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaDataHealthy,
                anySuccess,
                anySuccess
                    ? EwsValueStateEnum.Good
                    : EwsValueStateEnum.Uncertain);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaCycleCount,
                _cycleCount,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaRowsConsumed,
                _totalRowsConsumed,
                EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaTimeZone,
                TimeZoneInfo.Local.Id,
                EwsValueStateEnum.Good);
            adapter.CommitChanges();
            adapter.AutoCommit = true;
        }

        // ── Phase 5: Backfill ──────────────────────────────────────────

        private void RunBackfill(
            EwsServerDataAdapter adapter, List<ZoneContext> zones)
        {
            if (!zones.Any(z => z.Plan.ShouldBackfill)) return;

            using (var apiClient = new EntsoeApiClient(ApiToken))
            {
                foreach (var zone in zones)
                {
                    if (IsCancellationRequested) return;
                    if (!zone.Plan.ShouldBackfill) continue;
                    BackfillZoneHistory(
                        adapter, apiClient, zone.Area, zone.Structure, zone.Plan);
                }
            }
        }

        // ── Trend History Backfill ─────────────────────────────────────

        // Seeds trend logs with historical data so the EBO trend viewer is
        // populated immediately rather than waiting for live auto-samples to
        // accumulate. On first run it backfills BackfillDays of history; after
        // a reconnection gap it backfills only the missed period. Idempotent
        // across restarts because the window is derived from the persisted
        // zone Meta/LastUpdate timestamp.
        private void BackfillZoneHistory(
            EwsServerDataAdapter adapter,
            EntsoeApiClient apiClient,
            AreaMapping.AreaInfo area,
            ZoneStructure structure,
            BackfillPlanner.Plan plan)
        {
            if (!plan.ShouldBackfill) return;

            Logger.LogInfo(LogCategory.Processor,
                $"Backfilling history for {structure.ZoneCode} "
                + $"from {plan.Start:u} to {plan.End:u}.");

            int total = 0;

            if (UpdatePrices && !IsCancellationRequested)
            {
                total += BackfillSeries(
                    adapter, structure.TrendCurrentPrice, plan.FilterAfter,
                    () => apiClient.QueryDayAheadPrices(
                        area.EicCode, plan.Start, plan.End));
            }

            if (UpdateLoad && !IsCancellationRequested)
            {
                total += BackfillSeries(
                    adapter, structure.TrendLoadCurrent, plan.FilterAfter,
                    () => apiClient.QueryActualLoad(
                        area.EicCode, plan.Start, plan.End));
            }

            if (UpdateGeneration && !IsCancellationRequested)
            {
                total += BackfillSeries(
                    adapter, structure.TrendForecastTotal, plan.FilterAfter,
                    () => apiClient.QueryGenerationForecast(
                        area.EicCode, plan.Start, plan.End));
            }

            _rowsThisCycle += total;
            Logger.LogInfo(LogCategory.Processor,
                $"Backfill for {structure.ZoneCode} wrote {total} history records.");
        }

        // Runs a single backfill query, converts the points to history
        // records, and writes them to the trend. Failures are logged and
        // swallowed so one series cannot block the others.
        private int BackfillSeries(
            EwsServerDataAdapter adapter,
            string trendAlternateId,
            DateTime? filterAfter,
            Func<string> query)
        {
            try
            {
                var parsed = EntsoeResponseParser.Parse(query());
                var points = BackfillPlanner.ResampleHourly(
                    BackfillPlanner.FilterPoints(
                        parsed.GetAllPoints(), filterAfter));
                if (points.Count == 0) return 0;

                var samples = new List<Tuple<EwsValueStateEnum, DateTime, string>>();
                foreach (var point in points)
                {
                    samples.Add(Tuple.Create(
                        EwsValueStateEnum.Good,
                        point.Timestamp,
                        point.Value.ToString(
                            System.Globalization.CultureInfo.InvariantCulture)));
                }

                adapter.AddHistoryRecordsManually(trendAlternateId, samples);
                Logger.LogDebug(LogCategory.Processor,
                    $"Backfilled {samples.Count} records to {trendAlternateId}.");
                return samples.Count;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Logger.LogError(LogCategory.Processor,
                    $"Backfill failed for {trendAlternateId}: {ex.Message}");
                return 0;
            }
        }

        private static DateTime? ReadZoneLastUpdate(
            EwsServerDataAdapter adapter, ZoneStructure structure)
        {
            var item = adapter.ValueItems
                .FirstOrDefault(v => v.AlternateId == structure.MetaLastUpdate);

            if (item == null || string.IsNullOrEmpty(item.Value))
                return null;

            if (DateTime.TryParse(item.Value,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal
                    | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var dt)
                && dt > DateTime.MinValue)
            {
                return dt;
            }

            return null;
        }

        // ── Day-Ahead Prices ───────────────────────────────────────────

        private void FetchAndUpdatePrices(
            EwsServerDataAdapter adapter,
            EntsoeApiClient apiClient,
            AreaMapping.AreaInfo area,
            ZoneStructure structure,
            DateTime periodStart,
            DateTime periodEnd,
            DateTime currentUtc,
            List<Prompt> prompts)
        {
            try
            {
                string xml = apiClient.QueryDayAheadPrices(
                    area.EicCode, periodStart, periodEnd);

                var parsed = EntsoeResponseParser.Parse(xml);
                var points = parsed.GetAllPoints();
                _rowsThisCycle += points.Count;

                if (points.Count == 0)
                {
                    prompts.Add(new Prompt
                    {
                        Message = $"Day-ahead prices for "
                            + $"{structure.ZoneCode}: no data points.",
                        Severity = PromptSeverity.MayContinue
                    });
                    return;
                }

                var analysis = PriceAnalyzer.Analyze(
                    points, CheapPercentile, ExpensivePercentile,
                    currentUtc);

                string currency = "EUR";
                if (parsed.TimeSeries.Count > 0
                    && parsed.TimeSeries[0].Currency != null)
                    currency = parsed.TimeSeries[0].Currency;

                adapter.ModifyValueItemValue(
                    structure.PriceCurrentPrice,
                    analysis.CurrentPrice,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceNextHour,
                    analysis.NextHourPrice,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceCurrency,
                    currency,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceUnit,
                    "MWh",
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceDayMinimum,
                    analysis.DayMinimum,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceDayMaximum,
                    analysis.DayMaximum,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceDayAverage,
                    analysis.DayAverage,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceLevel,
                    (long)analysis.Level,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PricePriceCategory,
                    analysis.CurrentCategory.ToString(),
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceCheapThreshold,
                    analysis.CheapThreshold,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceExpensiveThreshold,
                    analysis.ExpensiveThreshold,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceRankToday,
                    (long)analysis.RankToday,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PricePercentileToday,
                    analysis.PercentileToday,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.PriceNegativeActive,
                    analysis.NegativeActive,
                    EwsValueStateEnum.Good);

                foreach (var hp in analysis.HourlyPrices)
                {
                    if (IsCancellationRequested) return;
                    if (hp.Hour >= 0 && hp.Hour < 24)
                    {
                        adapter.ModifyValueItemValue(
                            structure.HourlyPriceId(hp.Hour),
                            hp.Price,
                            EwsValueStateEnum.Good);
                    }
                }

                adapter.ModifyValueItemValue(
                    structure.ShiftIsCurrentlyCheap,
                    analysis.IsCurrentlyCheap,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.ShiftIsCurrentlyExpensive,
                    analysis.IsCurrentlyExpensive,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.ShiftHoursUntilCheap,
                    (long)analysis.HoursUntilCheap,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.ShiftHoursUntilExpensive,
                    (long)analysis.HoursUntilExpensive,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.ShiftCheapHoursToday,
                    analysis.CheapHoursToday,
                    EwsValueStateEnum.Good);
                adapter.ModifyValueItemValue(
                    structure.ShiftExpensiveHoursToday,
                    analysis.ExpensiveHoursToday,
                    EwsValueStateEnum.Good);

                Logger.LogInfo(LogCategory.Processor,
                    $"Prices for {structure.ZoneCode}: "
                    + $"current={analysis.CurrentPrice} {currency}/MWh "
                    + $"({analysis.CurrentCategory}), "
                    + $"range={analysis.DayMinimum}–"
                    + $"{analysis.DayMaximum}.");
            }
            catch (WebException ex)
            {
                Logger.LogError(LogCategory.Processor,
                    $"Failed to fetch prices for "
                    + $"{structure.ZoneCode}: {ex.Message}");
                prompts.Add(new Prompt
                {
                    Message = $"Price data for {structure.ZoneCode} "
                        + $"unavailable: {ex.Message}",
                    Severity = PromptSeverity.MayContinue
                });
            }
        }

        // ── Load Data ──────────────────────────────────────────────────

        private void FetchAndUpdateLoad(
            EwsServerDataAdapter adapter,
            EntsoeApiClient apiClient,
            AreaMapping.AreaInfo area,
            ZoneStructure structure,
            DateTime periodStart,
            DateTime periodEnd,
            List<Prompt> prompts)
        {
            // Current (actual) load — latest data point
            double currentLoad = 0;
            try
            {
                string xml = apiClient.QueryActualLoad(
                    area.EicCode, periodStart, periodEnd);

                var parsed = EntsoeResponseParser.Parse(xml);
                _rowsThisCycle += parsed.GetAllPoints().Count;
                var latest = parsed.GetLatestPoint();

                if (latest != null)
                {
                    currentLoad = latest.Value;
                    adapter.ModifyValueItemValue(
                        structure.LoadCurrent,
                        currentLoad,
                        EwsValueStateEnum.Good);
                    Logger.LogDebug(LogCategory.Processor,
                        $"Current load for {structure.ZoneCode}: "
                        + $"{currentLoad} MW");
                }
            }
            catch (WebException ex)
            {
                Logger.LogError(LogCategory.Processor,
                    $"Failed to fetch actual load for "
                    + $"{structure.ZoneCode}: {ex.Message}");
                prompts.Add(new Prompt
                {
                    Message = $"Actual load for {structure.ZoneCode} "
                        + $"unavailable: {ex.Message}",
                    Severity = PromptSeverity.MayContinue
                });
            }

            if (IsCancellationRequested) return;

            // Load forecast — derive today's peak, peak hour, and risk level
            try
            {
                string xml = apiClient.QueryLoadForecast(
                    area.EicCode, periodStart, periodEnd);

                var parsed = EntsoeResponseParser.Parse(xml);
                var points = parsed.GetAllPoints();
                _rowsThisCycle += points.Count;

                if (points.Count > 0)
                {
                    var (peakMw, peakHour) = LoadAnalyzer.FindForecastPeak(points);
                    int riskLevel = LoadAnalyzer.ComputePeakRiskLevel(
                        currentLoad, peakMw);

                    adapter.ModifyValueItemValue(
                        structure.LoadForecastPeakToday,
                        peakMw,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.LoadForecastPeakHour,
                        peakHour != DateTime.MinValue
                            ? (DateTime?)peakHour
                            : null,
                        peakHour != DateTime.MinValue
                            ? EwsValueStateEnum.Good
                            : EwsValueStateEnum.Uncertain);
                    adapter.ModifyValueItemValue(
                        structure.LoadPeakRiskLevel,
                        (long)riskLevel,
                        EwsValueStateEnum.Good);

                    Logger.LogDebug(LogCategory.Processor,
                        $"Load forecast for {structure.ZoneCode}: "
                        + $"peak={peakMw} MW at {peakHour:u}, risk={riskLevel}");
                }
            }
            catch (WebException ex)
            {
                Logger.LogError(LogCategory.Processor,
                    $"Failed to fetch load forecast for "
                    + $"{structure.ZoneCode}: {ex.Message}");
                prompts.Add(new Prompt
                {
                    Message = $"Load forecast for {structure.ZoneCode} "
                        + $"unavailable: {ex.Message}",
                    Severity = PromptSeverity.MayContinue
                });
            }
        }

        // ── Generation Data ────────────────────────────────────────────

        private const string PsrTypeSolar = "B16";
        private const string PsrTypeWindOffshore = "B18";
        private const string PsrTypeWindOnshore = "B19";

        private void FetchAndUpdateGeneration(
            EwsServerDataAdapter adapter,
            EntsoeApiClient apiClient,
            AreaMapping.AreaInfo area,
            ZoneStructure structure,
            DateTime periodStart,
            DateTime periodEnd,
            List<Prompt> prompts)
        {
            // Total generation forecast
            try
            {
                string xml = apiClient.QueryGenerationForecast(
                    area.EicCode, periodStart, periodEnd);

                var parsed = EntsoeResponseParser.Parse(xml);
                _rowsThisCycle += parsed.GetAllPoints().Count;
                var latest = parsed.GetLatestPoint();

                if (latest != null)
                {
                    adapter.ModifyValueItemValue(
                        structure.GenerationForecastTotal,
                        latest.Value,
                        EwsValueStateEnum.Good);
                    Logger.LogDebug(LogCategory.Processor,
                        $"Generation forecast for "
                        + $"{structure.ZoneCode}: {latest.Value} MW");
                }
            }
            catch (WebException ex)
            {
                Logger.LogError(LogCategory.Processor,
                    $"Failed to fetch generation forecast for "
                    + $"{structure.ZoneCode}: {ex.Message}");
                prompts.Add(new Prompt
                {
                    Message = $"Generation forecast for "
                        + $"{structure.ZoneCode} unavailable: "
                        + ex.Message,
                    Severity = PromptSeverity.MayContinue
                });
            }

            if (IsCancellationRequested) return;

            // Wind and solar forecasts (separate values from A69 response)
            try
            {
                string xml = apiClient.QueryWindSolarForecast(
                    area.EicCode, periodStart, periodEnd);

                var parsed = EntsoeResponseParser.Parse(xml);
                _rowsThisCycle += parsed.GetAllPoints().Count;

                // Wind = sum of onshore (B19) + offshore (B18)
                var windMw = parsed.GetLatestSumByPsrType(
                    PsrTypeWindOnshore, PsrTypeWindOffshore);
                if (windMw.HasValue)
                {
                    adapter.ModifyValueItemValue(
                        structure.GenerationWind,
                        windMw.Value,
                        EwsValueStateEnum.Good);
                    Logger.LogDebug(LogCategory.Processor,
                        $"Wind for {structure.ZoneCode}: {windMw.Value} MW");
                }

                // Solar (B16)
                var solarPoint = parsed.GetLatestPointByPsrType(PsrTypeSolar);
                if (solarPoint != null)
                {
                    adapter.ModifyValueItemValue(
                        structure.GenerationSolar,
                        solarPoint.Value,
                        EwsValueStateEnum.Good);
                    Logger.LogDebug(LogCategory.Processor,
                        $"Solar for {structure.ZoneCode}: {solarPoint.Value} MW");
                }

                // Renewable percent for load-shifting
                double totalRenewable = (windMw ?? 0) + (solarPoint?.Value ?? 0);
                var totalGen = parsed.GetLatestPoint();
                if (totalGen != null && totalGen.Value > 0)
                {
                    var renewablePct = (totalRenewable / totalGen.Value) * 100.0;
                    adapter.ModifyValueItemValue(
                        structure.ShiftRenewablePercent,
                        renewablePct,
                        EwsValueStateEnum.Good);
                }
            }
            catch (WebException ex)
            {
                Logger.LogError(LogCategory.Processor,
                    $"Failed to fetch wind/solar forecast for "
                    + $"{structure.ZoneCode}: {ex.Message}");
                prompts.Add(new Prompt
                {
                    Message = $"Wind/solar for {structure.ZoneCode} "
                        + $"unavailable: {ex.Message}",
                    Severity = PromptSeverity.MayContinue
                });
            }
        }
    }
}
