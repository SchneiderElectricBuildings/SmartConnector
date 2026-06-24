# EcoStruxure™ Smart Connector SDK Migration Guide

<!-- Document: EcoStruxure™ Smart Connector SDK Migration Guide -->
<!-- Applies to: Smart Connector SDK v2.6 -->
<!-- Revision: 1 -->
<!-- Date: June 2026 -->
<!-- Author: Karim Hussain -->
<!-- Audience: Extension developers upgrading existing extensions to Smart Connector v2.6 -->

This guide covers the changes you need to make when upgrading an existing Smart Connector extension to v2.6. It is organised by the action you need to take — not by internal version history.

If you are starting a new extension from scratch, skip this guide and go straight to the [Quick Start](developer-guide/smartconnector-developers-guide.md#3-quick-start).

---

## 1 Target Framework: .NET Framework 4.8

Smart Connector v2.6 requires .NET Framework 4.8.

**Action required:** Retarget your extension project.

1. Open your `.csproj` in Visual Studio.
2. Change the `TargetFrameworkVersion`:

```xml
<!-- Before -->
<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>

<!-- After -->
<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
```

Or, in SDK-style projects:

```xml
<TargetFramework>net48</TargetFramework>
```

3. Install .NET Framework 4.8 Developer Pack if prompted.
4. Rebuild and resolve any compiler warnings introduced by the new target.

> **Note:** .NET Framework 4.8 is backward-compatible with 4.5/4.7.2 APIs. Recompilation is typically sufficient — no code changes are needed unless you relied on APIs that were deprecated or removed between framework versions.

---

## 2 NuGet Feed: Migrate to GitHub Packages

The Smart Connector SDK packages have moved from MyGet to GitHub Packages.

**Action required:** Update your NuGet source configuration.

### 2.1 Remove the old source

```powershell
dotnet nuget remove source SmartConnector
```

### 2.2 Add the new source

**Option A — Encrypted credential store (recommended for developer machines):**

```powershell
dotnet nuget add source "https://nuget.pkg.github.com/SchneiderElectricBuildings/index.json" `
  --name "SmartConnector" `
  --username YOUR_GITHUB_USERNAME `
  --password YOUR_GITHUB_PAT
```

**Option B — Environment-variable substitution (recommended for CI/CD):**

Create a `nuget.config` that references an environment variable:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="SmartConnector"
         value="https://nuget.pkg.github.com/SchneiderElectricBuildings/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <SmartConnector>
      <add key="Username" value="USERNAME" />
      <add key="ClearTextPassword" value="%GITHUB_PACKAGES_PAT%" />
    </SmartConnector>
  </SmartConnector>
  </packageSourceCredentials>
</configuration>
```

Set `GITHUB_PACKAGES_PAT` in your CI/CD secrets vault.

> **Security:** The PAT requires only the `read:packages` scope. Set an expiry. Never commit a PAT in plain text. See the [Developer Guide — NuGet Personal Access Tokens](developer-guide/smartconnector-developers-guide.md#524-nuget-personal-access-tokens) for full guidance.

### 2.3 Restore and verify

```powershell
dotnet restore
```

Confirm all `Mongoose.*` and `SxL.*` packages resolve from the new source.

---

## 3 REST API: Take Limit

All list endpoints in the RESTful EWS Gateway now enforce a maximum `take` value of **5,000**.

**Impact:** If your REST client requests `take=10000`, the response will silently cap at 5,000 items. Update your client code to paginate if you need more than 5,000 results.

See [EWS Gateway Guide — Filtering, Sorting, and Paging](developer-guide/smartconnector-restful-ews-gateway.md#66-filtering-sorting-and-paging) for pagination patterns.

---

## 4 REST API: Metadata Parameter

All retrieval endpoints now accept an optional `includeMetadata` boolean query parameter (default: `false`).

**Impact on REST clients:** No breaking change — existing calls continue to work. Pass `?includeMetadata=true` to receive semantic metadata in responses.

**Impact on custom `IEwsRestProvider` implementations:** The interface method signatures have changed. If you implement `IEwsRestProvider` directly (not through the base classes), you must add the `includeMetadata` parameter to your retrieval method signatures — for example:

```csharp
// Before
ValueModel RetrieveValue(BaseApiController controller, string id);

// After
ValueModel RetrieveValue(BaseApiController controller, string id, bool includeMetadata = false);
```

The same applies to the other single-item retrieval methods (`RetrieveContainer`, `RetrieveTrend`, `RetrieveChildrenOfContainer`, and so on).

See [Developer Guide — Metadata API](developer-guide/smartconnector-developers-guide.md#1113-metadata-api).

---

## 5 Performance: AutoCommit Bulk Update Pattern

v2.6 introduces a bulk update pattern for `EwsServerDataAdapter` that delivers up to 10× faster writes.

**Action required:** No — existing code continues to work. But if your Processor updates many values per execution cycle, consider adopting the pattern:

```csharp
using (var adapter = EwsServerDataAdapter.ConnectExisting(ServerName, UserName, Password))
{
    adapter.AutoCommit = false;

    foreach (var item in valuesToUpdate)
    {
        adapter.ModifyValueItemValue(item.AlternateId, item.Value, EwsValueStateEnum.Good);
    }

    adapter.CommitChanges();
    adapter.AutoCommit = true;
}
```

See [Developer Guide — EwsServerDataAdapter](developer-guide/smartconnector-developers-guide.md#114-ewsserverdataadapter) for details.

---

## 6 Security Configuration Defaults

v2.6 ships with hardened security defaults. These affect the Smart Connector runtime, not your extension code — but your users and administrators should be aware:

| Setting | Default | Description |
|---------|---------|-------------|
| Account lockout | 3 failed attempts → 10 min lockout | Prevents brute-force login attacks on the Portal and REST endpoints |
| Password complexity | Minimum 6 characters; uppercase, lowercase, digit, and special character required | Applies to Portal user accounts and REST API credentials |
| Authentication timing | Randomised delay on failed authentication | Prevents timing-based username enumeration |

These defaults can be adjusted through the Smart Connector configuration. See [Developer Guide — Security Configuration](developer-guide/smartconnector-developers-guide.md#1115-security-configuration).

---

## 7 Migration Checklist

Use this checklist to confirm your extension is ready for v2.6:

- [ ] `.csproj` targets `net48` (or `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`)
- [ ] NuGet source points to `https://nuget.pkg.github.com/SchneiderElectricBuildings/index.json`
- [ ] `dotnet restore` resolves all Smart Connector packages from the new feed
- [ ] Project compiles with zero errors against the v2.6 SDK
- [ ] If you implement `IEwsRestProvider` directly: `includeMetadata` parameter added to all retrieval methods
- [ ] If your REST clients request `take` > 5,000: pagination implemented
- [ ] (Optional) Bulk update code uses `AutoCommit = false` + `CommitChanges()` pattern
- [ ] Extension tested against a v2.6 Smart Connector instance

---

## 8 Compatibility Matrix

| Smart Connector Version | .NET Framework |
|-------------------------|----------------|
| v2.3.x | 4.5 |
| v2.4.x | 4.5 |
| v2.5.x (early) | 4.7.2 |
| v2.5.x (late) | 4.8 |
| v2.6.x | 4.8 |

> **Note:** All Smart Connector SDK packages are now published exclusively to GitHub Packages on the `SchneiderElectricBuildings` organisation (<https://github.com/orgs/SchneiderElectricBuildings/packages>). The former MyGet feed has been retired, so every supported version is restored from GitHub Packages regardless of the source it originally shipped from.

---

*© 2017–2026 Schneider Electric. All rights reserved.*
