# Ledgerly ERP

Client–server ERP for small businesses: inventory, purchasing, sales, suppliers/customers, plus in-app and email reminders for stock and buying operations.

## Architecture

```
client/          Web SPA (port 3000)  →  talks to API over HTTP/JSON
app/             FastAPI API server (port 8000)
erp.db           SQLite database used by the server
```

- **Server** — REST API, database, reminder scheduler, email dispatch  
- **Client** — browser UI; no business logic stored server-side in templates  

## Downloads

**Requires Windows 10 or later** (Windows 10, 11, and newer). No Python install needed.

| Package | Download |
|---------|----------|
| **Ledgerly Setup (Client + Server)** | [LedgerlySetup.exe](https://github.com/devildog5x5/ERP/releases/download/v1.1.0/LedgerlySetup.exe) |

All releases: [https://github.com/devildog5x5/ERP/releases](https://github.com/devildog5x5/ERP/releases)

During setup you can choose:
- **Full installation** — Server and Client  
- **Server only**  
- **Client only**  
- **Custom** — pick either or both  

Recommended: install Server first (or Full), start the Server, then start the Client.

- Server data/config: `%LOCALAPPDATA%\Ledgerly\Server`
- Client config (`API_BASE`): `%LOCALAPPDATA%\Ledgerly\Client\config.txt`

Rebuild the installer (requires Python venv + [Inno Setup 6](https://jrsoftware.org/isinfo.php)):

```powershell
.\build_installers.ps1
```

## Quick start (development)

Requirements: Python 3.11+

```powershell
cd C:\Users\rober\Documents\GitHub\ERP
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
copy .env.example .env
python seed.py
```

### Run both processes

```powershell
.\start.ps1
```

Or in two terminals:

```powershell
# Terminal 1 — API server
python run_server.py

# Terminal 2 — web client
python run_client.py
```

Then open:

- **Client UI:** [http://127.0.0.1:3000](http://127.0.0.1:3000)  
- **API health:** [http://127.0.0.1:8000/api/health](http://127.0.0.1:8000/api/health)  
- **API docs:** [http://127.0.0.1:8000/docs](http://127.0.0.1:8000/docs)

Override the API URL from the browser with `?api=http://HOST:8000` (stored in localStorage).

## Features

- Inventory with reorder points and buy quantities  
- Purchase orders (create / receive / low-stock suggestions)  
- Sales orders with stock deduction  
- Reminders: low stock, suggested buys, overdue/incoming POs  
- Email alerts (SMTP or console demo mode)  
- Background reminder scheduler on the API server  

## Project layout

```
app/                 API server (FastAPI + SQLAlchemy)
client/              SPA client (HTML/CSS/JS modules)
bundle/              PyInstaller entry points + specs
installer/           Inno Setup script (combined Win10+ installer)
installers/          Published LedgerlySetup.exe
run_server.py        Start API on :8000
run_client.py        Start client on :3000
start.ps1            Start both (dev)
build_installers.ps1 Build Windows 10+ installer
seed.py              Demo data
```
