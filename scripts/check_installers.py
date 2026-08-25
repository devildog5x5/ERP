#!/usr/bin/env python3
"""Sanity-check Coalesce installer sources without needing Inno Setup."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
INSTALLER = ROOT / "installer"


def read(name: str) -> str:
    return (INSTALLER / name).read_text(encoding="utf-8")


def main() -> int:
    version_file = read("version.iss")
    match = re.search(r'#define\s+MyAppVersion\s+"([^"]+)"', version_file)
    if not match:
        print("FAIL: version.iss missing MyAppVersion", file=sys.stderr)
        return 1
    version = match.group(1)
    print(f"version: {version}")

    required = [
        "version.iss",
        "coalesce.iss",
        "info-server.txt",
        "info-client.txt",
        "info-combined.txt",
        "README.md",
    ]
    for name in required:
        path = INSTALLER / name
        if not path.is_file():
            print(f"FAIL: missing {name}", file=sys.stderr)
            return 1
        print(f"ok: {name}")

    iss = read("coalesce.iss")

    checks = [
        ('#include "version.iss"' in iss, "coalesce.iss includes version.iss"),
        ("CoalesceSetup" in iss, "combined output name"),
        ("CoalesceClientSetup" in iss, "client output name"),
        ("CoalesceServerSetup" in iss, "server output name"),
        ("Coalesce.Server.exe" in iss, "server exe name"),
        ("Coalesce.Client.exe" in iss, "client exe name"),
        ("MinVersion=6.1sp1" in iss, "Win7 SP1+ gate"),
        ("IsDotNet48OrLater" in iss, ".NET 4.8 gate"),
        ("InfoBeforeFile={#InfoBefore}" in iss, "package-specific InfoBefore"),
        ("info-client.txt" in iss, "client InfoBefore wired"),
        ("info-server.txt" in iss, "server InfoBefore wired"),
        ("info-combined.txt" in iss, "combined InfoBefore wired"),
        ("CreateRolePage" in iss, "chooser custom page"),
        ("MakeRolePanel" in iss and "RoleBothPanel" in iss, "clickable option cards"),
        ("BOTH  —  Server + Client" in iss or "BOTH  —  Server and Client" in iss, "both option label"),
        ("SERVER ONLY  —  data and API" in iss or "SERVER  —  data and API" in iss, "server option label"),
        ("CLIENT ONLY  —  desktop UI" in iss or "CLIENT  —  the screen you work in" in iss, "client option label"),
        ("RoleBadge" in iss and "Recommended" in iss, "recommended badge on Both"),
        ("RoleSummary" in iss and "You selected:" in iss, "live choice summary"),
        ("WizardKeyDown" in iss and "Key = 49" in iss and "Key = 97" in iss, "1/2/3 keyboard shortcuts"),
        ("SelectBoth" in iss and "RoleHintBoth.OnClick" in iss, "hint clicks select both"),
        ("AdvanceBoth" in iss and "OnDblClick" in iss, "double-click advances from card"),
        ("PaintRolePanels" in iss, "selected card visual feedback"),
        ("SelectSetupTypeByName" in iss, "type combo synced by name"),
        ("wpSelectComponents" in iss and "ShouldSkipPage" in iss, "stock components page skipped"),
        ("SyncRadiosFromType" in iss, "chooser respects /TYPE="),
        ("UpdateReadyMemo" in iss, "ready page restates choice"),
        ('Name: "full"' in iss and 'Name: "server"' in iss and 'Name: "client"' in iss, "silent /TYPE= support"),
        ("SetupMutex=Coalesce_ERP_Setup_Mutex" in iss, "single-instance mutex"),
        ("WriteCapacityConfig" in iss and "DatabaseSizeMb" in iss, "server capacity config"),
        ('#if Package == "client"' in iss and '#elif Package == "server"' in iss, "dedicated package branches"),
        ('#define IsChooser "1"' in iss and '#define IsChooser "0"' in iss, "chooser vs dedicated flags"),
        ('#define HasServer "0"' in iss and '#define HasClient "0"' in iss, "payload exclusion flags"),
        ('MyAppName "Coalesce Client"' in iss and 'MyAppName "Coalesce Server"' in iss, "per-package AppName"),
        ("CombinedAppId" in iss and "ClientAppId" in iss and "ServerAppId" in iss, "separate AppIds defined"),
        ("E1A7B3C2-D4E5-4F60-8A91-B2C3D4E5F617" in iss, "dedicated Client AppId"),
        ("F2B8C4D3-E5F6-4071-9BA2-C3D4E5F61728" in iss, "dedicated Server AppId"),
        ("Role-aware cleanup" in iss, "role-aware uninstall comments"),
        ("clInfoBk" in iss, "selected card highlight"),
        ("DisableWelcomePage=yes" in iss, "welcome page skipped"),
    ]

    failed = False
    for ok, label in checks:
        if ok:
            print(f"ok: {label}")
        else:
            print(f"FAIL: {label}", file=sys.stderr)
            failed = True

    if "#if HasServer" not in iss or "#if HasClient" not in iss:
        print("FAIL: Files section missing HasServer/HasClient guards", file=sys.stderr)
        failed = True
    else:
        print("ok: Files section gated by HasServer/HasClient")

    build = (ROOT / "build_installers.ps1").read_text(encoding="utf-8")
    for needle in (
        "CoalesceServerSetup",
        "CoalesceClientSetup",
        "CoalesceSetup",
        "/DPackage=$package",
        "installer\\coalesce.iss",
        "chooser",
        "Client only",
        "Server only",
    ):
        if needle not in build:
            print(f"FAIL: build_installers.ps1 missing {needle}", file=sys.stderr)
            failed = True
        else:
            print(f"ok: build script mentions {needle}")

    readme = (ROOT / "README.md").read_text(encoding="utf-8")
    for needle in (
        "CoalesceSetup.exe",
        "CoalesceClientSetup.exe",
        "CoalesceServerSetup.exe",
        "Both",
        "Client",
        "Server",
        "chooser",
    ):
        if needle not in readme:
            print(f"FAIL: README.md missing {needle}", file=sys.stderr)
            failed = True
        else:
            print(f"ok: README mentions {needle}")

    # Catch the old all-in-one packaging regression: every Setup.exe the same size.
    publish = ROOT / "installers"
    sizes = {}
    for name in ("CoalesceSetup.exe", "CoalesceClientSetup.exe", "CoalesceServerSetup.exe"):
        path = publish / name
        if path.is_file() and path.stat().st_size > 0:
            sizes[name] = path.stat().st_size
            print(f"ok: {name} present ({sizes[name]} bytes)")
    if len(sizes) == 3:
        combined = sizes["CoalesceSetup.exe"]
        client = sizes["CoalesceClientSetup.exe"]
        server = sizes["CoalesceServerSetup.exe"]
        if client >= combined:
            print("FAIL: Client Setup should be smaller than Combined", file=sys.stderr)
            failed = True
        else:
            print("ok: Client Setup smaller than Combined")
        if server >= combined:
            print("FAIL: Server Setup should be smaller than Combined", file=sys.stderr)
            failed = True
        else:
            print("ok: Server Setup smaller than Combined")
        if abs(client - server) < 50_000 and abs(client - combined) < 50_000:
            print("FAIL: Setup packages look identical (all-in-one regression)", file=sys.stderr)
            failed = True
        else:
            print("ok: Setup packages have distinct sizes")

    if failed:
        return 1
    print("installer sources look good")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
