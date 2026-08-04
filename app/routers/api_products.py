from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from app.database import get_db
from app.schemas import ProductCreate, ProductOut, ProductUpdate, StockAdjust
from app.services import inventory as inventory_service

router = APIRouter(prefix="/api/products", tags=["products"])


@router.get("", response_model=list[ProductOut])
def list_products(low_stock: bool = False, db: Session = Depends(get_db)):
    products = inventory_service.list_products(db, low_stock_only=low_stock)
    return [ProductOut(**inventory_service.product_to_dict(p)) for p in products]


@router.post("", response_model=ProductOut)
def create_product(data: ProductCreate, db: Session = Depends(get_db)):
    product = inventory_service.create_product(db, data)
    return ProductOut(**inventory_service.product_to_dict(product))


@router.get("/{product_id}", response_model=ProductOut)
def get_product(product_id: int, db: Session = Depends(get_db)):
    product = inventory_service.get_product(db, product_id)
    if not product:
        raise HTTPException(status_code=404, detail="Product not found")
    return ProductOut(**inventory_service.product_to_dict(product))


@router.patch("/{product_id}", response_model=ProductOut)
def update_product(product_id: int, data: ProductUpdate, db: Session = Depends(get_db)):
    product = inventory_service.get_product(db, product_id)
    if not product:
        raise HTTPException(status_code=404, detail="Product not found")
    product = inventory_service.update_product(db, product, data)
    return ProductOut(**inventory_service.product_to_dict(product))


@router.post("/{product_id}/adjust", response_model=ProductOut)
def adjust_stock(product_id: int, data: StockAdjust, db: Session = Depends(get_db)):
    product = inventory_service.get_product(db, product_id)
    if not product:
        raise HTTPException(status_code=404, detail="Product not found")
    product = inventory_service.adjust_stock(db, product, data)
    return ProductOut(**inventory_service.product_to_dict(product))
