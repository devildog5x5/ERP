from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from app.database import get_db
from app.models import Reminder
from app.schemas import DashboardOut, ReminderOut, SettingsOut, SettingsUpdate
from app.services import reminders as reminder_service
from app.services.dashboard import get_dashboard
from app.services.settings_service import get_app_settings, update_app_settings

router = APIRouter(prefix="/api", tags=["system"])


@router.get("/dashboard", response_model=DashboardOut)
def dashboard(db: Session = Depends(get_db)):
    return get_dashboard(db)


@router.get("/reminders", response_model=list[ReminderOut])
def reminders(unresolved_only: bool = True, db: Session = Depends(get_db)):
    return reminder_service.list_reminders(db, unresolved_only=unresolved_only)


@router.post("/reminders/run", response_model=dict)
async def run_reminders():
    return await reminder_service.run_reminder_cycle()


@router.post("/reminders/{reminder_id}/read", response_model=ReminderOut)
def read_reminder(reminder_id: int, db: Session = Depends(get_db)):
    reminder = db.get(Reminder, reminder_id)
    if not reminder:
        raise HTTPException(status_code=404, detail="Reminder not found")
    return reminder_service.mark_read(db, reminder)


@router.post("/reminders/{reminder_id}/resolve", response_model=ReminderOut)
def resolve_reminder(reminder_id: int, db: Session = Depends(get_db)):
    reminder = db.get(Reminder, reminder_id)
    if not reminder:
        raise HTTPException(status_code=404, detail="Reminder not found")
    return reminder_service.resolve_reminder(db, reminder)


@router.get("/settings", response_model=SettingsOut)
def settings_get(db: Session = Depends(get_db)):
    return get_app_settings(db)


@router.put("/settings", response_model=SettingsOut)
def settings_put(data: SettingsUpdate, db: Session = Depends(get_db)):
    return update_app_settings(db, data)
