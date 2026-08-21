# Coalesce

**Where ERP and CRM come together.**

Windows client/server app for small businesses: inventory, purchasing, sales, suppliers/customers, CRM pipeline (leads, accounts, contacts, opportunities, activities), and operational reminders for stock and buying.

**Runs on Windows 7 SP1 through current Windows** (7 / 8.1 / 10 / 11 and newer).

Built with **C# / .NET Framework 4.8** — the correct target when Windows 7 must be supported. Newer .NET (5/6/7/8/10) only supports Windows 10+.

## Downloads

Installers are published on the [GitHub Releases](https://github.com/devildog5x5/ERP/releases) page. Each release ships **all three** packages (same C# / .NET Framework 4.8 build).

| Package | What it installs | Download |
|---------|------------------|----------|
| **Combined (chooser)** | One wizard — pick **Both**, **Server**, or **Client** | [CoalesceSetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.6.15/CoalesceSetup.exe) |
| **Client only** | Desktop UI only (talks to a running Coalesce Server) | [CoalesceClientSetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.6.15/CoalesceClientSetup.exe) |
| **Server only** | API / database host only | [CoalesceServerSetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.6.15/CoalesceServerSetup.exe) |

- Latest published binaries: [v1.6.15](https://github.com/devildog5x5/ERP/releases/tag/v1.6.15) (installer sources on this branch target **1.6.16** — rebuild on Windows with `build_installers.ps1` before tagging)
- **CoalesceSetup.exe** opens with three numbered option cards (**1 Both** / **2 Server** / **3 Client**; Recommended on Both). Click, press 1–3, or double-click to continue. Silent: `/TYPE=full|server|client`.
- **Client** and **Server** packages install that role only — no chooser, smaller payload. They use separate AppIds so both can live on one machine; Combined replaces them if you switch later.
- Server installs also ask for a **planned database size** (Small 500 MB / Medium 2 GB / Large 10 GB / Custom). That choice is saved quietly into the server config and drives Database status warnings — not a hard engine limit. Need more room later? Use **Settings → Grow database…**.
- Default login after install: `admin` / `admin`
- Server listens at `http://127.0.0.1:8000` by default
- Requires **64-bit Windows** and **[.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)**
- Rebuild locally: `powershell -File .\build_installers.ps1` → `installers\CoalesceSetup.exe`, `CoalesceClientSetup.exe`, `CoalesceServerSetup.exe`
- Source check (no Inno needed): `python scripts/check_installers.py`

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

## Database providers

Configured in `%LOCALAPPDATA%\Coalesce\Server\server.json` (`Provider` + `ConnectionString` + optional `DatabaseSizeMb` / `CapacityProfile`). Default is **Sqlite** with a **Medium (2 GB)** planned size from the installer.

| Provider | Typical use | Example `Provider` value |
|----------|-------------|--------------------------|
| **Sqlite** | Single PC / small shop (default) | `Sqlite` |
| **SqlServer** | Windows Server / Azure SQL | `SqlServer` |
| **MySql** | MySQL or MariaDB | `MySql` |
| **PostgreSql** | PostgreSQL | `PostgreSql` |

**Point at an empty database** (creates schema, does not copy data):

```powershell
Coalesce.Server.exe set-db --provider MySql --connection "Server=localhost;Port=3306;Database=coalesce;User=coalesce;Password=***;"
Coalesce.Server.exe set-db --provider PostgreSql --connection "Host=localhost;Port=5432;Database=coalesce;Username=coalesce;Password=***;"
Coalesce.Server.exe set-db --provider SqlServer --connection "Server=localhost;Database=Coalesce;Trusted_Connection=True;TrustServerCertificate=True;"
```

**Copy current data** into an empty target and switch config:

```powershell
Coalesce.Server.exe migrate --provider SqlServer --connection "Server=localhost;Database=Coalesce;Trusted_Connection=True;TrustServerCertificate=True;"
Coalesce.Server.exe migrate --provider MySql --connection "Server=localhost;Database=coalesce;User=coalesce;Password=***;"
Coalesce.Server.exe migrate --provider PostgreSql --connection "Host=localhost;Database=coalesce;Username=coalesce;Password=***;"
```

Use `--no-switch` on `migrate` to copy without changing `server.json`. Restart the server after `set-db` or a config-switching migrate. File backup/restore in the app applies to SQLite only; use vendor tools for SQL Server / MySQL / PostgreSQL.

Administrators can open **Settings → Grow database…** to move onto SQL Server, MySQL, or PostgreSQL (test connection, copy data, switch automatically). **Database status…** shows capacity pies, table share, and suggestions. In the Danger zone (same area as Refresh database), the **(i) Database guide** opens full step-by-step instructions for grow, status, backup, purge, and refresh.

**Grow database (UI):** Settings → Grow database… → pick provider → host/database → Test connection → type `GROW DATABASE` → Grow. CLI (`migrate` / `set-db`) still works if you prefer.

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

Produces three setups under `installers\`:

- `CoalesceSetup.exe` — chooser (Both / Server / Client)
- `CoalesceClientSetup.exe` — Client only
- `CoalesceServerSetup.exe` — Server only

## Default credentials

- Username: `admin`
- Password: `admin`

## License

See repository for license terms.
