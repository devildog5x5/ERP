# Installer sources

Inno Setup 6 scripts for Windows 7 SP1+ / .NET Framework 4.8.

| Script | Output | Role |
|--------|--------|------|
| `server.iss` | `LedgerlyServerSetup.exe` | Server only |
| `client.iss` | `LedgerlyClientSetup.exe` | Client only |
| `ledgerly.iss` | `LedgerlySetup.exe` | Chooser: Both / Server / Client |

Shared version lives in `version.iss`. Dedicated packages show `info-*.txt` before the wizard continues. The chooser uses a custom page with three bold options (stock Components page is skipped).

Build everything with `..\build_installers.ps1` on a Windows machine that has the .NET Framework 4.8 targeting pack and Inno Setup 6.

Sanity-check the sources without Inno:

```powershell
python scripts\check_installers.py
```
