# EcoStruxure™ Smart Connector How-To Guides

<!-- Document: EcoStruxure™ Smart Connector How-To Guides -->
<!-- Applies to: Smart Connector SDK v2.6 -->
<!-- Revision: 1 -->
<!-- Date: June 2026 -->
<!-- Author: Karim Hussain -->
<!-- Audience: Extension developers — system integrators, third-party developers, building automation engineers -->

Task-oriented recipes for common Smart Connector development scenarios. Each recipe is self-contained — jump to the one you need.

For full API reference, see the [Developer Guide](developer-guide/smartconnector-developers-guide.md). For REST endpoint details, see the [RESTful EWS Gateway Guide](developer-guide/smartconnector-restful-ews-gateway.md).

---

## Table of Contents

1. [Write Values in Bulk with AutoCommit](#1-write-values-in-bulk-with-autocommit)
2. [Schedule a Processor with a Cron Expression](#2-schedule-a-processor-with-a-cron-expression)
3. [Read Values from an External EWS Server](#3-read-values-from-an-external-ews-server)
4. [Conditionally Show Configuration Parameters](#4-conditionally-show-configuration-parameters)
5. [Paginate REST API Results Beyond the Take Limit](#5-paginate-rest-api-results-beyond-the-take-limit)
6. [Authenticate and Call the REST API](#6-authenticate-and-call-the-rest-api)
7. [Forward Logs to a Syslog Server](#7-forward-logs-to-a-syslog-server)
8. [Dispose Resources Correctly in a Processor](#8-dispose-resources-correctly-in-a-processor)
9. [Provision an EWS Server Programmatically](#9-provision-an-ews-server-programmatically)
10. [Share Data Between Processors](#10-share-data-between-processors)
11. [Populate Trend Logs with Per-Cycle Samples and Backfill](#11-populate-trend-logs-with-per-cycle-samples-and-backfill)
12. [Run a Self-Provisioning Processor Safely on a Schedule](#12-run-a-self-provisioning-processor-safely-on-a-schedule)

---

## 1 Write Values in Bulk with AutoCommit

**Problem:** Your Processor updates hundreds or thousands of values per cycle and performance is slow.

**Solution:** Disable `AutoCommit`, apply all updates, then commit in a single database transaction.

```csharp
protected override IEnumerable<Prompt> Execute_Subclass()
{
    var prompts = new List<Prompt>();

    using (var adapter = EwsServerDataAdapter.ConnectExisting(ServerName, UserName, Password))
    {
        adapter.AutoCommit = false;

        foreach (var reading in GetLatestReadings())
        {
            adapter.ModifyValueItemValue(
                reading.AlternateId,
                reading.Value,
                EwsValueStateEnum.Good);
        }

        adapter.CommitChanges();
        adapter.AutoCommit = true;
    }

    return prompts;
}
```

**Why it works:** With `AutoCommit = true` (the default), every `ModifyValueItemValue` call issues a separate database write. Disabling it batches all changes into a single transaction at `CommitChanges()`, delivering up to 10× faster processing.

**Reference:** [Developer Guide — EwsServerDataAdapter](developer-guide/smartconnector-developers-guide.md#114-ewsserverdataadapter)

---

## 2 Schedule a Processor with a Cron Expression

**Problem:** You need a Processor to run at specific times (e.g., every weekday at 06:00) rather than on a fixed interval.

**Solution:** Use a cron expression in the schedule configuration.

| Expression | Meaning |
|------------|---------|
| `0 */5 * * *` | Every 5 minutes |
| `0 0 6 * * 1-5` | Weekdays at 06:00 |
| `0 30 */2 * *` | Every 2 hours at the half-hour mark |
| `0 0 0 1 * *` | Midnight on the first day of each month |

Cron scheduling uses the [NCrontab](https://github.com/atifaziz/NCrontab) library. Six-part expressions (with seconds) are supported.

Configure the schedule in the Smart Connector Portal under **Setup → Configuration Schedules → Add New**, selecting the cron expression option.

**Reference:** [Developer Guide — Cron Scheduling](developer-guide/smartconnector-developers-guide.md#1112-cron-scheduling)

---

## 3 Read Values from an External EWS Server

**Problem:** Your Processor needs to read current values from an EcoStruxure™ Building Operation or other EWS server.

**Solution:** Use `ValueItemReader` — set the address, credentials, and the points to read (by `Id`), then call `ReadData()`. The reader handles the EWS connection and paging internally:

```csharp
protected override IEnumerable<Prompt> Execute_Subclass()
{
    var reader = new ValueItemReader
    {
        Address = EwsServerAddress,
        UserName = UserName,
        Password = Password,
        DataPointsToRead = new List<DataPoint>
        {
            new DataPoint { Id = "Building1/Floor2/RoomTemp" }
        }
    };

    ReadResult<DataPointValueResultItem> result = reader.ReadData();
    foreach (var item in result.DataRead)
    {
        Logger.LogDebug(LogCategory.Processor,
            $"{item.Id} = {item.Value} ({item.ValueType})");
    }

    return new List<Prompt>();
}
```

`AlarmItemReader`, `HistoryItemReader`, and `SubscriptionReader` follow the same shape — set their properties, then call `ReadData()`.

**Available readers:**

| Class | Data type | Use case |
|-------|-----------|----------|
| `ValueItemReader` | Current values | Real-time point data |
| `AlarmItemReader` | Active alarms | Alarm monitoring |
| `HistoryItemReader` | Trend history | Historical data retrieval |
| `SubscriptionReader` | Change notifications | Event-driven updates |

**Reference:** [Developer Guide — Retrieving Data from an EWS Server](developer-guide/smartconnector-developers-guide.md#61-retrieving-data-from-an-ews-server)

---

## 4 Conditionally Show Configuration Parameters

**Problem:** Your Processor has parameters that are only relevant when another parameter has a specific value (e.g., show proxy settings only when a proxy is enabled).

**Solution:** Use the `[VisibleWhen]` attribute:

```csharp
[Required]
[Tooltip("Enable HTTP proxy for outbound connections.")]
public string UseProxy { get; set; } = "No";

[VisibleWhen("UseProxy", new[] { "Yes" })]
[Tooltip("Proxy server address (e.g. http://proxy.example.com:8080).")]
public string ProxyAddress { get; set; }

[VisibleWhen("UseProxy", new[] { "Yes" })]
[EncryptedString]
[Tooltip("Proxy authentication password.")]
public string ProxyPassword { get; set; }
```

**Rules:**

- `VisibleWhen` takes the property name and an array of values that make the decorated property visible.
- Supports `AllowMultiple = true` for OR logic across multiple conditions.
- Works on both `Parameter` and `ParameterSet` classes.

**Reference:** [Developer Guide — Smart Connector Attributes](developer-guide/smartconnector-developers-guide.md#113-smart-connector-attributes)

---

## 5 Paginate REST API Results Beyond the Take Limit

**Problem:** You need to retrieve more than 5,000 items from a REST endpoint.

**Solution:** Use `skip` and `take` query parameters to paginate:

```bash
# Page 1: items 0–999
curl -H "Authorization: Bearer TOKEN" \
  "https://localhost:5000/Values?skip=0&take=1000"

# Page 2: items 1000–1999
curl -H "Authorization: Bearer TOKEN" \
  "https://localhost:5000/Values?skip=1000&take=1000"
```

**Implementation pattern (C#):**

```csharp
var allValues = new List<ValueModel>();
int skip = 0;
const int pageSize = 1000;

while (true)
{
    var page = await client.GetAsync($"Values?skip={skip}&take={pageSize}");
    var items = JsonConvert.DeserializeObject<List<ValueModel>>(await page.Content.ReadAsStringAsync());

    if (items == null || items.Count == 0)
        break;

    allValues.AddRange(items);
    skip += pageSize;
}
```

> **Note:** The maximum `take` value is 5,000. Requests exceeding this limit are silently capped.

**Reference:** [EWS Gateway Guide — Filtering, Sorting, and Paging](developer-guide/smartconnector-restful-ews-gateway.md#66-filtering-sorting-and-paging)

---

## 6 Authenticate and Call the REST API

**Problem:** You need to acquire a bearer token and make authenticated REST API calls.

**Solution:**

### Step 1 — Acquire a bearer token

```bash
curl -X POST "https://localhost:5000/GetToken" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=YOUR_USER&password=YOUR_PASSWORD"
```

Response:

```json
{
  "access_token": "eyJhbGciOi...",
  "token_type": "bearer",
  "expires_in": 299
}
```

> **Note:** The token endpoint path (`GetToken` by default) and the token lifetime (`expires_in`, default 299 seconds ≈ 5 minutes) are configurable per endpoint in the Portal. Use the values configured for your endpoint.

### Step 2 — Use the token

```bash
curl -H "Authorization: Bearer eyJhbGciOi..." \
  "https://localhost:5000/AlarmEvents?take=10"
```

> **Note:** Which resources resolve depends on the data provider. The bulk collection GETs (`/Containers`, `/Values`, `/Alarms`, `/Trends`) return `405` on **every** provider — browse with `/Containers/{id}/Children` against a native Smart Connector EWS Server, or, against EBO via `SoapEwsRestProvider`, read `/AlarmEvents`, `/AlarmEventTypes`, and `/Subscriptions` and navigate by known IDs (`GET /Root` is not available). See [Choosing a Data Provider](developer-guide/smartconnector-restful-ews-gateway.md#331-choosing-a-data-provider).

### Step 3 — Renew before expiry

Track `expires_in` and re-authenticate before the token expires. There is no refresh token endpoint — request a new token using the same credentials.

**Reference:** [EWS Gateway Guide — Authentication](developer-guide/smartconnector-restful-ews-gateway.md#5-authentication)

---

## 7 Forward Logs to a Syslog Server

**Problem:** You need Smart Connector logs in a centralised log management system (Splunk, Graylog, Sentinel).

**Solution:** Configure syslog in the Smart Connector configuration file:

1. Open the Smart Connector configuration file (typically in the installation directory).
2. Add the syslog target configuration with the server address, port, and protocol (UDP or TCP).
3. Restart the Smart Connector service.

All log output — including entries from your extension's `Logger` calls — will be forwarded to the syslog target.

**Reference:** [Developer Guide — Syslog Logging](developer-guide/smartconnector-developers-guide.md#1116-syslog-logging)

---

## 8 Dispose Resources Correctly in a Processor

**Problem:** Your Processor holds resources (database connections, HTTP clients) that span multiple execution cycles, and you need to clean them up properly.

**Solution:** Override `Dispose(bool)` for long-lived resources:

```csharp
public class MyProcessor : Processor
{
    private HttpClient _client;

    protected override IEnumerable<Prompt> Execute_Subclass()
    {
        if (_client == null)
            _client = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
        // ... use _client ...
        return new List<Prompt>();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
            _client = null;
        }
        base.Dispose(disposing);
    }
}
```

For short-lived resources, use `using` blocks:

```csharp
using (var adapter = EwsServerDataAdapter.ConnectExisting(ServerName, UserName, Password))
{
    // adapter is disposed at the end of this block
}
```

**Reference:** [Developer Guide — Resource Disposal](developer-guide/smartconnector-developers-guide.md#1119-resource-disposal)

---

## 9 Provision an EWS Server Programmatically

**Problem:** You want your processor to create and provision an EWS Server with containers and value items automatically, instead of requiring manual Portal configuration.

**Solution:** Create the server with `EwsServerDataAdapter.ConnectNew`, then add containers and value items with `AddContainerItem` and `AddValueItem`:

```csharp
using (var adapter = EwsServerDataAdapter.ConnectNew(
    name: ServerName, address: EwsAddress, realm: Realm,
    username: UserName, password: Password, autoStart: true))
{
    // Add a container (folder). The first argument is the AlternateId (path);
    // the last is the parent container (null for the root).
    var weather = adapter.AddContainerItem(
        "/Weather", "Weather", "Weather", EwsContainerTypeEnum.Folder, null);

    // Add value items under the container.
    adapter.AddValueItem("/Weather/Temperature", "Temperature", "Temperature",
        EwsValueTypeEnum.Double, EwsValueWriteableEnum.Writeable,
        EwsValueForceableEnum.NotForceable, EwsValueStateEnum.Uncertain, "°C", weather);
    adapter.AddValueItem("/Weather/Humidity", "Humidity", "Humidity",
        EwsValueTypeEnum.Double, EwsValueWriteableEnum.Writeable,
        EwsValueForceableEnum.NotForceable, EwsValueStateEnum.Uncertain, "%", weather);
}
```

> **Tip:** Provision **idempotently** so one self-provisioning processor can both create the structure on the first run and re-run the provisioning step safely on every cycle — wrap each `Add*` call in an "ensure" helper that returns the existing item if it is already present. See [How-To 12](#12-run-a-self-provisioning-processor-safely-on-a-schedule) for the full pattern and [How-To 11](#11-populate-trend-logs-with-per-cycle-samples-and-backfill) for adding trend logs.

**Reference:** [Developer Guide — The Processor: A Phased Execution Cycle](developer-guide/smartconnector-developers-guide.md#76-the-processor-a-phased-execution-cycle)

---

## 10 Share Data Between Processors

**Problem:** Two Processors need to exchange data — for example, a setup Processor that provisions resources and an update Processor that needs to know what was provisioned.

**Solution:** Use a `ProcessorValue` for persistent data, or the injected `ICache` for volatile data.

### Persistent storage (survives service restart)

`FindOrCreateProcessorValue` is an extension method (in `Mongoose.Common.Data`) on the Processor; mutate `Value` and persist with `ProcessorValueSource.Save()`:

```csharp
using Mongoose.Common.Data;   // FindOrCreateProcessorValue

// Save a value
var pv = this.FindOrCreateProcessorValue("LastProvisionedServer");
pv.Value = serverName;
ProcessorValueSource.Save();

// Read it back (in this or another run of the same configuration)
var saved = this.FindOrCreateProcessorValue("LastProvisionedServer").Value;
```

### Volatile cache (cleared on service restart)

The `ICache` instance is injected into the Processor (see [Section 11.6.1](developer-guide/smartconnector-developers-guide.md#1161-in-memory-cache)):

```csharp
// Write — AddOrUpdateItem(item, key, tenantId, minutesUntilExpiry)
Cache.AddOrUpdateItem(reading, "LatestReading", "myTenant", 60);

// Read — RetrieveItem(key, tenantId) returns object; cast to your type
var latest = (Reading)Cache.RetrieveItem("LatestReading", "myTenant");
```

> **Caution:** Do not store credentials or personal identifiers in a `ProcessorValue` — it is not encrypted. Use `[EncryptedString]` properties for credentials.

**Reference:** [Developer Guide — Sharing Data within Smart Connector](developer-guide/smartconnector-developers-guide.md#116-sharing-data-within-smart-connector)

---

## 11 Populate Trend Logs with Per-Cycle Samples and Backfill

**Problem:** You provisioned trend logs (history items) but they stay empty, gain only one sample, or do not show any history until much later. You also want the trend viewer to be populated immediately after the first run, not days later.

**Solution:** Combine three things — enable the history methods on the server, append the current value to each trend every cycle, and seed history once on first run.

**1. Enable history when configuring the server.** Without these two methods the EWS Server disables trend logging:

```csharp
adapter.ModifyServerSupportedMethods(new EwsServerMethods
{
    // ... other methods ...
    GetHistory = true,
    GetHistoricalDataAggregation = true
});
```

**2. Provision an active history item linked to each value item:**

```csharp
var value = adapter.AddValueItem("/Weather/Temperature", "Temperature", "Temperature",
    EwsValueTypeEnum.Double, EwsValueWriteableEnum.Writeable,
    EwsValueForceableEnum.NotForceable, EwsValueStateEnum.Uncertain, "°C", parent);

adapter.AddHistoryItem("/Weather/TemperatureHistory", "Temperature History",
    "Temperature History", value, isActive: true, null);
```

**3. Append the current value to the trend every cycle.** The server auto-samples only when a value *changes*; if your source value is constant between cycles (for example an hourly price polled every 15 minutes), no sample is recorded. Writing the current value explicitly guarantees a sample each cycle. Take the value verbatim from the value item so its string format matches the trend's type:

```csharp
var item = adapter.ValueItems.FirstOrDefault(v => v.AlternateId == valueId);
if (item != null && !string.IsNullOrEmpty(item.Value))
{
    adapter.AddHistoryRecordsManually(trendId,
        new List<Tuple<EwsValueStateEnum, DateTime, string>>
        {
            Tuple.Create(EwsValueStateEnum.Good, DateTime.UtcNow, item.Value)
        });
}
```

**4. Backfill history on first run (and after gaps).** Fetch historical samples from your source and write them at their original timestamps. Make the backfill *idempotent* so a service restart does not duplicate or re-fetch everything:

- Derive the window from a **persisted** timestamp (e.g. a `Meta/LastUpdate` value item read back via `adapter.ValueItems`), not an in-memory field — an in-memory field resets on restart and re-backfills the whole window every time.
- Filter fetched points to those strictly **after** the persisted timestamp to avoid duplicate samples.
- If your source API caps rows per request, request a **window-sized** limit for backfill (for example `days * 24 + buffer`), not the small per-cycle limit — otherwise an initial multi-day backfill is silently truncated.

```csharp
var samples = new List<Tuple<EwsValueStateEnum, DateTime, string>>();
foreach (var point in historicalPoints)            // ordered, already filtered
    samples.Add(Tuple.Create(EwsValueStateEnum.Good, point.TimestampUtc,
        point.Value.ToString(CultureInfo.InvariantCulture)));

adapter.AddHistoryRecordsManually(trendId, samples);
```

> **Tip:** Run the backfill **last** in the cycle, after the live values and metadata are committed, because it can take minutes. See [How-To 12](#12-run-a-self-provisioning-processor-safely-on-a-schedule).

**Reference:** [Developer Guide — Recommended Patterns for Production Extensions](developer-guide/smartconnector-developers-guide.md#78-recommended-patterns-for-production-extensions) and [RESTful EWS Gateway — Trends and Trend Samples](developer-guide/smartconnector-restful-ews-gateway.md#10-api-reference--trends-and-trend-samples).

---

## 12 Run a Self-Provisioning Processor Safely on a Schedule

**Problem:** Separate setup and update processors drift out of sync; re-provisioning runs on every cycle; and a slow cycle (for example a long history backfill) can overlap the next scheduled trigger.

**Solution:** Use a single processor that provisions itself idempotently, runs its work in a fixed phase order, and guards against overlapping runs.

**1. Connect, creating the server only on the first run.** Try `ConnectExisting` first; only call `ConnectNew` when it fails. `ConnectNew` validates the server name up front and throws (`AssertEwsServerNameNotUsed`) if it already exists — *before* any row insert — so a name collision is **not** a `DbUpdateException` and cannot be caught as one. Gating `ConnectNew` behind `ConnectExisting` is what keeps a scheduled restart harmless:

```csharp
private EwsServerDataAdapter ConnectOrCreateServer()
{
    try
    {
        _adapter = EwsServerDataAdapter.ConnectExisting(ServerName, UserName, Password);
        return _adapter;                        // exists already — the common case
    }
    catch (Exception ex)
    {
        Logger.LogDebug(LogCategory.Processor,
            $"ConnectExisting failed ({ex.GetType().Name}); creating '{ServerName}'.");
    }

    using (EwsServerDataAdapter.ConnectNew(      // first run: create, then reconnect
        ServerName, EwsAddress, Realm, UserName, Password, autoStart: true)) { }

    _adapter = EwsServerDataAdapter.ConnectExisting(ServerName, UserName, Password);
    return _adapter;
}
```

**2. Provision idempotently.** Wrap every create call in a helper that checks for the existing item first, so the processor can run the provisioning step on every cycle harmlessly:

```csharp
private static EwsContainerItem EnsureFolder(
    EwsServerDataAdapter adapter, string altId, string name, EwsContainerItem parent)
{
    var existing = adapter.ContainerItems.FirstOrDefault(c => c.AlternateId == altId);
    return existing ?? adapter.AddContainerItem(
        altId, name, name, EwsContainerTypeEnum.Folder, parent);
}
```

Use the same pattern for value items and history items. This lets one processor combine setup and scheduled updates, which eliminates the configuration mismatch that two separate processors can suffer.

**3. Order the cycle in phases**, with the long-running work last:

1. **Provision** all containers, value items, and history items (idempotent).
2. **Update** all point values from your source.
3. **Sample** — append the current value to each trend (How-To 11).
4. **Metadata** — stamp `LastUpdate`, health, and counters.
5. **Backfill** history (may take minutes).

Capture the backfill decision in phase 1, *before* phase 4 writes a fresh `LastUpdate`, otherwise the new timestamp suppresses the backfill you just decided to run.

**4. Guard against overlapping triggers** with a static lock keyed by server name. Give it a timeout so a crashed run cannot block the processor forever:

```csharp
private static readonly object Lock = new object();
private static readonly Dictionary<string, DateTime> InProgress =
    new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

private static bool TryEnter(string key)
{
    lock (Lock)
    {
        if (InProgress.TryGetValue(key, out var started)
            && (DateTime.UtcNow - started).TotalMinutes < 30)
            return false;                       // a previous run is still active
        InProgress[key] = DateTime.UtcNow;
        return true;
    }
}
```

Acquire the lock at the top of `Execute_Subclass`, return immediately if it is held, and release it in a `finally`. The processor remains a normal scheduled processor — it still returns each cycle; the guard only prevents concurrent runs.

> **Tip:** When deriving an estimated "next update" timestamp from the interval between runs, clamp the interval to a sane range (for example 1 minute to 24 hours). Otherwise a long idle gap during development projects the next-run time far into the future.

**Reference:** [Developer Guide — Recommended Patterns for Production Extensions](developer-guide/smartconnector-developers-guide.md#78-recommended-patterns-for-production-extensions). Worked examples: the `SmartConnector.EntsoeExtension` and `SmartConnector.GridStatusExtension` projects under [`examples/`](../examples/).

---

*© 2017–2026 Schneider Electric. All rights reserved.*
