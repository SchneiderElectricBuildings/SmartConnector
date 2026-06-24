// ---------------------------------------------------------------------------
// Grid Status Processor
//
// Single processor that provisions and updates grid signal data in the
// EWS Server. Supports North American ISOs via GridStatus.io API.
// On each run:
//  1. Creates the EWS Server if it does not exist
//  2. Provisions the data hierarchy (idempotent, first run only)
//  3. Fetches data from GridStatus.io and updates EWS values
//
// Priority order for free-tier budget: prices first, then load, then fuel mix.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Data.Entity.Infrastructure;
using Ews.Common;
using Mongoose.Common;
using Mongoose.Common.Attributes;
using Mongoose.Ews.Server.Data;
using Mongoose.Ews.Server.Data.Shared;
using SxL.Common;

namespace SmartConnector.GridStatusExtension
{
    [ConfigurationDefaults("Grid Status Processor",
        "Provisions and updates grid signals (pricing, load, fuel mix, carbon) "
        + "from GridStatus.io for a configured North American ISO/RTO market.")]
    public class GridStatusProcessor : GridStatusProcessorBase
    {
        // -- Setup Properties ------------------------------------------------

        [DefaultValue("SmartConnectorRealm")]
        [Tooltip("HTTP Digest authentication realm for the EWS Server.")]
        public string Realm { get; set; }

        [Required]
        [DefaultValue("http://localhost:5300/GridStatus")]
        [Tooltip("HTTP endpoint address for the EWS Server.")]
        public string EwsAddress { get; set; }

        // -- Provisioning / Observability State ------------------------------

        private bool _provisioned;
        private long _cycleCount;
        private long _totalRowsConsumed;
        private long _rowsThisCycle;
        private string _resolvedPriceLocation;

        // -- Re-entrancy Guard -----------------------------------------------

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
            if (string.IsNullOrEmpty(key)) key = "GridStatus";
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
            if (string.IsNullOrEmpty(key)) key = "GridStatus";
            lock (ExecutionLock)
            {
                ExecutionsInProgress.Remove(key);
            }
        }

        // -- Execute ---------------------------------------------------------

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
            var guardKey = EffectiveServerName;

            if (!TryEnterExecution(guardKey))
            {
                Logger.LogInfo(LogCategory.Processor,
                    "A previous Grid Status execution is still running (likely a "
                    + "long backfill). Skipping this trigger to avoid overlap.");
                return prompts;
            }

