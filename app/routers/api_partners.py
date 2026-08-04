from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy import select
from sqlalchemy.orm import Session

from app.database import get_db
from app.models import Customer, Supplier
from app.schemas import (
    CustomerCreate,
    CustomerOut,
    CustomerUpdate,
    SupplierCreate,
    SupplierOut,
    SupplierUpdate,
)

suppliers_router = APIRouter(prefix="/api/suppliers", tags=["suppliers"])
customers_router = APIRouter(prefix="/api/customers", tags=["customers"])


@suppliers_router.get("", response_model=list[SupplierOut])
def list_suppliers(db: Session = Depends(get_db)):
    return list(db.scalars(select(Supplier).order_by(Supplier.name)).all())


@suppliers_router.post("", response_model=SupplierOut)
def create_supplier(data: SupplierCreate, db: Session = Depends(get_db)):
    supplier = Supplier(**data.model_dump())
    db.add(supplier)
    db.commit()
    db.refresh(supplier)
    return supplier


@suppliers_router.patch("/{supplier_id}", response_model=SupplierOut)
def update_supplier(supplier_id: int, data: SupplierUpdate, db: Session = Depends(get_db)):
    supplier = db.get(Supplier, supplier_id)
    if not supplier:
        raise HTTPException(status_code=404, detail="Supplier not found")
    for key, value in data.model_dump(exclude_unset=True).items():
        setattr(supplier, key, value)
    db.commit()
    db.refresh(supplier)
    return supplier


@customers_router.get("", response_model=list[CustomerOut])
def list_customers(db: Session = Depends(get_db)):
    return list(db.scalars(select(Customer).order_by(Customer.name)).all())


@customers_router.post("", response_model=CustomerOut)
def create_customer(data: CustomerCreate, db: Session = Depends(get_db)):
    customer = Customer(**data.model_dump())
    db.add(customer)
    db.commit()
    db.refresh(customer)
    return customer


@customers_router.patch("/{customer_id}", response_model=CustomerOut)
def update_customer(customer_id: int, data: CustomerUpdate, db: Session = Depends(get_db)):
    customer = db.get(Customer, customer_id)
    if not customer:
        raise HTTPException(status_code=404, detail="Customer not found")
    for key, value in data.model_dump(exclude_unset=True).items():
        setattr(customer, key, value)
    db.commit()
    db.refresh(customer)
    return customer
