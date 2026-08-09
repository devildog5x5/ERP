# Ledgerly ERP (C#)

Windows client/server ERP for small businesses: inventory, purchasing, sales, suppliers/customers, and operational reminders for stock and buying.

**Runs on Windows 7 SP1 through current Windows** (7 / 8.1 / 10 / 11 and newer).

Built with **C# / .NET Framework 4.8** — the correct target when Windows 7 must be supported. Newer .NET (5/6/7/8/10) only supports Windows 10+.

## Downloads

Installers are published on the [GitHub Releases](https://github.com/devildog5x5/ERP/releases) page. Each release ships **all three** packages (same C# / .NET Framework 4.8 build).

| Package | What it installs | Download |
|---------|------------------|----------|
| **Combined (chooser)** | One wizard — pick **Both**, **Server**, or **Client** (big clear radios) | [LedgerlySetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.4.0/LedgerlySetup.exe) |
| **Client only** | Desktop UI only (connects to a running Ledgerly Server) | [LedgerlyClientSetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.4.0/LedgerlyClientSetup.exe) |
| **Server only** | API / database host only | [LedgerlyServerSetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.4.0/LedgerlyServerSetup.exe) |

- Latest release: [v1.4.0](https://github.com/devildog5x5/ERP/releases/tag/v1.4.0)
- Default login after install: `admin` / `admin`
- Server listens at `http://127.0.0.1:8000` by default
- Requires **64-bit Windows** and **[.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)**
- Rebuild locally: `powershell -File .\build_installers.ps1` → `installers\LedgerlySetup.exe`, `LedgerlyClientSetup.exe`, `LedgerlyServerSetup.exe`

## Requirements

- **Windows 7 SP1 or later**
- **[.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)**  
  - Included with Windows 10/11  
  - Installable on Windows 7 SP1 / 8.1  
- **64-bit Windows** (x64 build)  
- [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist) (usually already present)

## Architecture

```
src/Ledgerly.Client   WPF desktop client (.NET Framework 4.8)
src/Ledgerly.Server   OWIN self-hosted Web API + EF Core 3.1 (SQLite or SQL Server)
src/Ledgerly.Shared   Shared DTOs
```

## Run (development)

Requires a modern .NET SDK that can build `net48` projects (e.g. .NET 8+ SDK), plus .NET Framework 4.8 targeting pack / developer pack on the build machine.

```powershell
cd C:\Users\rober\Documents\GitHub\ERP

# Terminal 1 — API server (http://127.0.0.1:8000)
dotnet run --project src/Ledgerly.Server

# Terminal 2 — WPF client
dotnet run --project src/Ledgerly.Client
```

- Default database: SQLite at `%LOCALAPPDATA%\Ledgerly\Server\ledgerly.db`
- Server config: `%LOCALAPPDATA%\Ledgerly\Server\server.json` (provider, connection string, listen URL)

## Why .NET Framework 4.8

| Runtime | Windows 7 | Windows 10/11 |
|---------|-----------|----------------|
| .NET 8 / 10 | No | Yes |
| **.NET Framework 4.8** | **Yes (SP1)** | **Yes** |

For “Windows 7 forward”, Framework 4.8 is the supported Microsoft stack. The server uses OWIN self-host (not ASP.NET Core) for the same reason.

## Features

- **Auth**: users, roles, permissions, login (`admin` / `admin`)
- **Audit log**, backups/restore (SQLite), API keys, webhooks
- Inventory with UPC/barcode, average costing, margins
- Multi-location warehouse, transfers, cycle counts, BOM/kits
- Scan station (lookup / stock adjust / PO receive / quick sale)
- Purchasing with approval threshold, print PO, vendor bills/AP
- Sales quotes→orders→invoice, payments/AR, RMA returns, print/email
- Tax codes, price lists, multi-currency rates, multi-company
- Finance: chart of accounts, journals, bank reconcile, period close
- Reports: margin, AR/AP aging, dead stock
- Integrations stubs: Shopify sync, accounting CSV export, shipping tracking
- SMTP settings for document email
- **Scale-up path** SQLite → SQL Server (same app)

## Scale-up / migrate to SQL Server

Ledgerly starts on **SQLite** (zero install, great for one shop). When you need more concurrent users or a shared server database, migrate in one step — **no rewrite of the client or API**.

1. Install SQL Server (Express is fine) and create an empty database, e.g. `Ledgerly`.
2. Stop the Ledgerly server.
3. Run:

```powershell
dotnet run --project src/Ledgerly.Server -- migrate --connection "Server=localhost;Database=Ledgerly;Trusted_Connection=True;TrustServerCertificate=True;"
```

Or against a built exe:

```powershell
.\Ledgerly.Server.exe migrate --connection "Server=db01;Database=Ledgerly;Trusted_Connection=True;TrustServerCertificate=True;"
```

4. Start the server again. It reads `server.json` and uses SQL Server automatically.
5. Keep the old SQLite file as a backup until you verify counts in Settings → Database platform.

Optional: `--no-switch` copies data but leaves the active provider on SQLite.

The health API reports `databaseProvider`, `database`, and `canScaleOut` so the client Settings page can show the current platform.  

