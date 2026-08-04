from __future__ import annotations

from datetime import date

from fastapi import HTTPException
from sqlalchemy import func, select
from sqlalchemy.orm import Session, joinedload

from app.models import (
    Customer,
    Product,
    SalesOrder,
    SalesOrderLine,
    SalesOrderStatus,
    StockMovementType,
)
from app.schemas import SalesOrderCreate
from app.services.inventory import apply_stock_change


def _next_so_number(db: Session) -> str:
    count = db.scalar(select(func.count()).select_from(SalesOrder)) or 0
    return f"SO-{date.today().strftime('%Y%m%d')}-{count + 1:04d}"


def list_sales_orders(db: Session) -> list[SalesOrder]:
    return list(
        db.scalars(
            select(SalesOrder)
            .options(
                joinedload(SalesOrder.customer),
                joinedload(SalesOrder.lines).joinedload(SalesOrderLine.product),
            )
            .order_by(SalesOrder.created_at.desc())
        )
        .unique()
        .all()
    )


def get_sales_order(db: Session, order_id: int) -> SalesOrder | None:
    return (
        db.scalars(
            select(SalesOrder)
            .options(
                joinedload(SalesOrder.customer),
                joinedload(SalesOrder.lines).joinedload(SalesOrderLine.product),
            )
            .where(SalesOrder.id == order_id)
        )
        .unique()
        .first()
    )


def create_sales_order(db: Session, data: SalesOrderCreate) -> SalesOrder:
    customer = db.get(Customer, data.customer_id)
    if not customer:
        raise HTTPException(status_code=404, detail="Customer not found")
    if not data.lines:
        raise HTTPException(status_code=400, detail="At least one line is required")

    order = SalesOrder(
        order_number=_next_so_number(db),
        customer_id=data.customer_id,
        status=SalesOrderStatus.CONFIRMED,
        order_date=date.today(),
        notes=data.notes,
    )
    total = 0.0
    reserved: list[tuple[Product, float]] = []

    for line in data.lines:
        product = db.get(Product, line.product_id)
        if not product:
            raise HTTPException(status_code=404, detail=f"Product {line.product_id} not found")
        if data.fulfill and product.quantity_on_hand < line.quantity:
            raise HTTPException(
                status_code=400,
                detail=f"Insufficient stock for {product.sku}: have {product.quantity_on_hand}, need {line.quantity}",
            )
        unit_price = line.unit_price if line.unit_price is not None else product.sell_price
        order.lines.append(
            SalesOrderLine(
                product_id=product.id,
                quantity=line.quantity,
                unit_price=unit_price,
            )
        )
        total += float(line.quantity) * float(unit_price)
        reserved.append((product, float(line.quantity)))

    order.total = total
    if data.fulfill:
        order.status = SalesOrderStatus.FULFILLED
    db.add(order)
    db.flush()

    if data.fulfill:
        for product, qty in reserved:
            apply_stock_change(
                db,
                product=product,
                delta=-qty,
                movement_type=StockMovementType.SALE,
                reference_type="sales_order",
                reference_id=order.id,
                notes=f"Sold on {order.order_number}",
            )

    db.commit()
    return get_sales_order(db, order.id)  # type: ignore[return-value]


def fulfill_sales_order(db: Session, order: SalesOrder) -> SalesOrder:
    if order.status == SalesOrderStatus.FULFILLED:
        raise HTTPException(status_code=400, detail="Order already fulfilled")
    if order.status == SalesOrderStatus.CANCELLED:
        raise HTTPException(status_code=400, detail="Cannot fulfill cancelled order")

    for line in order.lines:
        product = db.get(Product, line.product_id)
        if not product:
            raise HTTPException(status_code=404, detail="Product missing")
        if product.quantity_on_hand < line.quantity:
            raise HTTPException(
                status_code=400,
                detail=f"Insufficient stock for {product.sku}",
            )
        apply_stock_change(
            db,
            product=product,
            delta=-float(line.quantity),
            movement_type=StockMovementType.SALE,
            reference_type="sales_order",
            reference_id=order.id,
            notes=f"Sold on {order.order_number}",
        )

    order.status = SalesOrderStatus.FULFILLED
    db.commit()
    return get_sales_order(db, order.id)  # type: ignore[return-value]
