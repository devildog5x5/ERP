# Installer sources

Inno Setup 6 scripts for Windows 7 SP1+ / .NET Framework 4.8.

| Package define | Output | Role |
|----------------|--------|------|
| `combined` (default) | `CoalesceSetup.exe` | Chooser: Both / Server / Client |
| `client` | `CoalesceClientSetup.exe` | Client only |
| `server` | `CoalesceServerSetup.exe` | Server only |

Shared version lives in `version.iss`. Dedicated packages show `info-client.txt` / `info-server.txt` before the wizard continues. The combined package shows `info-combined.txt`, then a custom page with three bold options (stock Components page is skipped). Hint text is clickable.

Build everything with `..\build_installers.ps1` on a Windows machine that has the .NET Framework 4.8 targeting pack and Inno Setup 6.

Sanity-check the sources without Inno:

```
python scripts/check_installers.py
```
