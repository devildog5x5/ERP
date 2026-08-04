from __future__ import annotations

import logging
from datetime import date, datetime, timedelta

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.config import get_settings
from app.database import SessionLocal
from app.models import (
    Product,
    PurchaseOrder,
    PurchaseOrderStatus,
    Reminder,
    ReminderSeverity,
    ReminderType,
)
from app.services.email_service import send_email
from app.services.settings_service import get_app_settings

logger = logging.getLogger("erp.reminders")


def list_reminders(db: Session, *, unresolved_only: bool = False) -> list[Reminder]:
    stmt = select(Reminder).order_by(Reminder.created_at.desc())
    if unresolved_only:
        stmt = stmt.where(Reminder.is_resolved.is_(False))
    return list(db.scalars(stmt).all())


def mark_read(db: Session, reminder: Reminder) -> Reminder:
    reminder.is_read = True
    db.commit()
    db.refresh(reminder)
    return reminder


def resolve_reminder(db: Session, reminder: Reminder) -> Reminder:
    reminder.is_resolved = True
    reminder.is_read = True
    db.commit()
    db.refresh(reminder)
    return reminder


def _find_open_reminder(
    db: Session,
    reminder_type: ReminderType,
    *,
    product_id: int | None = None,
    related_entity_type: str | None = None,
    related_entity_id: int | None = None,
) -> Reminder | None:
    stmt = select(Reminder).where(
        Reminder.reminder_type == reminder_type,
        Reminder.is_resolved.is_(False),
    )
    if product_id is not None:
        stmt = stmt.where(Reminder.product_id == product_id)
    if related_entity_type is not None:
        stmt = stmt.where(Reminder.related_entity_type == related_entity_type)
    if related_entity_id is not None:
        stmt = stmt.where(Reminder.related_entity_id == related_entity_id)
    return db.scalars(stmt).first()


def _upsert_reminder(
    db: Session,
    *,
    reminder_type: ReminderType,
    severity: ReminderSeverity,
    title: str,
    message: str,
    product_id: int | None = None,
    related_entity_type: str | None = None,
    related_entity_id: int | None = None,
) -> Reminder:
    existing = _find_open_reminder(
        db,
        reminder_type,
        product_id=product_id,
        related_entity_type=related_entity_type,
        related_entity_id=related_entity_id,
    )
    if existing:
        existing.title = title
        existing.message = message
        existing.severity = severity
        existing.updated_at = datetime.utcnow()
        db.commit()
        db.refresh(existing)
        return existing

    reminder = Reminder(
        reminder_type=reminder_type,
        severity=severity,
        title=title,
        message=message,
        product_id=product_id,
        related_entity_type=related_entity_type,
        related_entity_id=related_entity_id,
    )
    db.add(reminder)
    db.commit()
    db.refresh(reminder)
    return reminder


def scan_low_stock(db: Session) -> list[Reminder]:
    created: list[Reminder] = []
    products = list(db.scalars(select(Product).where(Product.is_active.is_(True))).all())
    low_ids: set[int] = set()

    for product in products:
        if product.quantity_on_hand > product.reorder_point:
            continue
        low_ids.add(product.id)
        severity = (
            ReminderSeverity.CRITICAL
            if product.quantity_on_hand <= 0
            else ReminderSeverity.WARNING
        )
        qty_needed = max(product.reorder_quantity, product.reorder_point - product.quantity_on_hand)
        title = f"Low stock: {product.name}"
        message = (
            f"{product.name} ({product.sku}) is at {product.quantity_on_hand} {product.unit}. "
            f"Reorder point is {product.reorder_point}. "
            f"Suggested buy quantity: {qty_needed} {product.unit}."
        )
        reminder = _upsert_reminder(
            db,
            reminder_type=ReminderType.LOW_STOCK,
            severity=severity,
            title=title,
            message=message,
            product_id=product.id,
            related_entity_type="product",
            related_entity_id=product.id,
        )
        created.append(reminder)

        # Companion reorder suggestion reminder
        created.append(
            _upsert_reminder(
                db,
                reminder_type=ReminderType.REORDER_SUGGESTED,
                severity=ReminderSeverity.INFO,
                title=f"Buy suggested: {product.name}",
                message=(
                    f"Create a purchase order for {qty_needed} {product.unit} of "
                    f"{product.name} ({product.sku}). Estimated cost: "
                    f"${qty_needed * product.unit_cost:,.2f}."
                ),
                product_id=product.id,
                related_entity_type="product",
                related_entity_id=product.id,
            )
        )

    # Auto-resolve stock reminders when stock recovers
    open_stock = db.scalars(
        select(Reminder).where(
            Reminder.is_resolved.is_(False),
            Reminder.reminder_type.in_(
                [ReminderType.LOW_STOCK, ReminderType.REORDER_SUGGESTED]
            ),
        )
    ).all()
    for reminder in open_stock:
        if reminder.product_id and reminder.product_id not in low_ids:
            reminder.is_resolved = True
            reminder.is_read = True
    db.commit()
    return created


