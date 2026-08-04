"""Launch the Ledgerly web client (static SPA)."""

from __future__ import annotations

import functools
import http.server
import os
import socketserver
from pathlib import Path

CLIENT_DIR = Path(__file__).resolve().parent / "client"
HOST = "127.0.0.1"
PORT = 3000


class SpaHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(CLIENT_DIR), **kwargs)

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, format, *args):  # noqa: A003
        print(f"[client] {self.address_string()} - {format % args}")


def main() -> None:
    if not CLIENT_DIR.exists():
        raise SystemExit(f"Client directory not found: {CLIENT_DIR}")

    os.chdir(CLIENT_DIR)
    handler = functools.partial(SpaHandler)
    with socketserver.ThreadingTCPServer((HOST, PORT), handler) as httpd:
        print(f"Ledgerly client running at http://{HOST}:{PORT}", flush=True)
        print("API expected at http://127.0.0.1:8000", flush=True)
        print("Press Ctrl+C to stop.", flush=True)
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\nClient stopped.")


if __name__ == "__main__":
    main()
