from __future__ import annotations

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.models import Product, StockMovement, StockMovementType
from app.schemas import ProductCreate, ProductUpdate, StockAdjust


def list_products(db: Session, *, low_stock_only: bool = False) -> list[Product]:
    products = list(db.scalars(select(Product).order_by(Product.name)).all())
    if low_stock_only:
        return [p for p in products if p.quantity_on_hand <= p.reorder_point]
    return products


def get_product(db: Session, product_id: int) -> Product | None:
    return db.get(Product, product_id)


def create_product(db: Session, data: ProductCreate) -> Product:
    product = Product(**data.model_dump())
    db.add(product)
    db.commit()
    db.refresh(product)
    return product


def update_product(db: Session, product: Product, data: ProductUpdate) -> Product:
    for key, value in data.model_dump(exclude_unset=True).items():
        setattr(product, key, value)
    db.commit()
    db.refresh(product)
    return product


def adjust_stock(db: Session, product: Product, data: StockAdjust) -> Product:
    product.quantity_on_hand = float(product.quantity_on_hand) + float(data.quantity_delta)
    movement = StockMovement(
        product_id=product.id,
        movement_type=StockMovementType.ADJUSTMENT,
        quantity_delta=data.quantity_delta,
        reference_type="adjustment",
        notes=data.notes,
    )
    db.add(movement)
    db.commit()
    db.refresh(product)
    return product


def apply_stock_change(
    db: Session,
    *,
    product: Product,
    delta: float,
    movement_type: StockMovementType,
    reference_type: str,
    reference_id: int | None = None,
    notes: str | None = None,
    commit: bool = False,
) -> None:
    product.quantity_on_hand = float(product.quantity_on_hand) + float(delta)
    db.add(
        StockMovement(
            product_id=product.id,
            movement_type=movement_type,
            quantity_delta=delta,
            reference_type=reference_type,
            reference_id=reference_id,
            notes=notes,
        )
    )
    if commit:
        db.commit()


def product_to_dict(product: Product) -> dict:
    return {
        "id": product.id,
        "sku": product.sku,
        "name": product.name,
        "description": product.description,
        "category": product.category,
        "unit": product.unit,
        "quantity_on_hand": product.quantity_on_hand,
        "reorder_point": product.reorder_point,
        "reorder_quantity": product.reorder_quantity,
        "unit_cost": product.unit_cost,
        "sell_price": product.sell_price,
        "supplier_id": product.supplier_id,
        "is_active": product.is_active,
        "needs_reorder": product.quantity_on_hand <= product.reorder_point,
    }
