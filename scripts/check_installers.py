#!/usr/bin/env python3
"""Sanity-check installer sources without needing Inno Setup."""

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
        "server.iss",
        "client.iss",
        "ledgerly.iss",
        "info-server.txt",
        "info-client.txt",
    ]
    for name in required:
        path = INSTALLER / name
        if not path.is_file():
            print(f"FAIL: missing {name}", file=sys.stderr)
            return 1
        print(f"ok: {name}")

    server = read("server.iss")
    client = read("client.iss")
    chooser = read("ledgerly.iss")

    checks = [
        ('#include "version.iss"' in server, "server.iss includes version.iss"),
        ('#include "version.iss"' in client, "client.iss includes version.iss"),
        ('#include "version.iss"' in chooser, "ledgerly.iss includes version.iss"),
        ("OutputBaseFilename=LedgerlyServerSetup" in server, "server output name"),
        ("OutputBaseFilename=LedgerlyClientSetup" in client, "client output name"),
        ("OutputBaseFilename=LedgerlySetup" in chooser, "chooser output name"),
        ("Ledgerly.Server.exe" in server, "server exe name"),
        ("Ledgerly.Client.exe" in client, "client exe name"),
        ("MinVersion=6.1sp1" in server and "MinVersion=6.1sp1" in client and "MinVersion=6.1sp1" in chooser, "Win7 SP1+ gate"),
        ("IsDotNet48OrLater" in server and "IsDotNet48OrLater" in client and "IsDotNet48OrLater" in chooser, ".NET 4.8 gate"),
        ("InfoBeforeFile=info-server.txt" in server, "server InfoBefore"),
        ("InfoBeforeFile=info-client.txt" in client, "client InfoBefore"),
        ("CreateChoicePage" in chooser, "chooser custom page"),
        ("BOTH  —  Server and Client" in chooser, "both option label"),
        ("SERVER  —  data and API" in chooser, "server option label"),
        ("CLIENT  —  the screen you work in" in chooser, "client option label"),
        ("SelectBoth" in chooser and "LabelBothHint.OnClick" in chooser, "hint clicks select both"),
        ("wpSelectComponents" in chooser and "ShouldSkipPage" in chooser, "stock components page skipped"),
        ("SyncRadiosFromType" in chooser, "chooser respects /TYPE="),
        ("UpdateReadyMemo" in chooser, "ready page restates choice"),
        (
            'Name: "full"' in chooser
            and 'Name: "server"' in chooser
            and 'Name: "client"' in chooser,
            "silent /TYPE= support",
        ),
        ("AppId=" in server and "AppId=" in client and "AppId=" in chooser, "separate AppIds"),
        (
            "{{B8C4D3E2-5F60-789A-B1C2-D3E4F5061728}" in server
            and "{{A7B3C2D1-4E5F-6789-A0B1-C2D3E4F50617}" in client
            and "{{C9E5A1D4-6F70-4B13-9E4C-A2B1D3E5F708}" in chooser,
            "stable AppId GUIDs",
        ),
    ]

    failed = False
    for ok, label in checks:
        if ok:
            print(f"ok: {label}")
        else:
            print(f"FAIL: {label}", file=sys.stderr)
            failed = True

    build = (ROOT / "build_installers.ps1").read_text(encoding="utf-8")
    for needle in (
        "server.iss",
        "client.iss",
        "ledgerly.iss",
        "LedgerlyServerSetup",
        "LedgerlyClientSetup",
        "LedgerlySetup",
    ):
        if needle not in build:
            print(f"FAIL: build_installers.ps1 missing {needle}", file=sys.stderr)
            failed = True
        else:
            print(f"ok: build script mentions {needle}")

    if failed:
        return 1
    print("installer sources look good")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
