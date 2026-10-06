# Installer sources

Inno Setup 6 scripts for Windows 7 SP1+ / .NET Framework 4.8.

| `/DPackage=` | Output | Role |
|--------------|--------|------|
| `combined` (default) | `CoalesceSetup.exe` | Chooser first: Both / Server / Client |
| `client` | `CoalesceClientSetup.exe` | Client only |
| `server` | `CoalesceServerSetup.exe` | Server only |

Version lives in `version.iss`.

**Combined** skips Welcome and InfoBefore so the three numbered option cards are the first screen ("Pick ONE role for this PC"). A scannable decision tree maps situations to cards (**only PC → Both · shared DB → Server · elsewhere → Client**); unsure users are told to leave **Both** selected. **Both** is a full-width ★ Recommended card on top; **Server** and **Client** sit side by side underneath. An on-page **Keys:** legend lists `[1] Both / [2] Server / [3] Client`. Large **1 / 2 / 3** glyphs; titles are situation-first labels beside tiny radios. Each card has a left accent strip that lights navy / maroon / teal when selected, plus **Includes** / **Skips**, a **Pick if …** line, sunk bevel, color tint, and ✓ SELECTED. The summary bar leads with **▶ YOUR CHOICE →**, restates Includes/Skips plus a plain-English outcome; Next becomes **Yes — Install Both →** / **Yes — Install Server →** / **Yes — Install Client →**. Later wizard pages keep **Installing: …** in the subtitle. Click, press 1 / 2 / 3, use arrow keys, Enter / Space, or double-click to continue. Silent: `/TYPE=full|server|client`. Dedicated Client/Server packages ship smaller payloads with no chooser.

**Client** and **Server** packages open with a short InfoBefore page and never show the role chooser. They use separate Windows AppIds so both can sit on the same PC; Combined clears those dedicated entries when you switch later.

Build on Windows:

```
powershell -File ..\build_installers.ps1
```

Sanity-check sources without Inno:

```
python scripts/check_installers.py
```
