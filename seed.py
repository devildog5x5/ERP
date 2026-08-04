"""Seed demo data for SmallBiz ERP."""

from __future__ import annotations

from datetime import date, timedelta

from sqlalchemy import func, select

from app.database import Base, SessionLocal, engine
from app.models import (
    Customer,
    Product,
    PurchaseOrder,
    PurchaseOrderLine,
    PurchaseOrderStatus,
    Supplier,
)
from app.services.reminders import scan_low_stock, scan_purchase_orders
from app.services.settings_service import get_app_settings


def seed() -> None:
    Base.metadata.create_all(bind=engine)
    db = SessionLocal()
    try:
        get_app_settings(db)
        if (db.scalar(select(func.count()).select_from(Product)) or 0) > 0:
            print("Database already has products; skipping seed.")
            return

        suppliers = [
            Supplier(
                name="Northwind Supplies",
                email="orders@northwind.example",
                phone="555-0101",
                address="12 Harbor Rd",
            ),
            Supplier(
                name="Summit Packaging Co",
                email="sales@summitpack.example",
                phone="555-0144",
                address="88 Ridge Ave",
            ),
        ]
        customers = [
            Customer(
                name="Cafe Lumen",
                email="hello@cafelumen.example",
                phone="555-0202",
                address="4 Market St",
            ),
            Customer(
                name="Harbor Retail",
                email="buyer@harborretail.example",
                phone="555-0218",
            ),
        ]
        db.add_all(suppliers + customers)
        db.flush()

        products = [
            Product(
                sku="COF-BEAN-1KG",
                name="Coffee Beans 1kg",
                category="Grocery",
                unit="bag",
                quantity_on_hand=4,
                reorder_point=12,
                reorder_quantity=24,
                unit_cost=8.5,
                sell_price=14.0,
                supplier_id=suppliers[0].id,
            ),
            Product(
                sku="CUP-12OZ",
                name="Paper Cups 12oz (50pk)",
                category="Packaging",
                unit="pack",
                quantity_on_hand=2,
                reorder_point=10,
                reorder_quantity=20,
                unit_cost=3.25,
                sell_price=6.5,
                supplier_id=suppliers[1].id,
            ),
            Product(
                sku="NAP-WHT",
                name="White Napkins",
                category="Packaging",
                unit="pack",
                quantity_on_hand=40,
                reorder_point=15,
                reorder_quantity=30,
                unit_cost=1.1,
                sell_price=2.5,
                supplier_id=suppliers[1].id,
            ),
            Product(
                sku="SYR-VAN",
                name="Vanilla Syrup",
                category="Grocery",
                unit="bottle",
                quantity_on_hand=0,
                reorder_point=6,
                reorder_quantity=12,
                unit_cost=4.75,
                sell_price=9.0,
                supplier_id=suppliers[0].id,
            ),
            Product(
                sku="FLT-PAPER",
                name="Coffee Filters",
                category="Consumables",
                unit="box",
                quantity_on_hand=18,
                reorder_point=8,
                reorder_quantity=16,
                unit_cost=2.0,
                sell_price=4.25,
                supplier_id=suppliers[0].id,
            ),
        ]
        db.add_all(products)
        db.flush()

        overdue_po = PurchaseOrder(
            po_number=f"PO-{date.today().strftime('%Y%m%d')}-0001",
            supplier_id=suppliers[0].id,
            status=PurchaseOrderStatus.ORDERED,
            order_date=date.today() - timedelta(days=10),
            expected_date=date.today() - timedelta(days=2),
            notes="Demo overdue PO for reminder testing",
            total=102.0,
        )
        overdue_po.lines.append(
            PurchaseOrderLine(
                product_id=products[0].id,
                quantity_ordered=12,
                unit_cost=8.5,
            )
        )
        incoming_po = PurchaseOrder(
            po_number=f"PO-{date.today().strftime('%Y%m%d')}-0002",
            supplier_id=suppliers[1].id,
            status=PurchaseOrderStatus.ORDERED,
            order_date=date.today() - timedelta(days=3),
            expected_date=date.today() + timedelta(days=1),
            notes="Demo incoming delivery",
            total=65.0,
        )
        incoming_po.lines.append(
            PurchaseOrderLine(
                product_id=products[1].id,
                quantity_ordered=20,
                unit_cost=3.25,
            )
        )
        db.add_all([overdue_po, incoming_po])
        db.commit()

        scan_low_stock(db)
        scan_purchase_orders(db)
        print("Seeded suppliers, customers, products, purchase orders, and reminders.")
    finally:
        db.close()


if __name__ == "__main__":
    seed()
