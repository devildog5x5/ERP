# Ledgerly ERP

Client–server ERP for small businesses: inventory, purchasing, sales, suppliers/customers, plus operational reminders for stock and buying.

## Windows installers

**Requires Windows 10 or later.** No Python install needed.

| Package | What it installs | File |
|---------|------------------|------|
| **Ledgerly Setup** | Asks you: **Server**, **Client**, or **Both** | [`LedgerlySetup.exe`](installers/LedgerlySetup.exe) |
| **Server Setup** | Server only | [`LedgerlyServerSetup.exe`](installers/LedgerlyServerSetup.exe) |
| **Client Setup** | Client only | [`LedgerlyClientSetup.exe`](installers/LedgerlyClientSetup.exe) |

Installer version: **1.3.0** (`installer/version.iss`).

### Which one?

- **One PC for everything** → run *Ledgerly Setup* and pick **BOTH** (or install Server Setup + Client Setup on that machine).
- **Shared server + workstations** → Server Setup on the data machine, Client Setup on each desk.
- **Not sure** → run **Ledgerly Setup**. Right after Welcome you get three bold choices: Both / Server / Client.

Order that works: install and start the Server, then start the Client.

- Server data: `%LOCALAPPDATA%\Ledgerly\Server`
- Client config (`API_BASE`): `%LOCALAPPDATA%\Ledgerly\Client\config.txt`

Silent chooser:

```powershell
.\LedgerlySetup.exe /VERYSILENT /TYPE=full
.\LedgerlySetup.exe /VERYSILENT /TYPE=server
.\LedgerlySetup.exe /VERYSILENT /TYPE=client
```

Rebuild all three Setup packages (venv + [Inno Setup 6](https://jrsoftware.org/isinfo.php)):

```powershell
.\build_installers.ps1
```

Source check (no Inno required):

```powershell
python scripts\check_installers.py
```

---

## Python stack (what the installers package)

```
client/          Web SPA (port 3000)  →  API over HTTP/JSON
app/             FastAPI API server (port 8000)
bundle/          PyInstaller entry points + specs
```

### Dev quick start

Python 3.11+:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
copy .env.example .env
python seed.py
.\start.ps1
```

Or two terminals: `python run_server.py` then `python run_client.py`.

- Client: http://127.0.0.1:3000
- API health: http://127.0.0.1:8000/api/health
- Docs: http://127.0.0.1:8000/docs

Override the API URL in the browser with `?api=http://HOST:8000` (stored in localStorage).

---

## C# desktop stack (Windows 7 SP1+)

Optional WPF + OWIN path under `src/` for shops that need Windows 7 or a native desktop client.

```
src/Ledgerly.Client   WPF desktop client (.NET Framework 4.8)
src/Ledgerly.Server   OWIN self-hosted Web API + EF Core 3.1 (SQLite or SQL Server)
src/Ledgerly.Shared   Shared DTOs
```

Requires a modern .NET SDK that can build `net48`, plus the .NET Framework 4.8 targeting pack on the build machine.

```powershell
dotnet run --project src/Ledgerly.Server
dotnet run --project src/Ledgerly.Client
```

Default SQLite DB: `%LOCALAPPDATA%\Ledgerly\Server\ledgerly.db`  
Server config: `%LOCALAPPDATA%\Ledgerly\Server\server.json`

Login (seeded): `admin` / `admin`.

### Scale-up to SQL Server

1. Create an empty SQL Server database (Express is fine).
2. Stop the Ledgerly server.
3. Run:

```powershell
dotnet run --project src/Ledgerly.Server -- migrate --connection "Server=localhost;Database=Ledgerly;Trusted_Connection=True;TrustServerCertificate=True;"
```

Or against a built exe:

```powershell
.\Ledgerly.Server.exe migrate --connection "Server=db01;Database=Ledgerly;Trusted_Connection=True;TrustServerCertificate=True;"
```

`--no-switch` copies data but leaves the active provider on SQLite.

---

## Features

- Inventory with reorder points, UPC/barcode, average costing
- Purchasing (POs, receive, low-stock suggestions) and sales (quotes→orders→invoice)
- Partners (suppliers/customers), reminders, email alerts
- Auth, audit log, backups, API keys / webhooks (C# enterprise modules)
- Warehouse, scan station, finance/GL, reports, integration stubs (C#)

## Layout

```
app/                 FastAPI + SQLAlchemy API
client/              SPA (HTML/CSS/JS)
bundle/              PyInstaller specs
installer/           Inno Setup (server, client, chooser)
installers/          Published Setup executables
scripts/             Source checks (no Inno required)
src/                 C# client / server / shared
run_server.py        API on :8000
run_client.py        SPA on :3000
start.ps1            Start both (dev)
build_installers.ps1 Build all three Windows 10+ installers
seed.py              Demo data
```
