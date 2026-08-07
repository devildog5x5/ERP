using System.Linq;

namespace Ledgerly.Server.Data;

public static class InventoryService
{
    public static void ApplyDelta(
        ErpDbContext db,
        Product product,
        decimal delta,
        string reason,
        string? referenceType = null,
        int? referenceId = null,
        string? notes = null,
        int? locationId = null,
        string? reasonCode = null,
        string? lotNumber = null,
        string? serialNumber = null,
        int? userId = null)
    {
        var locId = locationId
                    ?? db.Settings.Select(s => s.DefaultLocationId).FirstOrDefault()
                    ?? db.Locations.OrderBy(l => l.Id).Select(l => (int?)l.Id).FirstOrDefault();

        if (locId.HasValue && product.Id > 0)
        {
            var pl = db.ProductLocations.FirstOrDefault(x => x.ProductId == product.Id && x.LocationId == locId.Value);
            if (pl is null)
            {
                pl = new ProductLocation { ProductId = product.Id, LocationId = locId.Value, Quantity = 0 };
                db.ProductLocations.Add(pl);
            }
            pl.Quantity += delta;
        }

        product.QuantityOnHand += delta;
        db.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            LocationId = locId,
            QuantityDelta = delta,
            QuantityAfter = product.QuantityOnHand,
            Reason = reason,
            ReasonCode = reasonCode,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            LotNumber = lotNumber,
            SerialNumber = serialNumber,
            Notes = notes,
            UserId = userId
        });
    }

    /// <summary>Call before applying the receive quantity delta.</summary>
    public static void ApplyAverageCostOnReceive(Product product, decimal qtyReceived, decimal unitCost)
    {
        if (qtyReceived <= 0) return;
        var existingQty = product.QuantityOnHand;
        var existingCost = product.AverageCost > 0 ? product.AverageCost : product.UnitCost;
        var newQty = existingQty + qtyReceived;
        product.AverageCost = newQty <= 0 ? unitCost : ((existingQty * existingCost) + (qtyReceived * unitCost)) / newQty;
        product.UnitCost = product.AverageCost;
    }
}
