# Installer sources

Inno Setup 6 scripts for Windows 7 SP1+ / .NET Framework 4.8.

| `/DPackage=` | Output | Role |
|--------------|--------|------|
| `combined` (default) | `CoalesceSetup.exe` | Chooser first: Both / Server / Client |
| `client` | `CoalesceClientSetup.exe` | Client only |
| `server` | `CoalesceServerSetup.exe` | Server only |

Version lives in `version.iss`.

**Combined** skips Welcome and InfoBefore so the three numbered option cards are the first screen ("What should this PC run?"). Selected card sinks, highlights, and shows SELECTED; Next becomes Install Both → / Install Server → / Install Client →. Click, press 1 / 2 / 3, use ↑ ↓, Enter, or double-click to continue. Silent: `/TYPE=full|server|client`.

**Client** and **Server** packages open with a short InfoBefore page and never show the role chooser. They use separate Windows AppIds so both can sit on the same PC; Combined clears those dedicated entries when you switch later.

Build on Windows:

```
powershell -File ..\build_installers.ps1
```

Sanity-check sources without Inno:

```
python scripts/check_installers.py
```
