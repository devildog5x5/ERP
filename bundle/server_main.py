"""Packaged Ledgerly API server entry point."""

from __future__ import annotations

import os
import shutil
import sys
from pathlib import Path

# Ensure project root imports work when not frozen
ROOT = Path(__file__).resolve().parent.parent
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from bundle.paths import appdata_dir, is_frozen, resource_root  # noqa: E402
from bundle.wincompat import require_windows_10  # noqa: E402


def prepare_runtime() -> Path:
    data_dir = appdata_dir("Server")
    os.chdir(data_dir)

    env_path = data_dir / ".env"
    if not env_path.exists():
        example = resource_root() / ".env.example"
        if example.exists():
            shutil.copy(example, env_path)
        else:
            env_path.write_text(
                "\n".join(
                    [
                        "APP_NAME=Ledgerly ERP",
                        "DATABASE_URL=sqlite:///./erp.db",
                        "CORS_ORIGINS=http://127.0.0.1:3000,http://localhost:3000",
                        "EMAIL_ENABLED=true",
                        "ALERT_EMAIL_TO=owner@yourbusiness.com",
                        "",
                    ]
                ),
                encoding="utf-8",
            )

    # Prefer local AppData database unless user overrides
    os.environ.setdefault("DATABASE_URL", f"sqlite:///{(data_dir / 'erp.db').as_posix()}")

    # Seed on first launch when DB is missing
    db_path = data_dir / "erp.db"
    if not db_path.exists():
        try:
            from seed import seed

            seed()
            print("Seeded demo data into", db_path, flush=True)
        except Exception as exc:  # noqa: BLE001
            print("Warning: could not seed demo data:", exc, flush=True)

    return data_dir


def main() -> None:
    require_windows_10()
    data_dir = prepare_runtime()
    print("Ledgerly API server", flush=True)
    print("Compatible with Windows 10 and later", flush=True)
    print(f"Data directory: {data_dir}", flush=True)
    print("Listening on http://127.0.0.1:8000", flush=True)
    print("API docs: http://127.0.0.1:8000/docs", flush=True)

    import uvicorn

    # Import after chdir/.env so settings resolve correctly
    from app.config import get_settings

    get_settings.cache_clear()
    uvicorn.run(
        "app.main:app",
        host="127.0.0.1",
        port=8000,
        reload=False,
        log_level="info",
    )


if __name__ == "__main__":
    # When frozen, make bundled package importable
    if is_frozen():
        sys.path.insert(0, str(resource_root()))
    main()
