using System;
using System.Collections.Generic;
using System.Linq;

namespace Ledgerly.Server.Data;

public static class ReminderScanner
{
    public static void Scan(ErpDbContext db)
    {
        var products = db.Products.Where(p => p.IsActive).ToList();
        var lowIds = new HashSet<int>();

        foreach (var p in products.Where(p => p.QuantityOnHand <= p.ReorderPoint))
        {
            lowIds.Add(p.Id);
            var qty = Math.Max(p.ReorderQuantity, p.ReorderPoint - p.QuantityOnHand);
            var severity = p.QuantityOnHand <= 0 ? "critical" : "warning";
            Upsert(db, "low_stock", severity, $"Low stock: {p.Name}",
                $"{p.Name} ({p.Sku}) is at {p.QuantityOnHand} {p.Unit}. Reorder point {p.ReorderPoint}. Suggested buy: {qty} {p.Unit}.",
                productId: p.Id, relatedType: "product", relatedId: p.Id);
            Upsert(db, "reorder_suggested", "info", $"Buy suggested: {p.Name}",
                $"Create a purchase order for {qty} {p.Unit} of {p.Name}. Estimated cost: {qty * p.UnitCost:C}.",
                productId: p.Id, relatedType: "product", relatedId: p.Id);
        }

        foreach (var r in db.Reminders
                     .Where(r => !r.IsResolved && (r.ReminderType == "low_stock" || r.ReminderType == "reorder_suggested"))
                     .ToList()
                     .Where(r => r.ProductId is int id && !lowIds.Contains(id)))
        {
            r.IsResolved = true;
            r.IsRead = true;
        }

        var today = DateTime.Today;
        var openPos = db.PurchaseOrders
            .Where(p => p.Status == "ordered" || p.Status == "partial" || p.Status == "draft")
            .ToList();
        var activePoIds = new HashSet<int>();
        foreach (var po in openPos.Where(p => p.ExpectedDate.HasValue))
        {
            activePoIds.Add(po.Id);
            if (po.ExpectedDate < today)
            {
                Upsert(db, "po_overdue", "critical", $"Overdue purchase order {po.PoNumber}",
                    $"{po.PoNumber} was expected on {po.ExpectedDate:yyyy-MM-dd} and is still {po.Status}.",
                    relatedType: "purchase_order", relatedId: po.Id);
            }
            else if (po.ExpectedDate <= today.AddDays(2))
            {
                Upsert(db, "po_expected", "warning", $"Incoming delivery {po.PoNumber}",
                    $"{po.PoNumber} is expected on {po.ExpectedDate:yyyy-MM-dd}. Prepare to receive stock.",
                    relatedType: "purchase_order", relatedId: po.Id);
            }
        }

        foreach (var r in db.Reminders
                     .Where(r => !r.IsResolved && (r.ReminderType == "po_overdue" || r.ReminderType == "po_expected"))
                     .ToList()
                     .Where(r => r.RelatedEntityId is int id && !activePoIds.Contains(id)))
        {
            r.IsResolved = true;
            r.IsRead = true;
        }

        db.SaveChanges();
    }

    private static void Upsert(
        ErpDbContext db,
        string type,
        string severity,
        string title,
        string message,
        int? productId = null,
        string? relatedType = null,
        int? relatedId = null)
    {
        var existing = db.Reminders.FirstOrDefault(r =>
            !r.IsResolved
            && r.ReminderType == type
            && r.ProductId == productId
            && r.RelatedEntityType == relatedType
            && r.RelatedEntityId == relatedId);

        if (existing is null)
        {
            db.Reminders.Add(new Reminder
            {
                ReminderType = type,
                Severity = severity,
                Title = title,
                Message = message,
                ProductId = productId,
                RelatedEntityType = relatedType,
                RelatedEntityId = relatedId,
                EmailSent = true,
            });
        }
        else
        {
            existing.Title = title;
            existing.Message = message;
            existing.Severity = severity;
        }
    }
}
