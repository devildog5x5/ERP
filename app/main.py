from __future__ import annotations

import logging
from contextlib import asynccontextmanager

from apscheduler.schedulers.asyncio import AsyncIOScheduler
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.config import get_settings
from app.database import Base, SessionLocal, engine
from app.routers import api_orders, api_partners, api_products, api_system
from app.services.reminders import run_reminder_cycle
from app.services.settings_service import get_app_settings

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("erp")

scheduler = AsyncIOScheduler()
settings = get_settings()


async def scheduled_reminder_job() -> None:
    await run_reminder_cycle()


@asynccontextmanager
async def lifespan(_: FastAPI):
    Base.metadata.create_all(bind=engine)
    db = SessionLocal()
    try:
        app_settings = get_app_settings(db)
        interval = max(1, app_settings.reminder_interval_minutes)
    finally:
        db.close()

    scheduler.add_job(
        scheduled_reminder_job,
        "interval",
        minutes=interval,
        id="reminder_cycle",
        replace_existing=True,
    )
    scheduler.start()
    logger.info("Reminder scheduler started (every %s minutes)", interval)

    try:
        await run_reminder_cycle()
    except Exception:
        logger.exception("Initial reminder cycle failed")

    yield
    scheduler.shutdown(wait=False)


app = FastAPI(
    title=settings.app_name,
    description="Ledgerly ERP API server",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origin_list,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(api_products.router)
app.include_router(api_partners.suppliers_router)
app.include_router(api_partners.customers_router)
app.include_router(api_orders.po_router)
app.include_router(api_orders.so_router)
app.include_router(api_system.router)


@app.get("/api/health")
def health():
    return {
        "status": "ok",
        "app": settings.app_name,
        "role": "api-server",
    }
