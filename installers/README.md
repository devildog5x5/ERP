# Windows installers

Produced by `..\build_installers.ps1` on a machine with Inno Setup 6 and the .NET Framework 4.8 targeting pack. Shared version lives in `..\installer\version.iss`.

| File | Role |
|------|------|
| `CoalesceSetup.exe` | Chooser — Both / Server / Client |
| `CoalesceClientSetup.exe` | Client only (own AppId; can coexist with Server) |
| `CoalesceServerSetup.exe` | Server only (own AppId; can coexist with Client) |

Published builds are on [GitHub Releases](https://github.com/devildog5x5/ERP/releases). Rebuild with `build_installers.ps1` on Windows, or the `Build installers` GitHub Actions workflow. Do not tag a release until all three Setup files match the current installer sources (`python scripts/check_installers.py`).
