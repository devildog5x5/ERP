from __future__ import annotations

from datetime import date, datetime
from typing import Optional

from pydantic import BaseModel, ConfigDict, EmailStr, Field

from app.models import (
    PurchaseOrderStatus,
    ReminderSeverity,
    ReminderType,
    SalesOrderStatus,
)


class ORMModel(BaseModel):
    model_config = ConfigDict(from_attributes=True)


# --- Suppliers / Customers ---


class SupplierCreate(BaseModel):
    name: str
    email: Optional[EmailStr] = None
    phone: Optional[str] = None
    address: Optional[str] = None
    notes: Optional[str] = None
    is_active: bool = True


class SupplierUpdate(BaseModel):
    name: Optional[str] = None
    email: Optional[EmailStr] = None
    phone: Optional[str] = None
    address: Optional[str] = None
    notes: Optional[str] = None
    is_active: Optional[bool] = None


class SupplierOut(ORMModel):
    id: int
    name: str
    email: Optional[str] = None
    phone: Optional[str] = None
    address: Optional[str] = None
    notes: Optional[str] = None
    is_active: bool


class CustomerCreate(BaseModel):
    name: str
    email: Optional[EmailStr] = None
    phone: Optional[str] = None
    address: Optional[str] = None
    notes: Optional[str] = None
    is_active: bool = True


class CustomerUpdate(BaseModel):
    name: Optional[str] = None
    email: Optional[EmailStr] = None
    phone: Optional[str] = None
    address: Optional[str] = None
    notes: Optional[str] = None
    is_active: Optional[bool] = None


class CustomerOut(ORMModel):
    id: int
    name: str
    email: Optional[str] = None
    phone: Optional[str] = None
    address: Optional[str] = None
    notes: Optional[str] = None
    is_active: bool


# --- Products ---


class ProductCreate(BaseModel):
    sku: str
    name: str
    description: Optional[str] = None
    category: Optional[str] = None
    unit: str = "ea"
    quantity_on_hand: float = 0
    reorder_point: float = 10
    reorder_quantity: float = 25
    unit_cost: float = 0
    sell_price: float = 0
    supplier_id: Optional[int] = None
    is_active: bool = True


class ProductUpdate(BaseModel):
    sku: Optional[str] = None
    name: Optional[str] = None
    description: Optional[str] = None
    category: Optional[str] = None
    unit: Optional[str] = None
    reorder_point: Optional[float] = None
    reorder_quantity: Optional[float] = None
    unit_cost: Optional[float] = None
    sell_price: Optional[float] = None
    supplier_id: Optional[int] = None
    is_active: Optional[bool] = None


class StockAdjust(BaseModel):
    quantity_delta: float
    notes: Optional[str] = None


class ProductOut(ORMModel):
    id: int
    sku: str
    name: str
    description: Optional[str] = None
    category: Optional[str] = None
    unit: str
    quantity_on_hand: float
    reorder_point: float
    reorder_quantity: float
    unit_cost: float
    sell_price: float
    supplier_id: Optional[int] = None
    is_active: bool
    needs_reorder: bool = False


# --- Purchase Orders ---


class POLineCreate(BaseModel):
    product_id: int
    quantity_ordered: float = Field(gt=0)
    unit_cost: Optional[float] = None


class PurchaseOrderCreate(BaseModel):
    supplier_id: int
    expected_date: Optional[date] = None
    notes: Optional[str] = None
    lines: list[POLineCreate]
    place_order: bool = True


class ReceiveLine(BaseModel):
    line_id: int
    quantity_received: float = Field(gt=0)


class ReceivePurchaseOrder(BaseModel):
    lines: list[ReceiveLine]


class POLineOut(ORMModel):
    id: int
    product_id: int
    quantity_ordered: float
    quantity_received: float
    unit_cost: float
    product: Optional[ProductOut] = None


class PurchaseOrderOut(ORMModel):
    id: int
    po_number: str
    supplier_id: int
    status: PurchaseOrderStatus
    order_date: date
    expected_date: Optional[date] = None
    received_date: Optional[date] = None
    notes: Optional[str] = None
    total: float
    supplier: Optional[SupplierOut] = None
    lines: list[POLineOut] = []


# --- Sales Orders ---


class SOLineCreate(BaseModel):
    product_id: int
    quantity: float = Field(gt=0)
    unit_price: Optional[float] = None


class SalesOrderCreate(BaseModel):
    customer_id: int
    notes: Optional[str] = None
    lines: list[SOLineCreate]
    fulfill: bool = True


class SOLineOut(ORMModel):
    id: int
    product_id: int
    quantity: float
    unit_price: float
    product: Optional[ProductOut] = None


class SalesOrderOut(ORMModel):
    id: int
    order_number: str
    customer_id: int
    status: SalesOrderStatus
    order_date: date
    notes: Optional[str] = None
    total: float
    customer: Optional[CustomerOut] = None
    lines: list[SOLineOut] = []


# --- Reminders / Settings / Dashboard ---


class ReminderOut(ORMModel):
    id: int
    reminder_type: ReminderType
    severity: ReminderSeverity
    title: str
    message: str
    related_entity_type: Optional[str] = None
    related_entity_id: Optional[int] = None
    product_id: Optional[int] = None
    is_read: bool
    is_resolved: bool
    email_sent: bool
    email_sent_at: Optional[datetime] = None
    created_at: datetime


class SettingsUpdate(BaseModel):
    alert_email_to: Optional[str] = None
    email_enabled: Optional[bool] = None
    reminder_interval_minutes: Optional[int] = None
    low_stock_email: Optional[bool] = None
    po_overdue_email: Optional[bool] = None


class SettingsOut(BaseModel):
    alert_email_to: str
    email_enabled: bool
    reminder_interval_minutes: int
    low_stock_email: bool
    po_overdue_email: bool
    smtp_configured: bool


class DashboardOut(BaseModel):
    product_count: int
    low_stock_count: int
    open_po_count: int
    open_so_count: int
    inventory_value: float
    unread_reminders: int
    low_stock_products: list[ProductOut]
    recent_reminders: list[ReminderOut]
    pending_purchase_orders: list[PurchaseOrderOut]
