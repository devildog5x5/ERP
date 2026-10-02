# Installer sources

Inno Setup 6 scripts for Windows 7 SP1+ / .NET Framework 4.8.

| `/DPackage=` | Output | Role |
|--------------|--------|------|
| `combined` (default) | `CoalesceSetup.exe` | Chooser first: Both / Server / Client |
| `client` | `CoalesceClientSetup.exe` | Client only |
| `server` | `CoalesceServerSetup.exe` | Server only |

Version lives in `version.iss`.

**Combined** skips Welcome and InfoBefore so the three numbered option cards are the first screen ("What should this PC run?"). A quick guide maps situations to cards (**one PC → Both · shared host → Server · extra desk → Client**); unsure users are told to leave **Both** selected. **Both** is a full-width ★ Recommended card on top; **Server** and **Client** sit side by side underneath so the specialized fork is obvious. Large **1 / 2 / 3** glyphs; titles are situation-first (**one PC does it all** / **holds the data** / **desk you work at**). Each card states **Includes** / **Skips** plus a **Pick if …** audience line. Selected card sinks, thickens, tints yellow / rose / mint (matching the summary bar), and shows ✓ SELECTED; unselected cards say “Click to choose”; the summary bar restates Includes/Skips plus a plain-English “After install this PC will …” outcome; Next becomes **Yes — Install Both →** / **Yes — Install Server →** / **Yes — Install Client →**. Later wizard pages keep **Installing: …** in the subtitle so the choice stays visible. Click, press 1 / 2 / 3, use arrow keys, Enter / Space, or double-click to continue. Silent: `/TYPE=full|server|client`. Dedicated Client/Server packages ship smaller payloads with no chooser.

**Client** and **Server** packages open with a short InfoBefore page and never show the role chooser. They use separate Windows AppIds so both can sit on the same PC; Combined clears those dedicated entries when you switch later.

Build on Windows:

```
powershell -File ..\build_installers.ps1
```

Sanity-check sources without Inno:

```
python scripts/check_installers.py
```
