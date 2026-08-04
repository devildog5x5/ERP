from __future__ import annotations

from sqlalchemy import func, select
from sqlalchemy.orm import Session, joinedload

from app.models import (
    Product,
    PurchaseOrder,
    PurchaseOrderStatus,
    Reminder,
    SalesOrder,
    SalesOrderStatus,
)
from app.schemas import DashboardOut, ProductOut, PurchaseOrderOut, ReminderOut
from app.services.inventory import product_to_dict


def get_dashboard(db: Session) -> DashboardOut:
    products = list(db.scalars(select(Product).where(Product.is_active.is_(True))).all())
    low_stock = [p for p in products if p.quantity_on_hand <= p.reorder_point]
    inventory_value = sum(p.quantity_on_hand * p.unit_cost for p in products)

    open_po_count = (
        db.scalar(
            select(func.count())
            .select_from(PurchaseOrder)
            .where(
                PurchaseOrder.status.in_(
                    [
                        PurchaseOrderStatus.DRAFT,
                        PurchaseOrderStatus.ORDERED,
                        PurchaseOrderStatus.PARTIAL,
                    ]
                )
            )
        )
        or 0
    )
    open_so_count = (
        db.scalar(
            select(func.count())
            .select_from(SalesOrder)
            .where(
                SalesOrder.status.in_(
                    [SalesOrderStatus.DRAFT, SalesOrderStatus.CONFIRMED]
                )
            )
        )
        or 0
    )
    unread = (
        db.scalar(
            select(func.count())
            .select_from(Reminder)
            .where(Reminder.is_resolved.is_(False), Reminder.is_read.is_(False))
        )
        or 0
    )

    recent_reminders = list(
        db.scalars(
            select(Reminder)
            .where(Reminder.is_resolved.is_(False))
            .order_by(Reminder.created_at.desc())
            .limit(8)
        ).all()
    )
    pending_pos = list(
        db.scalars(
            select(PurchaseOrder)
            .options(joinedload(PurchaseOrder.supplier), joinedload(PurchaseOrder.lines))
            .where(
                PurchaseOrder.status.in_(
                    [
                        PurchaseOrderStatus.ORDERED,
                        PurchaseOrderStatus.PARTIAL,
                        PurchaseOrderStatus.DRAFT,
                    ]
                )
            )
            .order_by(PurchaseOrder.expected_date.asc().nullslast())
            .limit(8)
        )
        .unique()
        .all()
    )

    return DashboardOut(
        product_count=len(products),
        low_stock_count=len(low_stock),
        open_po_count=int(open_po_count),
        open_so_count=int(open_so_count),
        inventory_value=round(inventory_value, 2),
        unread_reminders=int(unread),
        low_stock_products=[ProductOut(**product_to_dict(p)) for p in low_stock[:10]],
        recent_reminders=[ReminderOut.model_validate(r) for r in recent_reminders],
        pending_purchase_orders=[PurchaseOrderOut.model_validate(p) for p in pending_pos],
    )
