# Coalesce.ERP.CRM (C#)

Windows client/server **ERP + CRM** for small businesses: inventory, purchasing, sales, suppliers/customers, CRM pipeline (leads, accounts, contacts, opportunities, activities), and operational reminders for stock and buying.

**Runs on Windows 7 SP1 through current Windows** (7 / 8.1 / 10 / 11 and newer).

Built with **C# / .NET Framework 4.8** — the correct target when Windows 7 must be supported. Newer .NET (5/6/7/8/10) only supports Windows 10+.

## Downloads

Installers are published on the [GitHub Releases](https://github.com/devildog5x5/ERP/releases) page. Each release ships **all three** packages (same C# / .NET Framework 4.8 build).

| Package | What it installs | Download |
|---------|------------------|----------|
| **Combined** | Same chooser installer (default: Client + Server) | [CoalesceSetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.6.0/CoalesceSetup.exe) |
| **Client package** | Same chooser installer (default: Client only) | [CoalesceClientSetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.6.0/CoalesceClientSetup.exe) |
| **Server package** | Same chooser installer (default: Server only) | [CoalesceServerSetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.6.0/CoalesceServerSetup.exe) |

- Latest release: [v1.6.0](https://github.com/devildog5x5/ERP/releases/tag/v1.6.0)
- Every installer asks up front — loudly — whether to install **Client**, **Server**, or **Both**.
- Default login after install: `admin` / `admin`
- Server listens at `http://127.0.0.1:8000` by default
- Requires **64-bit Windows** and **[.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)**
- Rebuild locally: `powershell -File .\build_installers.ps1` → `installers\CoalesceSetup.exe`, `CoalesceClientSetup.exe`, `CoalesceServerSetup.exe`

## Requirements

- **Windows 7 SP1 or later**
- **[.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)**  
  - Included with Windows 10/11  
  - Installable on Windows 7 SP1 / 8.1  
- **64-bit Windows** (x64 build)  
- [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist) (usually already present)

## Architecture

```
src/Ledgerly.Client   WPF desktop client → Coalesce.Client.exe
src/Ledgerly.Server   OWIN self-hosted Web API + EF Core 3.1 → Coalesce.Server.exe
src/Ledgerly.Shared   Shared DTOs
```

App data lives under `%LOCALAPPDATA%\Coalesce\` (Server / Client / Backups). Prior Ledgerly AppData is migrated automatically on first run.

## CRM (complements ERP)

| Area | Purpose |
|------|---------|
| **Leads** | Prospects; convert → CRM account + ERP customer |
| **Accounts** | Relationship wrapper; optional link to ERP Customer |
| **Contacts** | People at accounts |
| **Pipeline** | Opportunities by stage; **Win** creates a sales quote |
| **Activities** | Calls / meetings / tasks (separate from ops Reminders) |

ERP **Customers**, **Sales**, **Purchasing**, **Inventory**, and **Finance** remain the transactional source of truth.

## Run (development)

Requires a modern .NET SDK that can build `net48` projects (e.g. .NET 8+ SDK), plus .NET Framework 4.8 targeting pack / developer pack on the build machine.

```powershell
cd C:\Users\rober\Documents\GitHub\ERP

# Terminal 1 — API server (http://127.0.0.1:8000)
dotnet run --project src/Ledgerly.Server

# Terminal 2 — desktop client
dotnet run --project src/Ledgerly.Client
```

Optional demo catalog (sample products/partners):

```powershell
dotnet run --project src/Ledgerly.Server -- --demo
```

## Installers

```powershell
powershell -File .\build_installers.ps1
```

Produces Combined / Client / Server setups under `installers\`.

## Default credentials

- Username: `admin`
- Password: `admin`

## License

See repository for license terms.
