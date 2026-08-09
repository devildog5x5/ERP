using System;
using System.Collections.Generic;

namespace Ledgerly.Shared;

public class ProductDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string? Upc { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string Unit { get; set; } = "ea";
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal ReorderQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal AverageCost { get; set; }
    public decimal SellPrice { get; set; }
    public decimal? MarginPercent { get; set; }
    public int? SupplierId { get; set; }
    public int? TaxCodeId { get; set; }
    public bool TrackLots { get; set; }
    public bool TrackSerials { get; set; }
    public bool IsKit { get; set; }
    public bool NeedsReorder { get; set; }
}

public class ProductCreateDto
{
    public string Sku { get; set; } = "";
    public string? Upc { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string Unit { get; set; } = "ea";
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderPoint { get; set; } = 10;
    public decimal ReorderQuantity { get; set; } = 25;
    public decimal UnitCost { get; set; }
    public decimal SellPrice { get; set; }
    public int? SupplierId { get; set; }
    public int? TaxCodeId { get; set; }
    public bool TrackLots { get; set; }
    public bool TrackSerials { get; set; }
    public bool IsKit { get; set; }
}

public class StockAdjustDto
{
    public decimal QuantityDelta { get; set; }
    public string? Notes { get; set; }
}

public class ScanAdjustDto
{
    public string Code { get; set; } = "";
    public decimal QuantityDelta { get; set; } = 1;
    public string? Notes { get; set; }
}

public class ScanReceiveDto
{
    public int PurchaseOrderId { get; set; }
    public string Code { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
}

public class PartnerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class PartnerCreateDto
{
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class PurchaseOrderLineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductUpc { get; set; }
    public string? ProductName { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
}

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = "";
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal Total { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}

public class PurchaseOrderLineCreateDto
{
    public int ProductId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal? UnitCost { get; set; }
}

public class PurchaseOrderCreateDto
{
    public int SupplierId { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public string? Notes { get; set; }
    public List<PurchaseOrderLineCreateDto> Lines { get; set; } = new();
}

public class ReceiveLineDto
{
    public int LineId { get; set; }
    public decimal QuantityReceived { get; set; }
}

public class ReceivePurchaseOrderDto
{
    public List<ReceiveLineDto> Lines { get; set; } = new();
}

public class SalesOrderLineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductUpc { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SalesOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string DocumentType { get; set; } = "order";
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal MarginAmount { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? Notes { get; set; }
    public List<SalesOrderLineDto> Lines { get; set; } = new();
}

public class SalesOrderLineCreateDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

public class SalesOrderCreateDto
{
    public int CustomerId { get; set; }
    public string? Notes { get; set; }
    public decimal? TaxRate { get; set; }
    public string DocumentType { get; set; } = "order"; // quote|order|invoice
    public decimal DiscountAmount { get; set; }
    public int? LocationId { get; set; }
    public List<SalesOrderLineCreateDto> Lines { get; set; } = new();
}

public class ReminderDto
{
    public int Id { get; set; }
    public string ReminderType { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public bool IsResolved { get; set; }
    public bool EmailSent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReminderCreateDto
{
    public string ReminderType { get; set; } = "manual";
    public string Severity { get; set; } = "info";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
}

public class PurchaseOrderUpdateDto
{
    public int SupplierId { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public List<PurchaseOrderLineCreateDto>? Lines { get; set; }
}

public class SalesOrderUpdateDto
{
    public int CustomerId { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public decimal? TaxRate { get; set; }
    public List<SalesOrderLineCreateDto>? Lines { get; set; }
}

public class StockMovementDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductName { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal QuantityAfter { get; set; }
    public string Reason { get; set; } = "";
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SettingsDto
{
    public string CompanyName { get; set; } = "Ledgerly";
    public decimal DefaultTaxRate { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ReceiptFooter { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpEnableSsl { get; set; } = true;
    public string? SmtpFrom { get; set; }
    public decimal PoApprovalThreshold { get; set; } = 1000;
    public bool RequireLogin { get; set; } = true;
    public int? DefaultLocationId { get; set; }
}

public class DashboardDto
{
    public int ProductCount { get; set; }
    public int LowStockCount { get; set; }
    public int OpenPoCount { get; set; }
    public int OpenSoCount { get; set; }
    public decimal InventoryValue { get; set; }
    public int UnreadReminders { get; set; }
    public List<ProductDto> LowStockProducts { get; set; } = new();
    public List<ReminderDto> RecentReminders { get; set; } = new();
    public List<PurchaseOrderDto> PendingPurchaseOrders { get; set; } = new();
}

public class HealthDto
{
    public string Status { get; set; } = "ok";
    public string App { get; set; } = "Ledgerly ERP";
    public string Role { get; set; } = "api-server";
    public string DatabaseProvider { get; set; } = "Sqlite";
    public string Database { get; set; } = "";
    public string? ConfigPath { get; set; }
    public bool CanScaleOut { get; set; }
}
