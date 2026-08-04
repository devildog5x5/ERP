from __future__ import annotations

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.config import get_settings
from app.models import AppSetting
from app.schemas import SettingsOut, SettingsUpdate

DEFAULTS = {
    "alert_email_to": None,  # filled from env
    "email_enabled": "true",
    "reminder_interval_minutes": None,
    "low_stock_email": "true",
    "po_overdue_email": "true",
}


def _ensure_defaults(db: Session) -> None:
    env = get_settings()
    defaults = {
        "alert_email_to": env.alert_email_to,
        "email_enabled": "true" if env.email_enabled else "false",
        "reminder_interval_minutes": str(env.reminder_interval_minutes),
        "low_stock_email": "true",
        "po_overdue_email": "true",
    }
    existing = {s.key: s for s in db.scalars(select(AppSetting)).all()}
    for key, value in defaults.items():
        if key not in existing:
            db.add(AppSetting(key=key, value=value))
    db.commit()


def get_setting(db: Session, key: str, fallback: str = "") -> str:
    row = db.scalar(select(AppSetting).where(AppSetting.key == key))
    return row.value if row else fallback


def set_setting(db: Session, key: str, value: str) -> None:
    row = db.scalar(select(AppSetting).where(AppSetting.key == key))
    if row:
        row.value = value
    else:
        db.add(AppSetting(key=key, value=value))
    db.commit()


def get_app_settings(db: Session) -> SettingsOut:
    _ensure_defaults(db)
    env = get_settings()
    return SettingsOut(
        alert_email_to=get_setting(db, "alert_email_to", env.alert_email_to),
        email_enabled=get_setting(db, "email_enabled", "true").lower() == "true",
        reminder_interval_minutes=int(
            get_setting(db, "reminder_interval_minutes", str(env.reminder_interval_minutes))
        ),
        low_stock_email=get_setting(db, "low_stock_email", "true").lower() == "true",
        po_overdue_email=get_setting(db, "po_overdue_email", "true").lower() == "true",
        smtp_configured=bool(env.smtp_host),
    )


def update_app_settings(db: Session, data: SettingsUpdate) -> SettingsOut:
    payload = data.model_dump(exclude_unset=True)
    mapping = {
        "alert_email_to": lambda v: str(v),
        "email_enabled": lambda v: "true" if v else "false",
        "reminder_interval_minutes": lambda v: str(int(v)),
        "low_stock_email": lambda v: "true" if v else "false",
        "po_overdue_email": lambda v: "true" if v else "false",
    }
    for key, value in payload.items():
        if key in mapping:
            set_setting(db, key, mapping[key](value))
    return get_app_settings(db)
