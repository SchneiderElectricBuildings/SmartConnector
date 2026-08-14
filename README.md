# EcoStruxure&trade; Smart Connector

An extensible Windows service framework for integrating building automation systems with third-party applications using EcoStruxure&trade; Web Services (EWS).

Smart Connector lets developers write **Processors** (scheduled tasks) and **REST endpoints** that bridge Schneider Electric systems with external data sources — weather APIs, energy analytics, IoT platforms, and more.

| | |
|---|---|
| **Framework** | .NET Framework 4.8 &middot; C# |
| **Runtime** | Windows Service |
| **Protocols** | EWS SOAP 1.1/1.2 &middot; REST/JSON &middot; Syslog |
| **Current SDK** | v2.6 |

---

## Documentation

| Guide | Description |
|-------|-------------|
| [Developer Guide](docs/developer-guide/smartconnector-developers-guide.md) | Architecture, Quick Start, Extension development, full SDK reference |
| [RESTful EWS Gateway](docs/developer-guide/smartconnector-restful-ews-gateway.md) | REST API reference for building automation data (values, alarms, trends, subscriptions) |
| [Migration Guide](docs/migration-guide.md) | Upgrading to SDK v2.6 — breaking changes, NuGet feed migration, new features |
| [How-To Guides](docs/how-to-guides.md) | Task-oriented recipes: bulk writes, cron scheduling, REST auth, syslog, trend logs with backfill, safe scheduled processors, and more |

## Quick Start

> **Time to first build: ~10 minutes**

1. **Configure the NuGet feed** — add the GitHub Packages source for `SchneiderElectricBuildings`:

   ```powershell
   dotnet nuget add source "https://nuget.pkg.github.com/SchneiderElectricBuildings/index.json" `
     --name "SmartConnector" --username YOUR_USERNAME --password YOUR_PAT
   ```

   Your PAT needs `read:packages` scope only. See the [full instructions](docs/developer-guide/smartconnector-developers-guide.md#32-configure-the-nuget-feed-90-seconds).

2. **Create a class library** targeting .NET Framework 4.8.

3. **Add NuGet packages**: `Mongoose.Process` (provides the `Processor` base class), `Mongoose.Ews.Server.Data` (provides `EwsServerDataAdapter`), and `SxL.Common` (provides `Logger`).

4. **Write a Processor** — see the [Quick Start walkthrough](docs/developer-guide/smartconnector-developers-guide.md#3-quick-start) or the [sample project](examples/QuickStart.HelloEws/).

5. **Deploy** the compiled DLL to the Smart Connector Extensions folder and configure via the Portal.

## Examples

| Project | Description |
|---------|-------------|
| [QuickStart.HelloEws](examples/QuickStart.HelloEws/) | Minimal Processor that writes a UTC timestamp to an EWS ValueItem |
| [SmartConnector.WeatherExtension](examples/SmartConnector.WeatherExtension/) | The Developer Guide "Develop an Extension" walkthrough, fully implemented — fetches current conditions and a 3-day forecast from OpenWeatherMap and demonstrates self-provisioning, per-cycle trend sampling, a phased cycle, and a re-entrancy guard |
| [SmartConnector.GridStatusExtension](examples/SmartConnector.GridStatusExtension/) | Full reference extension — fetches North American grid signals (prices, load, fuel mix, carbon) and demonstrates self-provisioning, trend logs with backfill, a phased update cycle, and a re-entrancy guard |
| [SmartConnector.EntsoeExtension](examples/SmartConnector.EntsoeExtension/) | Full reference extension — fetches European energy-market data (prices, load, generation) with multi-zone support, using the same production patterns as the Grid Status example |

## Security

See [SECURITY.md](SECURITY.md) for vulnerability reporting.

## License

Copyright &copy; 2017&ndash;2026 Schneider Electric. All rights reserved.

The examples and documentation in this repository are released under the [MIT License](LICENSE), so you are free to use, copy, and modify them in your own projects, including commercial ones.

