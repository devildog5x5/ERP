# Installer sources

Inno Setup 6 scripts for Windows 10+.

| Script | Output | Role |
|--------|--------|------|
| `server.iss` | `LedgerlyServerSetup.exe` | Server only |
| `client.iss` | `LedgerlyClientSetup.exe` | Client only |
| `ledgerly.iss` | `LedgerlySetup.exe` | Chooser: Both / Server / Client |

Shared version lives in `version.iss`. Build everything with `..\build_installers.ps1` on a Windows machine that has a project venv and Inno Setup 6.

Sanity-check the sources without Inno:

```powershell
python scripts\check_installers.py
```
