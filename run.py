"""Compatibility launcher — starts the API server.

Prefer:
  python run_server.py   # API on :8000
  python run_client.py   # SPA on :3000
  .\\start.ps1           # both
"""

from run_server import *

if __name__ == "__main__":
    import uvicorn

    uvicorn.run("app.main:app", host="127.0.0.1", port=8000, reload=True)
