"""Packaged Ledgerly web client entry point."""

from __future__ import annotations

import functools
import http.server
import os
import socketserver
import sys
import threading
import time
import webbrowser
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from bundle.paths import appdata_dir, is_frozen, resource_root  # noqa: E402

HOST = "127.0.0.1"
PORT = 3000


class SpaHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, directory: str | None = None, **kwargs):
        super().__init__(*args, directory=directory, **kwargs)

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, format, *args):  # noqa: A003
        print(f"[client] {self.address_string()} - {format % args}", flush=True)


def client_dir() -> Path:
    bundled = resource_root() / "client"
    if bundled.exists():
        return bundled
    raise SystemExit(f"Client assets not found at {bundled}")


def ensure_config() -> Path:
    cfg_dir = appdata_dir("Client")
    cfg = cfg_dir / "config.txt"
    if not cfg.exists():
        cfg.write_text(
            "\n".join(
                [
                    "# Ledgerly client settings",
                    "API_BASE=http://127.0.0.1:8000",
                    "OPEN_BROWSER=true",
                    "",
                ]
            ),
            encoding="utf-8",
        )
    return cfg


def read_config(cfg: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    for line in cfg.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        values[key.strip()] = value.strip()
    return values


def main() -> None:
    assets = client_dir()
    cfg_path = ensure_config()
    cfg = read_config(cfg_path)
    api_base = cfg.get("API_BASE", "http://127.0.0.1:8000")
    open_browser = cfg.get("OPEN_BROWSER", "true").lower() in {"1", "true", "yes"}

    os.chdir(assets)
    handler = functools.partial(SpaHandler, directory=str(assets))

    url = f"http://{HOST}:{PORT}/?api={api_base}"
    print("Ledgerly web client", flush=True)
    print(f"Serving UI from: {assets}", flush=True)
    print(f"Client URL: {url}", flush=True)
    print(f"Config file: {cfg_path}", flush=True)
    print("Press Ctrl+C to stop.", flush=True)

    with socketserver.ThreadingTCPServer((HOST, PORT), handler) as httpd:
        if open_browser:
            threading.Thread(
                target=lambda: (time.sleep(0.8), webbrowser.open(url)),
                daemon=True,
            ).start()
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\nClient stopped.", flush=True)


if __name__ == "__main__":
    if is_frozen():
        sys.path.insert(0, str(resource_root()))
    main()
