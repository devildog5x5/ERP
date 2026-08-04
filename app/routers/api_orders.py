from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from app.database import get_db
from app.schemas import (
    PurchaseOrderCreate,
    PurchaseOrderOut,
    ReceivePurchaseOrder,
    SalesOrderCreate,
    SalesOrderOut,
)
from app.services import purchasing as purchasing_service
from app.services import sales as sales_service

po_router = APIRouter(prefix="/api/purchase-orders", tags=["purchase-orders"])
so_router = APIRouter(prefix="/api/sales-orders", tags=["sales-orders"])


@po_router.get("", response_model=list[PurchaseOrderOut])
def list_pos(db: Session = Depends(get_db)):
    return purchasing_service.list_purchase_orders(db)


@po_router.get("/reorder-suggestions")
def reorder_suggestions(supplier_id: int | None = None, db: Session = Depends(get_db)):
    return purchasing_service.suggest_reorder_po(db, supplier_id)


@po_router.post("", response_model=PurchaseOrderOut)
def create_po(data: PurchaseOrderCreate, db: Session = Depends(get_db)):
    return purchasing_service.create_purchase_order(db, data)


@po_router.get("/{po_id}", response_model=PurchaseOrderOut)
def get_po(po_id: int, db: Session = Depends(get_db)):
    po = purchasing_service.get_purchase_order(db, po_id)
    if not po:
        raise HTTPException(status_code=404, detail="Purchase order not found")
    return po


@po_router.post("/{po_id}/place", response_model=PurchaseOrderOut)
def place_po(po_id: int, db: Session = Depends(get_db)):
    po = purchasing_service.get_purchase_order(db, po_id)
    if not po:
        raise HTTPException(status_code=404, detail="Purchase order not found")
    return purchasing_service.place_purchase_order(db, po)


@po_router.post("/{po_id}/receive", response_model=PurchaseOrderOut)
def receive_po(po_id: int, data: ReceivePurchaseOrder, db: Session = Depends(get_db)):
    po = purchasing_service.get_purchase_order(db, po_id)
    if not po:
        raise HTTPException(status_code=404, detail="Purchase order not found")
    return purchasing_service.receive_purchase_order(db, po, data)


@so_router.get("", response_model=list[SalesOrderOut])
def list_sos(db: Session = Depends(get_db)):
    return sales_service.list_sales_orders(db)


@so_router.post("", response_model=SalesOrderOut)
def create_so(data: SalesOrderCreate, db: Session = Depends(get_db)):
    return sales_service.create_sales_order(db, data)


@so_router.get("/{order_id}", response_model=SalesOrderOut)
def get_so(order_id: int, db: Session = Depends(get_db)):
    order = sales_service.get_sales_order(db, order_id)
    if not order:
        raise HTTPException(status_code=404, detail="Sales order not found")
    return order


@so_router.post("/{order_id}/fulfill", response_model=SalesOrderOut)
def fulfill_so(order_id: int, db: Session = Depends(get_db)):
    order = sales_service.get_sales_order(db, order_id)
    if not order:
        raise HTTPException(status_code=404, detail="Sales order not found")
    return sales_service.fulfill_sales_order(db, order)
