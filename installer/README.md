# Installer sources

Inno Setup 6 scripts for Windows 7 SP1+ / .NET Framework 4.8.

| `/DPackage=` | Output | Role |
|--------------|--------|------|
| `combined` (default) | `CoalesceSetup.exe` | Chooser: Both / Server / Client |
| `client` | `CoalesceClientSetup.exe` | Client only |
| `server` | `CoalesceServerSetup.exe` | Server only |

Version lives in `version.iss`. Dedicated packages open with a short InfoBefore page and never show the role chooser. The combined package shows InfoBefore, then three clickable option cards (stock Components page is skipped). The selected card sinks; hints and the Recommended label select that option. Double-click a card to select it and continue.

Build on Windows:

```
powershell -File ..\build_installers.ps1
```

Sanity-check sources without Inno:

```
python scripts/check_installers.py
```
