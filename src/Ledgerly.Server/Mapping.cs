using System;
using System.Linq;
using Ledgerly.Server.Data;
using Ledgerly.Shared;

namespace Ledgerly.Server;

public static class Mapping
{
    public static ProductDto ToDto(this Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Upc = p.Upc,
        Name = p.Name,
        Category = p.Category,
        Unit = p.Unit,
        QuantityOnHand = p.QuantityOnHand,
        ReorderPoint = p.ReorderPoint,
        ReorderQuantity = p.ReorderQuantity,
        UnitCost = p.UnitCost,
        AverageCost = p.AverageCost > 0 ? p.AverageCost : p.UnitCost,
        SellPrice = p.SellPrice,
        MarginPercent = p.SellPrice <= 0 ? 0 :
            Math.Round((p.SellPrice - (p.AverageCost > 0 ? p.AverageCost : p.UnitCost)) / p.SellPrice * 100m, 2),
        SupplierId = p.SupplierId,
        TaxCodeId = p.TaxCodeId,
        TrackLots = p.TrackLots,
        TrackSerials = p.TrackSerials,
        IsKit = p.IsKit,
        NeedsReorder = p.QuantityOnHand <= p.ReorderPoint
    };

    public static PartnerDto ToDto(this Supplier s) => new()
    {
        Id = s.Id, Name = s.Name, Email = s.Email, Phone = s.Phone, Address = s.Address
    };

    public static PartnerDto ToDto(this Customer c) => new()
    {
        Id = c.Id, Name = c.Name, Email = c.Email, Phone = c.Phone, Address = c.Address
    };

    public static ReminderDto ToDto(this Reminder r) => new()
    {
        Id = r.Id,
        ReminderType = r.ReminderType,
        Severity = r.Severity,
        Title = r.Title,
        Message = r.Message,
        IsRead = r.IsRead,
        IsResolved = r.IsResolved,
        EmailSent = r.EmailSent,
        CreatedAt = r.CreatedAt
    };

    public static StockMovementDto ToDto(this StockMovement m) => new()
    {
        Id = m.Id,
        ProductId = m.ProductId,
        ProductSku = m.Product?.Sku,
        ProductName = m.Product?.Name,
        QuantityDelta = m.QuantityDelta,
        QuantityAfter = m.QuantityAfter,
        Reason = m.Reason,
        ReferenceType = m.ReferenceType,
        ReferenceId = m.ReferenceId,
        Notes = m.Notes,
        CreatedAt = m.CreatedAt
    };

    public static SettingsDto ToDto(this CompanySettings s) => new()
    {
        CompanyName = s.CompanyName,
        DefaultTaxRate = s.DefaultTaxRate,
        Currency = s.Currency,
        ReceiptFooter = s.ReceiptFooter,
        Address = s.Address,
        Phone = s.Phone,
        Email = s.Email,
        SmtpHost = s.SmtpHost,
        SmtpPort = s.SmtpPort,
        SmtpUsername = s.SmtpUsername,
        // Never return the stored SMTP password to clients.
        SmtpPassword = string.IsNullOrEmpty(s.SmtpPassword) ? null : "********",
        SmtpEnableSsl = s.SmtpEnableSsl,
        SmtpFrom = s.SmtpFrom,
        PoApprovalThreshold = s.PoApprovalThreshold,
        RequireLogin = s.RequireLogin,
        DefaultLocationId = s.DefaultLocationId
    };

    public static PurchaseOrderDto ToDto(this PurchaseOrder po) => new()
    {
        Id = po.Id,
        PoNumber = po.PoNumber,
        SupplierId = po.SupplierId,
        SupplierName = po.Supplier?.Name,
        Status = po.Status,
        OrderDate = po.OrderDate,
        ExpectedDate = po.ExpectedDate,
        Total = po.Total,
        Lines = po.Lines.Select(l => new PurchaseOrderLineDto
        {
            Id = l.Id,
            ProductId = l.ProductId,
            ProductSku = l.Product?.Sku,
            ProductUpc = l.Product?.Upc,
            ProductName = l.Product?.Name,
            QuantityOrdered = l.QuantityOrdered,
            QuantityReceived = l.QuantityReceived,
            UnitCost = l.UnitCost
        }).ToList()
    };

    public static SalesOrderDto ToDto(this SalesOrder so) => new()
    {
        Id = so.Id,
        OrderNumber = so.OrderNumber,
        CustomerId = so.CustomerId,
        CustomerName = so.Customer?.Name,
        DocumentType = so.DocumentType,
        Status = so.Status,
        OrderDate = so.OrderDate,
        Subtotal = so.Subtotal,
        DiscountAmount = so.DiscountAmount,
        TaxRate = so.TaxRate,
        TaxAmount = so.TaxAmount,
        Total = so.Total,
        AmountPaid = so.AmountPaid,
        MarginAmount = so.Lines.Sum(l => (l.UnitPrice * (1 - l.DiscountPercent / 100m) - l.UnitCostSnapshot) * l.Quantity),
        TrackingNumber = so.TrackingNumber,
        Carrier = so.Carrier,
        Notes = so.Notes,
        Lines = so.Lines.Select(l => new SalesOrderLineDto
        {
            Id = l.Id,
            ProductId = l.ProductId,
            ProductSku = l.Product?.Sku,
            ProductUpc = l.Product?.Upc,
            ProductName = l.Product?.Name,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList()
    };
}