            try
            {
                var adapter = DataAdapter;
                if (adapter == null)
                {
                    adapter = ConnectOrCreateServer();
                    _dataAdapter = adapter;
                }
                if (adapter == null)
                {
                    prompts.Add(new Prompt
                    {
                        Message = "Failed to connect to or create the EWS Server.",
                        Severity = PromptSeverity.MayNotContinue
                    });
                    return prompts;
                }
                ConfigureServer(adapter);

                var market = MarketRegistry.Lookup(Market);
                var structure = EwsStructure.ForIso(market.IsoId);
                _resolvedPriceLocation = GetEffectivePriceLocation(market);
                var now = DateTime.UtcNow;

                // ── Phase 1: Provision all EWS objects ──
                EnsureProvisioned(adapter, market, structure);
                if (IsCancellationRequested) return prompts;

                // Capture the backfill plan now, before Phase 4 stamps
                // Meta/LastUpdate — the decision must be based on the previous
                // cycle's timestamp, not this one's.
                var plan = BackfillPlanner.Create(
                    ReadIsoLastUpdate(adapter, structure), now, BackfillDays);

                _rowsThisCycle = 0;
                bool anySuccess = false;
                string lastError = "";

                using (var client = new GridStatusClient(ApiKey, ApiBaseUrl))
                {
                    // ── Phase 2: Update all point values ──
                    adapter.AutoCommit = false;

                    if (UpdatePrices && !IsCancellationRequested)
                    {
                        var result = FetchPrices(adapter, client, market, structure);
                        _rowsThisCycle += result.Rows;
                        if (result.Success) anySuccess = true;
                        if (!string.IsNullOrEmpty(result.Error)) lastError = result.Error;
                    }

                    if (UpdateLoad && !IsCancellationRequested)
                    {
                        var result = FetchLoad(adapter, client, market, structure);
                        _rowsThisCycle += result.Rows;
                        if (result.Success) anySuccess = true;
                        if (!string.IsNullOrEmpty(result.Error)) lastError = result.Error;
                    }

                    if (UpdateFuelMix && !IsCancellationRequested)
                    {
                        var result = FetchFuelMix(adapter, client, market, structure);
                        _rowsThisCycle += result.Rows;
                        if (result.Success) anySuccess = true;
                        if (!string.IsNullOrEmpty(result.Error)) lastError = result.Error;
                    }

                    adapter.CommitChanges();
                    adapter.AutoCommit = true;

                    // ── Phase 3: Add the current value to all history items ──
                    if (!IsCancellationRequested)
                        AddCurrentHistorySamples(adapter, structure, now);

                    // ── Phase 4: Update all metadata ──
                    UpdateMeta(adapter, structure, market, now, anySuccess, lastError);

                    Logger.LogInfo(LogCategory.Processor,
                        $"Cycle {_cycleCount}: {market.IsoId}, "
                        + $"rows={_rowsThisCycle}, healthy={anySuccess}.");

                    // ── Phase 5: Backfill trend history (long-running, last) ──
                    if (plan.ShouldBackfill && !IsCancellationRequested)
                    {
                        _rowsThisCycle += BackfillHistory(
                            adapter, client, market, structure, plan);
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Logger.LogError(LogCategory.Processor,
                    $"Processor failed: {ex.GetType().Name}: {ex.Message}");
                Logger.LogError(LogCategory.Processor,
                    $"Stack trace:\n{ex}");

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

        // -- Phase 3: Add current value to all history items -----------------

        // Writes the current value of each trended point as a history record
        // at the cycle timestamp. This guarantees every trend gains a sample
        // each cycle, independent of the EWS Server's change-driven auto-
        // sampling. The value is taken verbatim from the value item so the
        // string format always matches the trend's value type.
        private void AddCurrentHistorySamples(
            EwsServerDataAdapter adapter,
            IsoStructure structure,
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

        // -- Phase 4: Update metadata ----------------------------------------

        private void UpdateMeta(
            EwsServerDataAdapter adapter,
            IsoStructure structure,
            MarketConfig market,
            DateTime now,
            bool anySuccess,
            string lastError)
        {
            _cycleCount++;
            _totalRowsConsumed += _rowsThisCycle;
            var nextUpdate = DeriveNextUpdate(adapter, now);
            var marketTz = market.GetTimeZone();

            adapter.AutoCommit = false;

            // Per-ISO observability
            adapter.ModifyValueItemValue(
                structure.MetaLastUpdate, (DateTime?)now, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                structure.MetaNextUpdate, (DateTime?)nextUpdate, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                structure.MetaDataHealthy, anySuccess,
                anySuccess ? EwsValueStateEnum.Good : EwsValueStateEnum.Uncertain);
            adapter.ModifyValueItemValue(
                structure.MetaTimeZone, marketTz.Id, EwsValueStateEnum.Good);

            // Root-level observability
            adapter.ModifyValueItemValue(
                EwsStructure.MetaLastUpdate, (DateTime?)now, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaNextUpdate, (DateTime?)nextUpdate, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaDataHealthy, anySuccess,
                anySuccess ? EwsValueStateEnum.Good : EwsValueStateEnum.Uncertain);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaLastError, lastError,
                string.IsNullOrEmpty(lastError) ? EwsValueStateEnum.Good : EwsValueStateEnum.Uncertain);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaCycleCount, _cycleCount, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaRowsConsumed, _totalRowsConsumed, EwsValueStateEnum.Good);
            adapter.ModifyValueItemValue(
                EwsStructure.MetaTimeZone, TimeZoneInfo.Local.Id, EwsValueStateEnum.Good);

            adapter.CommitChanges();
            adapter.AutoCommit = true;
        }

        private static DateTime? ReadIsoLastUpdate(
            EwsServerDataAdapter adapter, IsoStructure structure)
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

        // -- EWS Server Connection -------------------------------------------

        /// <summary>
        /// Starts the EWS HTTP listener and ensures the server row exists
        /// in the database, then returns a clean adapter via ConnectExisting.
        ///
        /// ConnectNew defers the EwsServers row INSERT until the first
        /// SaveChanges. If we keep using that adapter for AddContainerItem
        /// calls, the deferred INSERT fires and hits a duplicate-key error
        /// when the server already exists from a prior run. The working
        /// pattern is:
        ///   1. ConnectNew  → starts HTTP listener, defers row INSERT
        ///   2. Dispose     → flushes the INSERT (or duplicate-key is caught)
        ///   3. ConnectExisting → returns adapter with clean DbContext
        /// </summary>
        private EwsServerDataAdapter ConnectOrCreateServer()
        {
            Logger.LogInfo(LogCategory.Processor,
                $"Starting EWS Server '{EffectiveServerName}' at {EwsAddress}.");

            try
            {
                using (EwsServerDataAdapter.ConnectNew(
                    name: EffectiveServerName,
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
                    $"Server '{EffectiveServerName}' row already exists; reusing.");
            }

            var adapter = EwsServerDataAdapter.ConnectExisting(
                EffectiveServerName, UserName, Password);
            Logger.LogInfo(LogCategory.Processor,
                "Connected to EWS Server.");
            return adapter;
        }

        /// <summary>
        /// Configures server parameters and supported methods. GetHistory and
        /// GetHistoricalDataAggregation must be set to true — without these
        /// the EWS server disables trend logging.
        /// </summary>
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

        // -- Provisioning helpers --------------------------------------------

        /// <summary>
        /// Returns an existing container or creates a new one. Idempotent.
        /// </summary>
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

        /// <summary>
        /// Returns an existing value item or creates a new one. Idempotent.
        /// </summary>
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

        /// <summary>
        /// Returns an existing history item or creates a new one. Idempotent.
        /// </summary>
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

        // -- Provisioning ----------------------------------------------------

        private void EnsureProvisioned(
            EwsServerDataAdapter adapter,
            MarketConfig market,
            IsoStructure structure)
        {
            if (_provisioned) return;

            Logger.LogInfo(LogCategory.Processor,
                $"Provisioning EWS structure for {market.DisplayName}.");

            // Root container: /Grid
            Logger.LogDebug(LogCategory.Processor, "Provision: creating container hierarchy.");
            var gridRoot = EnsureFolder(adapter,
                EwsStructure.Root, "Grid", null);

            // Root-level observability: /Grid/Meta
            var rootMeta = EnsureFolder(adapter, "/Grid/Meta", "Meta", gridRoot);
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

            // Per-ISO root: /Grid/{ISO}
            var isoRoot = EnsureFolder(adapter,
                structure.IsoRoot, market.IsoId, gridRoot);

            // Price container: /Grid/{ISO}/Price
            Logger.LogDebug(LogCategory.Processor, "Provision: creating Price items.");
            var priceFolder = EnsureFolder(adapter,
                $"{structure.IsoRoot}/Price", "Price", isoRoot);
            EnsureValue(adapter, structure.PriceRealTimeLmp, "RealTimeLMP",
                EwsValueTypeEnum.Double, priceFolder, "$/MWh");
            EnsureValue(adapter, structure.PriceCurrentPrice, "CurrentPrice",
                EwsValueTypeEnum.Double, priceFolder, "$/MWh");
            EnsureValue(adapter, structure.PriceNextHour, "NextHour",
                EwsValueTypeEnum.Double, priceFolder, "$/MWh");
            EnsureValue(adapter, structure.PriceLevel, "Level",
                EwsValueTypeEnum.Long, priceFolder);
            EnsureValue(adapter, structure.PricePriceCategory, "PriceCategory",
                EwsValueTypeEnum.String, priceFolder);
            EnsureValue(adapter, structure.PriceRankToday, "RankToday",
                EwsValueTypeEnum.Long, priceFolder);
            EnsureValue(adapter, structure.PricePercentileToday, "PercentileToday",
                EwsValueTypeEnum.Double, priceFolder, "%");
            EnsureValue(adapter, structure.PriceNegativeActive, "NegativeActive",
                EwsValueTypeEnum.Boolean, priceFolder);
            EnsureValue(adapter, structure.PriceCheapThreshold, "CheapThreshold",
                EwsValueTypeEnum.Double, priceFolder, "$/MWh");
            EnsureValue(adapter, structure.PriceExpensiveThreshold, "ExpensiveThreshold",
                EwsValueTypeEnum.Double, priceFolder, "$/MWh");
            EnsureValue(adapter, structure.PriceCurrency, "Currency",
                EwsValueTypeEnum.String, priceFolder);
            EnsureValue(adapter, structure.PriceUnit, "Unit",
                EwsValueTypeEnum.String, priceFolder);
            EnsureValue(adapter, structure.PriceDayMinimum, "DayMinimum",
                EwsValueTypeEnum.Double, priceFolder, "$/MWh");
            EnsureValue(adapter, structure.PriceDayMaximum, "DayMaximum",
                EwsValueTypeEnum.Double, priceFolder, "$/MWh");
            EnsureValue(adapter, structure.PriceDayAverage, "DayAverage",
                EwsValueTypeEnum.Double, priceFolder, "$/MWh");

            // Hourly prices container: /Grid/{ISO}/Price/Hourly
            var hourlyFolder = EnsureFolder(adapter,
                structure.HourlyPrices, "Hourly", priceFolder);
            for (int hour = 0; hour < 24; hour++)
            {
                EnsureValue(adapter, structure.HourlyPriceId(hour),
                    $"Hour{hour:D2}", EwsValueTypeEnum.Double, hourlyFolder, "$/MWh");
            }

            // LoadShifting container: /Grid/{ISO}/LoadShifting
            Logger.LogDebug(LogCategory.Processor, "Provision: creating LoadShifting items.");
            var loadShiftingFolder = EnsureFolder(adapter,
                structure.LoadShiftingRoot, "LoadShifting", isoRoot);
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

            // Load container: /Grid/{ISO}/Load
            Logger.LogDebug(LogCategory.Processor, "Provision: creating Load items.");
            var loadFolder = EnsureFolder(adapter,
                $"{structure.IsoRoot}/Load", "Load", isoRoot);
            EnsureValue(adapter, structure.LoadCurrent, "Current",
                EwsValueTypeEnum.Double, loadFolder, "MW");
            EnsureValue(adapter, structure.LoadForecastPeakToday, "ForecastPeakToday",
                EwsValueTypeEnum.Double, loadFolder, "MW");
            EnsureValue(adapter, structure.LoadForecastPeakHour, "ForecastPeakHour",
                EwsValueTypeEnum.DateTime, loadFolder);
            EnsureValue(adapter, structure.LoadPeakRiskLevel, "PeakRiskLevel",
                EwsValueTypeEnum.Long, loadFolder);

            // FuelMix container: /Grid/{ISO}/FuelMix
            Logger.LogDebug(LogCategory.Processor, "Provision: creating FuelMix items.");
            var fuelMixFolder = EnsureFolder(adapter,
                $"{structure.IsoRoot}/FuelMix", "FuelMix", isoRoot);
            EnsureValue(adapter, structure.FuelMixRenewablePct, "RenewablePct",
                EwsValueTypeEnum.Double, fuelMixFolder, "%");
            EnsureValue(adapter, structure.FuelMixSolar, "Solar",
                EwsValueTypeEnum.Double, fuelMixFolder, "MW");
            EnsureValue(adapter, structure.FuelMixWind, "Wind",
                EwsValueTypeEnum.Double, fuelMixFolder, "MW");
            EnsureValue(adapter, structure.FuelMixGas, "Gas",
                EwsValueTypeEnum.Double, fuelMixFolder, "MW");

            // Carbon container: /Grid/{ISO}/Carbon
            Logger.LogDebug(LogCategory.Processor, "Provision: creating Carbon items.");
            var carbonFolder = EnsureFolder(adapter,
                $"{structure.IsoRoot}/Carbon", "Carbon", isoRoot);
            EnsureValue(adapter, structure.CarbonGridIntensity, "GridIntensity",
                EwsValueTypeEnum.Double, carbonFolder, "gCO2/kWh");
            EnsureValue(adapter, structure.CarbonLevel, "Level",
                EwsValueTypeEnum.Long, carbonFolder);

            // Per-ISO metadata: /Grid/{ISO}/Meta
            Logger.LogDebug(LogCategory.Processor, "Provision: creating per-ISO Meta items.");
            var isoMeta = EnsureFolder(adapter,
                $"{structure.IsoRoot}/Meta", "Meta", isoRoot);
            EnsureValue(adapter, structure.MetaLastUpdate, "LastUpdate",
                EwsValueTypeEnum.DateTime, isoMeta);
            EnsureValue(adapter, structure.MetaNextUpdate, "NextUpdate",
                EwsValueTypeEnum.DateTime, isoMeta);
            EnsureValue(adapter, structure.MetaDataHealthy, "DataHealthy",
                EwsValueTypeEnum.Boolean, isoMeta);
            EnsureValue(adapter, structure.MetaTimeZone, "TimeZone",
                EwsValueTypeEnum.String, isoMeta);

            // Trend logs — linked to the value items created above
            Logger.LogDebug(LogCategory.Processor, "Provision: creating trend logs.");
            foreach (var trend in structure.TrendDefinitions)
            {
                if (IsCancellationRequested) return;
                EnsureHistoryItem(adapter,
                    trend.TrendId, trend.Name, trend.ValueId);
            }

            _provisioned = true;

            Logger.LogInfo(LogCategory.Processor,
                $"Provisioned {structure.TrendDefinitions.Length} trend logs "
                + $"for {market.IsoId}.");
        }

        // -- Gap Backfill ----------------------------------------------------

        // The backfill query must be allowed to return more rows than the
        // live RowLimit (which conserves budget on small per-cycle queries):
        // an initial 30-day hourly window is ~720 rows per dataset.
        private int BackfillRowLimit =>
            Math.Min(Math.Max(BackfillDays, 1) * 24 + 48, 10000);

        /// <summary>
        /// Backfills trend log history for the planned window. Uses
        /// AddHistoryRecordsManually to write samples at the original
        /// timestamps from GridStatus.io, hourly-resampled. On first run the
        /// window is BackfillDays; after a reconnection gap only the missed
        /// period is fetched. Idempotent across restarts because the window is
        /// derived from the persisted Meta/LastUpdate timestamp.
        /// </summary>
        private int BackfillHistory(
            EwsServerDataAdapter adapter,
            GridStatusClient client,
            MarketConfig market,
            IsoStructure structure,
            BackfillPlanner.Plan plan)
        {
            int totalRows = 0;

            Logger.LogInfo(LogCategory.Processor,
                $"Backfilling history for {market.IsoId} "
                + $"from {plan.Start:u} to {plan.End:u}.");

            try
            {
                // Backfill real-time prices (hourly resampled)
                if (UpdatePrices)
                {
                    totalRows += BackfillDataset(
                        adapter, client,
                        market.RealTimePriceDataset,
                        structure.TrendRealTimeLmp,
                        market.PriceColumn,
                        plan.Start, plan.End, plan.FilterAfter,
                        market.PriceFilterColumn, _resolvedPriceLocation,
                        resampleFrequency: "1 hour");
                }

                if (IsCancellationRequested) return totalRows;

                // Backfill load
                if (UpdateLoad)
                {
                    totalRows += BackfillDataset(
                        adapter, client,
                        market.LoadDataset,
                        structure.TrendLoadCurrent,
                        "load",
                        plan.Start, plan.End, plan.FilterAfter,
                        resampleFrequency: "1 hour");
                }

                if (IsCancellationRequested) return totalRows;

                // Backfill fuel mix (hourly resampled)
                if (UpdateFuelMix)
                {
                    totalRows += BackfillFuelMix(
                        adapter, client, market, structure,
                        plan.Start, plan.End, plan.FilterAfter);
                }

                Logger.LogInfo(LogCategory.Processor,
                    $"Backfill complete: {totalRows} rows for "
                    + $"{(plan.End - plan.Start).TotalHours:F1} hours.");
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Logger.LogError(LogCategory.Processor,
                    $"Backfill failed (partial data may exist): {ex.Message}");
            }

            return totalRows;
        }

        /// <summary>
        /// Backfills a single dataset's values into its trend log using
        /// AddHistoryRecordsManually with original timestamps.
        /// </summary>
        private int BackfillDataset(
            EwsServerDataAdapter adapter,
            GridStatusClient client,
            string datasetId,
            string trendAlternateId,
            string valueColumn,
            DateTime startUtc,
            DateTime endUtc,
            DateTime? filterAfter,
            string filterColumn = null,
            string filterValue = null,
            string resampleFrequency = null)
        {
            string json = client.QueryDataset(
                datasetId,
                startTimeUtc: startUtc,
                endTimeUtc: endUtc,
                filterColumn: filterColumn,
                filterValue: filterValue,
                columns: $"interval_start_utc,{valueColumn}",
                limit: BackfillRowLimit,
                resampleFrequency: resampleFrequency);

            var data = GridStatusResponseParser.Parse(json);
            var rows = BackfillPlanner.FilterRows(data.Rows, filterAfter);
            if (rows.Count == 0)
                return 0;

            var samples = new List<Tuple<EwsValueStateEnum, DateTime, string>>();
            foreach (var row in rows)
            {
                double value = row.GetDouble(valueColumn);
                samples.Add(Tuple.Create(
                    EwsValueStateEnum.Good,
                    row.IntervalStartUtc,
                    value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            adapter.AddHistoryRecordsManually(trendAlternateId, samples);
            Logger.LogDebug(LogCategory.Processor,
                $"Backfilled {samples.Count} samples to {trendAlternateId}");

            return samples.Count;
        }

        /// <summary>
        /// Backfills fuel mix history: solar, wind, gas, renewable pct,
        /// and grid intensity.
        /// </summary>
        private int BackfillFuelMix(
            EwsServerDataAdapter adapter,
            GridStatusClient client,
            MarketConfig market,
            IsoStructure structure,
            DateTime startUtc,
            DateTime endUtc,
            DateTime? filterAfter)
        {
            string json = client.QueryDataset(
                market.FuelMixDataset,
                startTimeUtc: startUtc,
                endTimeUtc: endUtc,
                limit: BackfillRowLimit,
                resampleFrequency: "1 hour");

            var data = GridStatusResponseParser.Parse(json);
            var rows = BackfillPlanner.FilterRows(data.Rows, filterAfter);
            if (rows.Count == 0)
                return 0;

            var solarSamples = new List<Tuple<EwsValueStateEnum, DateTime, string>>();
            var windSamples = new List<Tuple<EwsValueStateEnum, DateTime, string>>();
            var gasSamples = new List<Tuple<EwsValueStateEnum, DateTime, string>>();
            var renewableSamples = new List<Tuple<EwsValueStateEnum, DateTime, string>>();
            var intensitySamples = new List<Tuple<EwsValueStateEnum, DateTime, string>>();

            foreach (var row in rows)
            {
                var ts = row.IntervalStartUtc;

                // Build fuel mix from row
                var fuelMix = new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in row.Columns)
                {
                    if (kvp.Key.StartsWith("interval_", StringComparison.OrdinalIgnoreCase))
                        continue;
                    double mw = row.GetDouble(kvp.Key);
                    if (mw > 0)
                        fuelMix[kvp.Key] = mw;
                }

                double solar = GetFuelValue(fuelMix, "solar");
                double wind = GetFuelValue(fuelMix, "wind");
                double gas = GetFuelValue(fuelMix, "gas", "natural_gas");
                double renewPct = SignalDerivation.ComputeRenewablePct(fuelMix);
                double intensity = SignalDerivation.ComputeGridIntensity(fuelMix);

                var inv = System.Globalization.CultureInfo.InvariantCulture;
                solarSamples.Add(Tuple.Create(EwsValueStateEnum.Good, ts, solar.ToString(inv)));
                windSamples.Add(Tuple.Create(EwsValueStateEnum.Good, ts, wind.ToString(inv)));
                gasSamples.Add(Tuple.Create(EwsValueStateEnum.Good, ts, gas.ToString(inv)));
                renewableSamples.Add(Tuple.Create(EwsValueStateEnum.Good, ts, renewPct.ToString(inv)));
                intensitySamples.Add(Tuple.Create(EwsValueStateEnum.Good, ts, intensity.ToString(inv)));
            }

            if (solarSamples.Count > 0)
            {
                adapter.AddHistoryRecordsManually(structure.TrendFuelMixSolar, solarSamples);
                adapter.AddHistoryRecordsManually(structure.TrendFuelMixWind, windSamples);
                adapter.AddHistoryRecordsManually(structure.TrendFuelMixGas, gasSamples);
                adapter.AddHistoryRecordsManually(structure.TrendFuelMixRenewablePct, renewableSamples);
                adapter.AddHistoryRecordsManually(structure.TrendCarbonGridIntensity, intensitySamples);

                Logger.LogDebug(LogCategory.Processor,
                    $"Backfilled {solarSamples.Count} fuel mix samples.");
            }

            return solarSamples.Count;
        }

        // -- Data Fetch Results ----------------------------------------------

        private struct FetchResult
        {
            public bool Success;
            public int Rows;
            public string Error;
        }

        // -- Price Fetch -----------------------------------------------------

        private FetchResult FetchPrices(
            EwsServerDataAdapter adapter,
            GridStatusClient client,
            MarketConfig market,
            IsoStructure structure)
        {
            var result = new FetchResult();

            try
            {
                // Real-time price: latest single row
                string rtJson = client.QueryDataset(
                    market.RealTimePriceDataset,
                    filterColumn: market.PriceFilterColumn,
                    filterValue: _resolvedPriceLocation,
                    columns: $"interval_start_utc,{market.PriceColumn}",
                    limit: 1,
                    time: "latest");

                var rtData = GridStatusResponseParser.Parse(rtJson);
                result.Rows += rtData.RowCount;

                double rtLmp = 0;
                if (rtData.Rows.Count > 0)
                {
                    rtLmp = rtData.Rows[0].GetDouble(market.PriceColumn);
                    adapter.ModifyValueItemValue(
                        structure.PriceRealTimeLmp, rtLmp,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceNegativeActive, rtLmp < 0,
                        EwsValueStateEnum.Good);
                }

                if (IsCancellationRequested) return result;

                // Day-ahead prices for today (24 rows max)
                var tz = market.GetTimeZone();
                var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                var todayStart = TimeZoneInfo.ConvertTimeToUtc(localNow.Date, tz);
                var todayEnd = TimeZoneInfo.ConvertTimeToUtc(localNow.Date.AddDays(1), tz);

                string daJson = client.QueryDataset(
                    market.DayAheadPriceDataset,
                    startTimeUtc: todayStart,
                    endTimeUtc: todayEnd,
                    filterColumn: market.PriceFilterColumn,
                    filterValue: _resolvedPriceLocation,
                    columns: $"interval_start_utc,{market.PriceColumn}",
                    limit: 48);

                var daData = GridStatusResponseParser.Parse(daJson);
                result.Rows += daData.RowCount;

                if (daData.Rows.Count > 0)
                {
                    var prices = daData.Rows.Select(r => r.GetDouble(market.PriceColumn)).ToList();
                    int currentHour = localNow.Hour;
                    int nextHour = (currentHour + 1) % 24;

                    double currentHourPrice = prices.Count > currentHour
                        ? prices[currentHour] : (prices.Count > 0 ? prices.Last() : 0);
                    double nextHourPrice = prices.Count > nextHour
                        ? prices[nextHour] : currentHourPrice;

                    int level = SignalDerivation.ComputePriceLevel(
                        currentHourPrice, prices, CheapPercentile, ExpensivePercentile);
                    int rank = SignalDerivation.ComputeRankToday(currentHourPrice, prices);
                    double pct = SignalDerivation.ComputePercentileToday(currentHourPrice, prices);

                    // Compute thresholds for standard interface
                    double cheapThreshold = SignalDerivation.ComputePercentileValue(
                        prices, CheapPercentile);
                    double expensiveThreshold = SignalDerivation.ComputePercentileValue(
                        prices, ExpensivePercentile);
                    string priceCategory = level == 0 ? "Cheap"
                        : level == 2 ? "Expensive" : "Normal";

                    adapter.ModifyValueItemValue(
                        structure.PriceCurrentPrice, currentHourPrice,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceNextHour, nextHourPrice,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceLevel, (long)level,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PricePriceCategory, priceCategory,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceRankToday, (long)rank,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PricePercentileToday, pct,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceCheapThreshold, Math.Round(cheapThreshold, 2),
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceExpensiveThreshold, Math.Round(expensiveThreshold, 2),
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceCurrency, "USD",
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceUnit, "MWh",
                        EwsValueStateEnum.Good);

                    // Hourly prices
                    for (int h = 0; h < prices.Count && h < 24; h++)
                    {
                        adapter.ModifyValueItemValue(
                            structure.HourlyPriceId(h), prices[h],
                            EwsValueStateEnum.Good);
                    }

                    // Day summary statistics
                    var (dayMin, dayMax, dayAvg) = SignalDerivation.ComputeDaySummary(prices);
                    adapter.ModifyValueItemValue(
                        structure.PriceDayMinimum, dayMin,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceDayMaximum, dayMax,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.PriceDayAverage, dayAvg,
                        EwsValueStateEnum.Good);

                    // Load shifting signals
                    double renewablePct = 0;
                    var existingRenewable = adapter.ValueItems
                        .FirstOrDefault(v => v.AlternateId == structure.FuelMixRenewablePct);
                    if (existingRenewable != null)
                        double.TryParse(existingRenewable.Value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out renewablePct);

                    var shifting = SignalDerivation.ComputeLoadShifting(
                        prices, currentHour, CheapPercentile, ExpensivePercentile,
                        renewablePct);

                    adapter.ModifyValueItemValue(
                        structure.ShiftIsCurrentlyCheap, shifting.IsCurrentlyCheap,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.ShiftIsCurrentlyExpensive, shifting.IsCurrentlyExpensive,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.ShiftHoursUntilCheap, (long)shifting.HoursUntilCheap,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.ShiftHoursUntilExpensive, (long)shifting.HoursUntilExpensive,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.ShiftCheapHoursToday, shifting.CheapHoursToday,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.ShiftExpensiveHoursToday, shifting.ExpensiveHoursToday,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.ShiftRenewablePercent, shifting.RenewablePercent,
                        EwsValueStateEnum.Good);

                    Logger.LogInfo(LogCategory.Processor,
                        $"Prices for {market.IsoId}: RT={rtLmp:F2}, "
                        + $"DA={currentHourPrice:F2} $/MWh ({priceCategory}), "
                        + $"level={level}, rank={rank}/{prices.Count}, "
                        + $"range={dayMin:F2}–{dayMax:F2}, "
                        + $"cheap={shifting.IsCurrentlyCheap}");
                }

                result.Success = true;
            }
            catch (GridStatusApiException ex) when ((int)ex.StatusCode == 401 || (int)ex.StatusCode == 403)
            {
                result.Error = $"Price auth error: {ex.Message}";
                Logger.LogError(LogCategory.Processor, result.Error);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                result.Error = $"Price fetch failed: {ex.Message}";
                Logger.LogError(LogCategory.Processor, result.Error);
            }

            return result;
        }

        // -- Load Fetch ------------------------------------------------------

        private FetchResult FetchLoad(
            EwsServerDataAdapter adapter,
            GridStatusClient client,
            MarketConfig market,
            IsoStructure structure)
        {
            var result = new FetchResult();

            try
            {
                // Current load: latest row
                string loadJson = client.QueryDataset(
                    market.LoadDataset,
                    columns: "interval_start_utc,load",
                    limit: 1,
                    time: "latest");

                var loadData = GridStatusResponseParser.Parse(loadJson);
                result.Rows += loadData.RowCount;

                double currentLoad = 0;
                if (loadData.Rows.Count > 0)
                {
                    currentLoad = loadData.Rows[0].GetDouble("load");
                    adapter.ModifyValueItemValue(
                        structure.LoadCurrent, currentLoad,
                        EwsValueStateEnum.Good);
                }

                if (IsCancellationRequested) return result;

                // Load forecast for today
                var tz = market.GetTimeZone();
                var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                var todayStart = TimeZoneInfo.ConvertTimeToUtc(localNow.Date, tz);
                var todayEnd = TimeZoneInfo.ConvertTimeToUtc(localNow.Date.AddDays(1), tz);

                string fcJson = client.QueryDataset(
                    market.LoadForecastDataset,
                    startTimeUtc: todayStart,
                    endTimeUtc: todayEnd,
                    columns: "interval_start_utc,load_forecast",
                    limit: RowLimit);

                var fcData = GridStatusResponseParser.Parse(fcJson);
                result.Rows += fcData.RowCount;

                if (fcData.Rows.Count > 0)
                {
                    var (peakMw, peakHour) = SignalDerivation.FindForecastPeak(
                        fcData.Rows, "load_forecast");
                    int riskLevel = SignalDerivation.ComputePeakRiskLevel(
                        currentLoad, peakMw,
                        PeakRiskMediumThreshold, PeakRiskHighThreshold);

                    adapter.ModifyValueItemValue(
                        structure.LoadForecastPeakToday, peakMw,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.LoadForecastPeakHour,
                        peakHour != DateTime.MinValue ? (DateTime?)peakHour : null,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.LoadPeakRiskLevel, (long)riskLevel,
                        EwsValueStateEnum.Good);

                    Logger.LogInfo(LogCategory.Processor,
                        $"Load for {market.IsoId}: current={currentLoad:F0} MW, "
                        + $"peak={peakMw:F0} MW, risk={riskLevel}");
                }

                result.Success = true;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                result.Error = $"Load fetch failed: {ex.Message}";
                Logger.LogError(LogCategory.Processor, result.Error);
            }

            return result;
        }

        // -- Fuel Mix Fetch --------------------------------------------------

        private FetchResult FetchFuelMix(
            EwsServerDataAdapter adapter,
            GridStatusClient client,
            MarketConfig market,
            IsoStructure structure)
        {
            var result = new FetchResult();

            try
            {
                string json = client.QueryDataset(
                    market.FuelMixDataset,
                    limit: 1,
                    time: "latest");

                var data = GridStatusResponseParser.Parse(json);
                result.Rows += data.RowCount;

                if (data.Rows.Count > 0)
                {
                    var row = data.Rows[0];
                    var fuelMix = new Dictionary<string, double>(
                        StringComparer.OrdinalIgnoreCase);

                    // Extract all numeric columns as fuel types
                    foreach (var kvp in row.Columns)
                    {
                        if (kvp.Key.StartsWith("interval_", StringComparison.OrdinalIgnoreCase))
                            continue;

                        double mw = row.GetDouble(kvp.Key);
                        if (mw > 0)
                            fuelMix[kvp.Key] = mw;
                    }

                    double solarMw = GetFuelValue(fuelMix, "solar");
                    double windMw = GetFuelValue(fuelMix, "wind");
                    double gasMw = GetFuelValue(fuelMix, "gas", "natural_gas");

                    double renewablePct = SignalDerivation.ComputeRenewablePct(fuelMix);
                    double intensity = SignalDerivation.ComputeGridIntensity(fuelMix);
                    int carbonLevel = SignalDerivation.ComputeCarbonLevel(
                        intensity, CarbonCleanThreshold, CarbonDirtyThreshold);

                    adapter.ModifyValueItemValue(
                        structure.FuelMixRenewablePct, renewablePct,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.CarbonGridIntensity, intensity,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.CarbonLevel, (long)carbonLevel,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.FuelMixSolar, solarMw,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.FuelMixWind, windMw,
                        EwsValueStateEnum.Good);
                    adapter.ModifyValueItemValue(
                        structure.FuelMixGas, gasMw,
                        EwsValueStateEnum.Good);

                    Logger.LogInfo(LogCategory.Processor,
                        $"FuelMix for {market.IsoId}: "
                        + $"renewable={renewablePct:F1}%, "
                        + $"intensity={intensity:F0} gCO2/kWh, "
                        + $"carbon={carbonLevel}");
                }

                result.Success = true;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                result.Error = $"FuelMix fetch failed: {ex.Message}";
                Logger.LogError(LogCategory.Processor, result.Error);
            }

            return result;
        }

        private static double GetFuelValue(
            IDictionary<string, double> mix, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (mix.TryGetValue(key, out double val))
                    return val;
            }
            return 0.0;
        }
    }
}
