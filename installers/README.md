# Windows installers

Produced by `..\build_installers.ps1` on a machine with Inno Setup 6 and the .NET Framework 4.8 targeting pack.

| File | Role |
|------|------|
| `CoalesceSetup.exe` | Chooser — Both / Server / Client |
| `CoalesceClientSetup.exe` | Client only (own AppId; can coexist with Server) |
| `CoalesceServerSetup.exe` | Server only (own AppId; can coexist with Client) |

Published builds are on [GitHub Releases](https://github.com/devildog5x5/ERP/releases). Checked-in EXEs can lag installer source until the next Windows rebuild. Do not tag a new release until `build_installers.ps1` has refreshed all three Setup files.
