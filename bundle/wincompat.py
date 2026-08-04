"""Windows 10+ runtime checks for packaged Ledgerly apps."""

from __future__ import annotations

import sys


def require_windows_10() -> None:
    if sys.platform != "win32":
        return
    version = sys.getwindowsversion()
    if version.major < 10:
        raise SystemExit(
            "Ledgerly requires Windows 10 or later.\n"
            f"Detected Windows version: {version.major}.{version.minor}"
        )
