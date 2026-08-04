from __future__ import annotations

from datetime import date

from fastapi import HTTPException
from sqlalchemy import func, select
from sqlalchemy.orm import Session, joinedload

from app.models import (
    Product,
    PurchaseOrder,
    PurchaseOrderLine,
    PurchaseOrderStatus,
    StockMovementType,
    Supplier,
)
from app.schemas import PurchaseOrderCreate, ReceivePurchaseOrder
from app.services.inventory import apply_stock_change


def _next_po_number(db: Session) -> str:
    count = db.scalar(select(func.count()).select_from(PurchaseOrder)) or 0
    return f"PO-{date.today().strftime('%Y%m%d')}-{count + 1:04d}"


def list_purchase_orders(db: Session) -> list[PurchaseOrder]:
    return list(
        db.scalars(
            select(PurchaseOrder)
            .options(joinedload(PurchaseOrder.supplier), joinedload(PurchaseOrder.lines).joinedload(PurchaseOrderLine.product))
            .order_by(PurchaseOrder.created_at.desc())
        )
        .unique()
        .all()
    )


def get_purchase_order(db: Session, po_id: int) -> PurchaseOrder | None:
    return db.scalars(
        select(PurchaseOrder)
        .options(joinedload(PurchaseOrder.supplier), joinedload(PurchaseOrder.lines).joinedload(PurchaseOrderLine.product))
        .where(PurchaseOrder.id == po_id)
    ).unique().first()


def create_purchase_order(db: Session, data: PurchaseOrderCreate) -> PurchaseOrder:
    supplier = db.get(Supplier, data.supplier_id)
    if not supplier:
        raise HTTPException(status_code=404, detail="Supplier not found")
    if not data.lines:
        raise HTTPException(status_code=400, detail="At least one line is required")

    po = PurchaseOrder(
        po_number=_next_po_number(db),
        supplier_id=data.supplier_id,
        status=PurchaseOrderStatus.ORDERED if data.place_order else PurchaseOrderStatus.DRAFT,
        order_date=date.today(),
        expected_date=data.expected_date,
        notes=data.notes,
    )
    total = 0.0
    for line in data.lines:
        product = db.get(Product, line.product_id)
        if not product:
            raise HTTPException(status_code=404, detail=f"Product {line.product_id} not found")
        unit_cost = line.unit_cost if line.unit_cost is not None else product.unit_cost
        po.lines.append(
            PurchaseOrderLine(
                product_id=product.id,
                quantity_ordered=line.quantity_ordered,
                unit_cost=unit_cost,
            )
        )
        total += float(line.quantity_ordered) * float(unit_cost)
    po.total = total
    db.add(po)
    db.commit()
    return get_purchase_order(db, po.id)  # type: ignore[return-value]


def place_purchase_order(db: Session, po: PurchaseOrder) -> PurchaseOrder:
    if po.status != PurchaseOrderStatus.DRAFT:
        raise HTTPException(status_code=400, detail="Only draft POs can be placed")
    po.status = PurchaseOrderStatus.ORDERED
    db.commit()
    return get_purchase_order(db, po.id)  # type: ignore[return-value]


def receive_purchase_order(db: Session, po: PurchaseOrder, data: ReceivePurchaseOrder) -> PurchaseOrder:
    if po.status in {PurchaseOrderStatus.CANCELLED, PurchaseOrderStatus.RECEIVED}:
        raise HTTPException(status_code=400, detail="Cannot receive this purchase order")

    line_map = {line.id: line for line in po.lines}
    for item in data.lines:
        line = line_map.get(item.line_id)
        if not line:
            raise HTTPException(status_code=404, detail=f"Line {item.line_id} not found")
        remaining = float(line.quantity_ordered) - float(line.quantity_received)
        if item.quantity_received > remaining + 1e-9:
            raise HTTPException(
                status_code=400,
                detail=f"Cannot receive more than remaining qty for line {line.id}",
            )
        product = db.get(Product, line.product_id)
        if not product:
            raise HTTPException(status_code=404, detail="Product missing")
        apply_stock_change(
            db,
            product=product,
            delta=item.quantity_received,
            movement_type=StockMovementType.PURCHASE,
            reference_type="purchase_order",
            reference_id=po.id,
            notes=f"Received on {po.po_number}",
        )
        line.quantity_received = float(line.quantity_received) + float(item.quantity_received)
        if line.unit_cost:
            product.unit_cost = line.unit_cost

    fully_received = all(
        float(line.quantity_received) >= float(line.quantity_ordered) for line in po.lines
    )
    any_received = any(float(line.quantity_received) > 0 for line in po.lines)
    if fully_received:
        po.status = PurchaseOrderStatus.RECEIVED
        po.received_date = date.today()
    elif any_received:
        po.status = PurchaseOrderStatus.PARTIAL

    db.commit()
    return get_purchase_order(db, po.id)  # type: ignore[return-value]


def suggest_reorder_po(db: Session, supplier_id: int | None = None) -> dict:
    """Build a suggested PO payload from low-stock products."""
    products = list(db.scalars(select(Product).where(Product.is_active.is_(True))).all())
    low = [p for p in products if p.quantity_on_hand <= p.reorder_point]
    if supplier_id is not None:
        low = [p for p in low if p.supplier_id == supplier_id]

    by_supplier: dict[int, list[dict]] = {}
    for product in low:
        if not product.supplier_id:
            continue
        by_supplier.setdefault(product.supplier_id, []).append(
            {
                "product_id": product.id,
                "sku": product.sku,
                "name": product.name,
                "quantity_on_hand": product.quantity_on_hand,
                "reorder_point": product.reorder_point,
                "quantity_ordered": product.reorder_quantity,
                "unit_cost": product.unit_cost,
            }
        )
    return {"suggestions": by_supplier, "low_stock_count": len(low)}