def scan_purchase_orders(db: Session) -> list[Reminder]:
    created: list[Reminder] = []
    today = date.today()
    open_statuses = {
        PurchaseOrderStatus.ORDERED,
        PurchaseOrderStatus.PARTIAL,
        PurchaseOrderStatus.DRAFT,
    }
    orders = list(
        db.scalars(
            select(PurchaseOrder).where(PurchaseOrder.status.in_(open_statuses))
        ).all()
    )
    active_ids: set[int] = set()

    for po in orders:
        if not po.expected_date:
            continue
        active_ids.add(po.id)
        if po.expected_date < today:
            created.append(
                _upsert_reminder(
                    db,
                    reminder_type=ReminderType.PO_OVERDUE,
                    severity=ReminderSeverity.CRITICAL,
                    title=f"Overdue purchase order {po.po_number}",
                    message=(
                        f"{po.po_number} was expected on {po.expected_date.isoformat()} "
                        f"and is still {po.status.value}. Follow up with the supplier and "
                        f"receive stock when it arrives."
                    ),
                    related_entity_type="purchase_order",
                    related_entity_id=po.id,
                )
            )
        elif po.expected_date <= today + timedelta(days=2):
            created.append(
                _upsert_reminder(
                    db,
                    reminder_type=ReminderType.PO_EXPECTED,
                    severity=ReminderSeverity.WARNING,
                    title=f"Incoming delivery {po.po_number}",
                    message=(
                        f"{po.po_number} is expected on {po.expected_date.isoformat()}. "
                        f"Prepare to receive and update inventory quantities."
                    ),
                    related_entity_type="purchase_order",
                    related_entity_id=po.id,
                )
            )

    open_po_reminders = db.scalars(
        select(Reminder).where(
            Reminder.is_resolved.is_(False),
            Reminder.reminder_type.in_([ReminderType.PO_OVERDUE, ReminderType.PO_EXPECTED]),
        )
    ).all()
    for reminder in open_po_reminders:
        if reminder.related_entity_id and reminder.related_entity_id not in active_ids:
            reminder.is_resolved = True
            reminder.is_read = True
    db.commit()
    return created


async def dispatch_emails(db: Session) -> int:
    settings = get_app_settings(db)
    env = get_settings()
    if not settings.email_enabled:
        return 0

    pending = list(
        db.scalars(
            select(Reminder).where(
                Reminder.is_resolved.is_(False),
                Reminder.email_sent.is_(False),
            )
        ).all()
    )
    sent_count = 0
    for reminder in pending:
        if reminder.reminder_type in {
            ReminderType.LOW_STOCK,
            ReminderType.REORDER_SUGGESTED,
        } and not settings.low_stock_email:
            continue
        if reminder.reminder_type in {
            ReminderType.PO_OVERDUE,
            ReminderType.PO_EXPECTED,
        } and not settings.po_overdue_email:
            continue

        subject = f"[{env.app_name}] {reminder.title}"
        body = (
            f"{reminder.message}\n\n"
            f"Severity: {reminder.severity.value}\n"
            f"Type: {reminder.reminder_type.value}\n"
            f"Created: {reminder.created_at.isoformat()}\n\n"
            f"Open the ERP reminders page to acknowledge or resolve this alert."
        )
        ok = await send_email(subject, body, to_address=settings.alert_email_to)
        if ok:
            reminder.email_sent = True
            reminder.email_sent_at = datetime.utcnow()
            sent_count += 1
    db.commit()
    return sent_count


async def run_reminder_cycle() -> dict:
    db = SessionLocal()
    try:
        low = scan_low_stock(db)
        pos = scan_purchase_orders(db)
        emailed = await dispatch_emails(db)
        logger.info(
            "Reminder cycle complete: low_stock=%s po=%s emails=%s",
            len(low),
            len(pos),
            emailed,
        )
        return {
            "low_stock_reminders": len(low),
            "po_reminders": len(pos),
            "emails_sent": emailed,
        }
    finally:
        db.close()
