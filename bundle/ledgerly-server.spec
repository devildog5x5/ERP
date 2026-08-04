# -*- mode: python ; coding: utf-8 -*-
from pathlib import Path

from PyInstaller.utils.hooks import collect_all, collect_submodules

block_cipher = None
root = Path(SPECPATH).resolve().parent

datas = [
    (str(root / ".env.example"), "."),
    (str(root / "seed.py"), "."),
]

hiddenimports = [
    "uvicorn.logging",
    "uvicorn.loops",
    "uvicorn.loops.auto",
    "uvicorn.protocols",
    "uvicorn.protocols.http",
    "uvicorn.protocols.http.auto",
    "uvicorn.protocols.websockets",
    "uvicorn.protocols.websockets.auto",
    "uvicorn.lifespan",
    "uvicorn.lifespan.on",
    "apscheduler.triggers.interval",
    "apscheduler.schedulers.asyncio",
    "email_validator",
    "app",
    "app.main",
    "app.config",
    "app.database",
    "app.models",
    "app.schemas",
    "app.routers.api_products",
    "app.routers.api_partners",
    "app.routers.api_orders",
    "app.routers.api_system",
    "app.services.inventory",
    "app.services.purchasing",
    "app.services.sales",
    "app.services.reminders",
    "app.services.email_service",
    "app.services.dashboard",
    "app.services.settings_service",
    "seed",
]

for pkg in ("uvicorn", "fastapi", "starlette", "pydantic", "pydantic_settings", "anyio", "aiosmtplib"):
    tmp_ret = collect_all(pkg)
    datas += tmp_ret[0]
    hiddenimports += tmp_ret[1]
    # binaries ignored for pure python packages mostly

hiddenimports += collect_submodules("app")
hiddenimports += collect_submodules("sqlalchemy")

a = Analysis(
    [str(root / "bundle" / "server_main.py")],
    pathex=[str(root)],
    binaries=[],
    datas=datas,
    hiddenimports=hiddenimports + ["bundle", "bundle.paths", "bundle.server_main"],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name="LedgerlyServer",
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    console=True,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)

coll = COLLECT(
    exe,
    a.binaries,
    a.zipfiles,
    a.datas,
    strip=False,
    upx=True,
    upx_exclude=[],
    name="LedgerlyServer",
)
