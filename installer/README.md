# Installer sources

Inno Setup 6 scripts for Windows 7 SP1+ / .NET Framework 4.8.

| `/DPackage=` | Output | Role |
|--------------|--------|------|
| `combined` (default) | `CoalesceSetup.exe` | Chooser first: Both / Server / Client |
| `client` | `CoalesceClientSetup.exe` | Client only |
| `server` | `CoalesceServerSetup.exe` | Server only |

Version lives in `version.iss`.

**Combined** skips Welcome and InfoBefore so the three numbered option cards are the first screen ("What should this PC run?" / "CHOOSE ONE"). A quick guide line maps situations to cards (**one PC → Both · shared host → Server · extra desk → Client**). Large **1 / 2 / 3** glyphs sit on each card; each states **Includes** / **Skips** plus a **Pick if …** audience line. Selected card sinks, thickens, highlights, and shows ✓ SELECTED; unselected cards say “Click to choose”; a summary bar under the cards restates the choice and says Next installs that; Next becomes **Yes — Install Both →** / **Yes — Install Server →** / **Yes — Install Client →**. Click, press 1 / 2 / 3, use arrow keys, Enter / Space, or double-click to continue. Silent: `/TYPE=full|server|client`. Dedicated Client/Server packages ship smaller payloads with no chooser.

**Client** and **Server** packages open with a short InfoBefore page and never show the role chooser. They use separate Windows AppIds so both can sit on the same PC; Combined clears those dedicated entries when you switch later.

Build on Windows:

```
powershell -File ..\build_installers.ps1
```

Sanity-check sources without Inno:

```
python scripts/check_installers.py
```
