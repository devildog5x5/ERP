using System;
using System.Linq;
using System.Web.Http;
using Ledgerly.Server.Data;
using Ledgerly.Server.Services;
using Ledgerly.Shared;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Server.Controllers;

[RoutePrefix("api")]
public class ErpController : ApiController
{
    [HttpGet, Route("health")]
    public HealthDto Health() => new()
    {
        DatabaseProvider = Db.Provider.ToString(),
        Database = new ServerConfig { Provider = Db.Provider, ConnectionString = Db.ConnectionString }.Describe(),
        ConfigPath = Db.ConfigPath,
        CanScaleOut = Db.IsServerDatabase(Db.Provider)
    };

    [HttpGet, Route("settings")]
    public IHttpActionResult GetSettings()
    {
        using var db = Db.Create();
        return Ok(GetOrCreateSettings(db).ToDto());
    }

    [HttpPut, Route("settings"), RequirePermission("settings")]
    public IHttpActionResult UpdateSettings([FromBody] SettingsDto dto)
    {
        if (dto == null) return BadRequest("Body required");
        using var db = Db.Create();
        var s = GetOrCreateSettings(db);
        s.CompanyName = string.IsNullOrWhiteSpace(dto.CompanyName) ? s.CompanyName : dto.CompanyName.Trim();
        s.DefaultTaxRate = Math.Max(0, dto.DefaultTaxRate);
        s.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.Trim().ToUpperInvariant();
        s.ReceiptFooter = dto.ReceiptFooter;
        s.Address = dto.Address;
        s.Phone = dto.Phone;
        s.Email = dto.Email;
        s.SmtpHost = dto.SmtpHost;
        s.SmtpPort = dto.SmtpPort <= 0 ? 587 : dto.SmtpPort;
        s.SmtpUsername = dto.SmtpUsername;
        if (!string.IsNullOrWhiteSpace(dto.SmtpPassword) && dto.SmtpPassword != "********")
            s.SmtpPassword = dto.SmtpPassword;
        s.SmtpEnableSsl = dto.SmtpEnableSsl;
        s.SmtpFrom = dto.SmtpFrom;
        s.PoApprovalThreshold = dto.PoApprovalThreshold;
        s.RequireLogin = dto.RequireLogin;
        s.DefaultLocationId = dto.DefaultLocationId;
        db.SaveChanges();
        return Ok(s.ToDto());
    }

    [HttpGet, Route("dashboard"), RequirePermission("dashboard")]
    public DashboardDto Dashboard()
    {
        using var db = Db.Create();
        var products = db.Products.Where(p => p.IsActive).ToList();
        var low = products.Where(p => p.QuantityOnHand <= p.ReorderPoint).ToList();
        var openPo = db.PurchaseOrders.Count(p => p.Status == "ordered" || p.Status == "partial" || p.Status == "draft");
        var openSo = db.SalesOrders.Count(s => s.Status == "draft" || s.Status == "confirmed");
        var unread = db.Reminders.Count(r => !r.IsResolved && !r.IsRead);
        var reminders = db.Reminders.Where(r => !r.IsResolved).OrderByDescending(r => r.CreatedAt).Take(8).ToList();
        var pending = db.PurchaseOrders.Include(p => p.Supplier).Include(p => p.Lines).ThenInclude(l => l.Product)
            .Where(p => p.Status == "ordered" || p.Status == "partial" || p.Status == "draft")
            .OrderBy(p => p.ExpectedDate).Take(8).ToList();

        return new DashboardDto
        {
            ProductCount = products.Count,
            LowStockCount = low.Count,
            OpenPoCount = openPo,
            OpenSoCount = openSo,
            InventoryValue = products.Sum(p => p.QuantityOnHand * p.UnitCost),
            UnreadReminders = unread,
            LowStockProducts = low.Take(10).Select(p => p.ToDto()).ToList(),
            RecentReminders = reminders.Select(r => r.ToDto()).ToList(),
            PendingPurchaseOrders = pending.Select(p => p.ToDto()).ToList()
        };
    }

    [HttpGet, Route("products")]
    public IHttpActionResult Products(bool lowStock = false, string? q = null)
    {
        using var db = Db.Create();
        IQueryable<Product> query = db.Products.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Sku.ToLower().Contains(term) ||
                p.Name.ToLower().Contains(term) ||
                (p.Upc != null && p.Upc.ToLower().Contains(term)) ||
                (p.Category != null && p.Category.ToLower().Contains(term)));
        }
        var list = query.OrderBy(p => p.Name).ToList();
        if (lowStock) list = list.Where(p => p.QuantityOnHand <= p.ReorderPoint).ToList();
        return Ok(list.Select(p => p.ToDto()).ToList());
    }

    [HttpGet, Route("products/by-code/{code}")]
    public IHttpActionResult ProductByCode(string code)
    {
        using var db = Db.Create();
        var product = FindByCode(db, code);
        if (product is null) return NotFound();
        return Ok(product.ToDto());
    }

    [HttpPost, Route("products"), RequirePermission("inventory")]
    public IHttpActionResult CreateProduct([FromBody] ProductCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Sku) || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("SKU and name are required");
        using var db = Db.Create();
        if (db.Products.Any(p => p.Sku == dto.Sku && p.IsActive))
            return BadRequest("SKU already exists");
        var upc = NormalizeCode(dto.Upc);
        if (upc != null && db.Products.Any(p => p.Upc == upc && p.IsActive))
            return BadRequest("UPC already exists");
        var p = new Product
        {
            Sku = dto.Sku.Trim(), Upc = upc, Name = dto.Name.Trim(), Category = dto.Category,
            Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "ea" : dto.Unit,
            QuantityOnHand = dto.QuantityOnHand, ReorderPoint = dto.ReorderPoint,
            ReorderQuantity = dto.ReorderQuantity, UnitCost = dto.UnitCost, AverageCost = dto.UnitCost,
            SellPrice = dto.SellPrice, SupplierId = dto.SupplierId, TaxCodeId = dto.TaxCodeId,
            TrackLots = dto.TrackLots, TrackSerials = dto.TrackSerials, IsKit = dto.IsKit
        };
        db.Products.Add(p);
        db.SaveChanges();
        if (p.QuantityOnHand != 0)
        {
            db.StockMovements.Add(new StockMovement
            {
                ProductId = p.Id,
                QuantityDelta = p.QuantityOnHand,
                QuantityAfter = p.QuantityOnHand,
                Reason = "opening",
                Notes = "Opening balance"
            });
            db.SaveChanges();
        }
        ReminderScanner.Scan(db);
        return Ok(p.ToDto());
    }

    [HttpPut, Route("products/{id:int}"), RequirePermission("inventory")]
    public IHttpActionResult UpdateProduct(int id, [FromBody] ProductCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Sku) || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("SKU and name are required");
        using var db = Db.Create();
        var p = db.Products.Find(id);
        if (p is null || !p.IsActive) return NotFound();
        if (db.Products.Any(x => x.Id != id && x.Sku == dto.Sku && x.IsActive))
            return BadRequest("SKU already exists");
        var upc = NormalizeCode(dto.Upc);
        if (upc != null && db.Products.Any(x => x.Id != id && x.Upc == upc && x.IsActive))
            return BadRequest("UPC already exists");

        var qtyDelta = dto.QuantityOnHand - p.QuantityOnHand;
        p.Sku = dto.Sku.Trim();
        p.Upc = upc;
        p.Name = dto.Name.Trim();
        p.Category = dto.Category;
        p.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "ea" : dto.Unit;
        p.ReorderPoint = dto.ReorderPoint;
        p.ReorderQuantity = dto.ReorderQuantity;
        p.UnitCost = dto.UnitCost;
        if (p.AverageCost <= 0) p.AverageCost = dto.UnitCost;
        p.SellPrice = dto.SellPrice;
        p.SupplierId = dto.SupplierId;
        p.TaxCodeId = dto.TaxCodeId;
        p.TrackLots = dto.TrackLots;
        p.TrackSerials = dto.TrackSerials;
        p.IsKit = dto.IsKit;
        if (qtyDelta != 0)
            InventoryService.ApplyDelta(db, p, qtyDelta, "edit", notes: dto.Name);
        else
            p.QuantityOnHand = dto.QuantityOnHand;
        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(p.ToDto());
    }

    [HttpDelete, Route("products/{id:int}"), RequirePermission("inventory")]
    public IHttpActionResult DeleteProduct(int id)
    {
        using var db = Db.Create();
        var p = db.Products.Find(id);
        if (p is null || !p.IsActive) return NotFound();
        p.IsActive = false;
        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(new { deleted = true });
    }

    [HttpPost, Route("products/{id:int}/adjust"), RequirePermission("inventory")]
    public IHttpActionResult Adjust(int id, [FromBody] StockAdjustDto dto)
    {
        if (dto == null) return BadRequest("Body required");
        using var db = Db.Create();
        var p = db.Products.Find(id);
        if (p is null || !p.IsActive) return NotFound();
        InventoryService.ApplyDelta(db, p, dto.QuantityDelta, "adjust", notes: dto.Notes);
        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(p.ToDto());
    }

    [HttpGet, Route("stock-movements")]
    public IHttpActionResult StockMovements(int? productId = null, int take = 100)
    {
        using var db = Db.Create();
        var q = db.StockMovements.Include(m => m.Product).AsQueryable();
        if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
        var list = q.OrderByDescending(m => m.CreatedAt).Take(Math.Min(take, 500)).ToList();
        return Ok(list.Select(m => m.ToDto()).ToList());
    }

    [HttpPost, Route("scan/adjust")]
    public IHttpActionResult ScanAdjust([FromBody] ScanAdjustDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Code required");
        using var db = Db.Create();
        var p = FindByCode(db, dto.Code);
        if (p is null) return NotFound();
        var delta = dto.QuantityDelta == 0 ? 1 : dto.QuantityDelta;
        InventoryService.ApplyDelta(db, p, delta, "scan-adjust", notes: dto.Notes ?? $"Scanned {NormalizeCode(dto.Code)}");
        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(p.ToDto());
    }

    [HttpPost, Route("scan/receive")]
    public IHttpActionResult ScanReceive([FromBody] ScanReceiveDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Code required");
        using var db = Db.Create();
        var product = FindByCode(db, dto.Code);
        if (product is null) return NotFound();
        var po = LoadPo(db, dto.PurchaseOrderId);
        if (po is null) return BadRequest("Purchase order not found");
        if (po.Status == "received" || po.Status == "cancelled") return BadRequest("Cannot receive this PO");
        if (!po.IsApproved || po.Status == "pending_approval")
            return BadRequest("PO must be approved before receiving");

        var line = po.Lines.FirstOrDefault(l => l.ProductId == product.Id);
        if (line is null) return BadRequest($"UPC/SKU not on PO {po.PoNumber}");
        var qty = dto.Quantity <= 0 ? 1 : dto.Quantity;
        var remaining = line.QuantityOrdered - line.QuantityReceived;
        if (qty > remaining) return BadRequest($"Only {remaining} remaining on this line");

        line.QuantityReceived += qty;
        InventoryService.ApplyDelta(db, product, qty, "receive", "purchase-order", po.Id, po.PoNumber);
        if (line.UnitCost > 0) product.UnitCost = line.UnitCost;

        if (po.Lines.All(l => l.QuantityReceived >= l.QuantityOrdered))
        {
            po.Status = "received";
            po.ReceivedDate = DateTime.Today;
        }
        else if (po.Lines.Any(l => l.QuantityReceived > 0))
            po.Status = "partial";

        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(LoadPo(db, po.Id)!.ToDto());
    }

    [HttpGet, Route("suppliers")]
    public IHttpActionResult Suppliers()
    {
        using var db = Db.Create();
        return Ok(db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList().Select(s => s.ToDto()).ToList());
    }

    [HttpPost, Route("suppliers")]
    public IHttpActionResult CreateSupplier([FromBody] PartnerCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        using var db = Db.Create();
        var s = new Supplier { Name = dto.Name.Trim(), Email = dto.Email, Phone = dto.Phone, Address = dto.Address };
        db.Suppliers.Add(s);
        db.SaveChanges();
        return Ok(s.ToDto());
    }

    [HttpPut, Route("suppliers/{id:int}")]
    public IHttpActionResult UpdateSupplier(int id, [FromBody] PartnerCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        using var db = Db.Create();
        var s = db.Suppliers.Find(id);
        if (s is null || !s.IsActive) return NotFound();
        s.Name = dto.Name.Trim();
        s.Email = dto.Email;
        s.Phone = dto.Phone;
        s.Address = dto.Address;
        db.SaveChanges();
        return Ok(s.ToDto());
    }

    [HttpDelete, Route("suppliers/{id:int}")]
    public IHttpActionResult DeleteSupplier(int id)
    {
        using var db = Db.Create();
        var s = db.Suppliers.Find(id);
        if (s is null || !s.IsActive) return NotFound();
        if (db.PurchaseOrders.Any(p => p.SupplierId == id && p.Status != "cancelled" && p.Status != "received"))
            return BadRequest("Supplier has open purchase orders");
        s.IsActive = false;
        foreach (var p in db.Products.Where(p => p.SupplierId == id))
            p.SupplierId = null;
        db.SaveChanges();
        return Ok(new { deleted = true });
    }

    [HttpGet, Route("customers")]
    public IHttpActionResult Customers()
    {
        using var db = Db.Create();
        return Ok(db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToList().Select(c => c.ToDto()).ToList());
    }

    [HttpPost, Route("customers")]
    public IHttpActionResult CreateCustomer([FromBody] PartnerCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        using var db = Db.Create();
        var c = new Customer { Name = dto.Name.Trim(), Email = dto.Email, Phone = dto.Phone, Address = dto.Address };
        db.Customers.Add(c);
        db.SaveChanges();
        return Ok(c.ToDto());
    }

    [HttpPut, Route("customers/{id:int}")]
    public IHttpActionResult UpdateCustomer(int id, [FromBody] PartnerCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        using var db = Db.Create();
        var c = db.Customers.Find(id);
        if (c is null || !c.IsActive) return NotFound();
        c.Name = dto.Name.Trim();
        c.Email = dto.Email;
        c.Phone = dto.Phone;
        c.Address = dto.Address;
        db.SaveChanges();
        return Ok(c.ToDto());
    }

    [HttpDelete, Route("customers/{id:int}")]
    public IHttpActionResult DeleteCustomer(int id)
    {
        using var db = Db.Create();
        var c = db.Customers.Find(id);
        if (c is null || !c.IsActive) return NotFound();
        if (db.SalesOrders.Any(s => s.CustomerId == id && (s.Status == "draft" || s.Status == "confirmed")))
            return BadRequest("Customer has open sales orders");
        c.IsActive = false;
        db.SaveChanges();
        return Ok(new { deleted = true });
    }

    [HttpGet, Route("purchase-orders")]
    public IHttpActionResult PurchaseOrders()
    {
        using var db = Db.Create();
        var list = db.PurchaseOrders.Include(p => p.Supplier).Include(p => p.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(p => p.Id).ToList();
        return Ok(list.Select(p => p.ToDto()).ToList());
    }

    [HttpPost, Route("purchase-orders"), RequirePermission("purchasing")]
    public IHttpActionResult CreatePo([FromBody] PurchaseOrderCreateDto dto)
    {
        if (dto == null || dto.Lines == null || dto.Lines.Count == 0) return BadRequest("At least one line is required");
        using var db = Db.Create();
        if (!db.Suppliers.Any(s => s.Id == dto.SupplierId && s.IsActive)) return BadRequest("Supplier not found");
        var settings = GetOrCreateSettings(db);
        var po = new PurchaseOrder
        {
            PoNumber = DocumentNumbers.Next(db, "PO", "PO-"),
            SupplierId = dto.SupplierId,
            Status = "ordered",
            ExpectedDate = dto.ExpectedDate,
            Notes = dto.Notes,
            LocationId = settings.DefaultLocationId,
            IsApproved = true
        };
        decimal total = 0;
        foreach (var line in dto.Lines)
        {
            var product = db.Products.Find(line.ProductId);
            if (product is null || !product.IsActive) return BadRequest($"Product {line.ProductId} not found");
            var cost = line.UnitCost ?? product.UnitCost;
            po.Lines.Add(new PurchaseOrderLine
            {
                ProductId = product.Id,
                QuantityOrdered = line.QuantityOrdered,
                UnitCost = cost
            });
            total += line.QuantityOrdered * cost;
        }
        po.Total = total;
        if (total >= settings.PoApprovalThreshold)
        {
            po.ApprovalRequired = true;
            po.IsApproved = false;
            po.Status = "pending_approval";
        }
        db.PurchaseOrders.Add(po);
        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(LoadPo(db, po.Id)!.ToDto());
    }

    [HttpPut, Route("purchase-orders/{id:int}")]
    public IHttpActionResult UpdatePo(int id, [FromBody] PurchaseOrderUpdateDto dto)
    {
        if (dto == null) return BadRequest("Body required");
        using var db = Db.Create();
        var po = LoadPo(db, id);
        if (po is null) return NotFound();
        if (po.Status == "received" || po.Status == "cancelled")
            return BadRequest("Cannot modify a received or cancelled PO");
        if (!db.Suppliers.Any(s => s.Id == dto.SupplierId && s.IsActive)) return BadRequest("Supplier not found");

        po.SupplierId = dto.SupplierId;
        po.ExpectedDate = dto.ExpectedDate;
        po.Notes = dto.Notes;
        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var status = dto.Status.Trim().ToLowerInvariant();
            if (status is "received" or "partial")
                return BadRequest("Use the receive endpoint to receive stock");
            if (po.Status == "pending_approval" && status is not ("pending_approval" or "cancelled" or "draft"))
                return BadRequest("PO requires approval before changing status");
            if (status == "ordered" && !po.IsApproved)
                return BadRequest("PO must be approved before marking ordered");
            po.Status = status;
        }

        if (dto.Lines != null)
        {
            if (po.Lines.Any(l => l.QuantityReceived > 0))
                return BadRequest("Cannot change lines after receiving stock");
            if (dto.Lines.Count == 0) return BadRequest("At least one line is required");
            db.PurchaseOrderLines.RemoveRange(po.Lines);
            po.Lines.Clear();
            decimal total = 0;
            foreach (var line in dto.Lines)
            {
                var product = db.Products.Find(line.ProductId);
                if (product is null || !product.IsActive) return BadRequest($"Product {line.ProductId} not found");
                var cost = line.UnitCost ?? product.UnitCost;
                po.Lines.Add(new PurchaseOrderLine
                {
                    ProductId = product.Id,
                    QuantityOrdered = line.QuantityOrdered,
                    UnitCost = cost
                });
                total += line.QuantityOrdered * cost;
            }
            po.Total = total;
        }

        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(LoadPo(db, id)!.ToDto());
    }

    [HttpDelete, Route("purchase-orders/{id:int}")]
    public IHttpActionResult DeletePo(int id)
    {
        using var db = Db.Create();
        var po = LoadPo(db, id);
        if (po is null) return NotFound();
        if (po.Lines.Any(l => l.QuantityReceived > 0))
            return BadRequest("Cannot delete a PO that has received stock; cancel it instead");
        db.PurchaseOrderLines.RemoveRange(po.Lines);
        db.PurchaseOrders.Remove(po);
        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(new { deleted = true });
    }

    [HttpPost, Route("purchase-orders/{id:int}/receive")]
    public IHttpActionResult ReceivePo(int id, [FromBody] ReceivePurchaseOrderDto dto)
    {
        if (dto?.Lines == null || dto.Lines.Count == 0) return BadRequest("Receive lines required");
        using var db = Db.Create();
        var po = LoadPo(db, id);
        if (po is null) return NotFound();
        if (po.Status == "received" || po.Status == "cancelled") return BadRequest("Cannot receive this PO");
        if (!po.IsApproved || po.Status == "pending_approval")
            return BadRequest("PO must be approved before receiving");

        foreach (var item in dto.Lines)
        {
            var line = po.Lines.FirstOrDefault(l => l.Id == item.LineId);
            if (line is null) return BadRequest($"Line {item.LineId} not found");
            var remaining = line.QuantityOrdered - line.QuantityReceived;
            if (item.QuantityReceived > remaining) return BadRequest("Cannot receive more than remaining qty");
            line.QuantityReceived += item.QuantityReceived;
            if (line.UnitCost > 0)
                InventoryService.ApplyAverageCostOnReceive(line.Product, item.QuantityReceived, line.UnitCost);
            InventoryService.ApplyDelta(db, line.Product, item.QuantityReceived, "receive", "purchase-order", po.Id, po.PoNumber, locationId: po.LocationId);
        }

        if (po.Lines.All(l => l.QuantityReceived >= l.QuantityOrdered))
        {
            po.Status = "received";
            po.ReceivedDate = DateTime.Today;
        }
        else if (po.Lines.Any(l => l.QuantityReceived > 0))
            po.Status = "partial";

        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(LoadPo(db, id)!.ToDto());
    }

    [HttpGet, Route("sales-orders")]
    public IHttpActionResult SalesOrders()
    {
        using var db = Db.Create();
        var list = db.SalesOrders.Include(s => s.Customer).Include(s => s.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(s => s.Id).ToList();
        return Ok(list.Select(s => s.ToDto()).ToList());
    }

    [HttpPost, Route("sales-orders"), RequirePermission("sales")]
    public IHttpActionResult CreateSo([FromBody] SalesOrderCreateDto dto)
    {
        if (dto == null || dto.Lines == null || dto.Lines.Count == 0) return BadRequest("At least one line is required");
        using var db = Db.Create();
        var customer = db.Customers.Include(c => c.PriceList).ThenInclude(p => p!.Items).FirstOrDefault(c => c.Id == dto.CustomerId && c.IsActive);
        if (customer is null) return BadRequest("Customer not found");
        var settings = GetOrCreateSettings(db);
        var docType = string.IsNullOrWhiteSpace(dto.DocumentType) ? "order" : dto.DocumentType.Trim().ToLowerInvariant();
        var isQuote = docType == "quote";
        var taxRate = customer.TaxExempt ? 0 : (dto.TaxRate ?? settings.DefaultTaxRate);
        var prefix = docType switch { "quote" => "QT-", "invoice" => "INV-", _ => "SO-" };
        var seq = docType switch { "quote" => "QT", "invoice" => "INV", _ => "SO" };
        var so = new SalesOrder
        {
            OrderNumber = DocumentNumbers.Next(db, seq, prefix),
            CustomerId = dto.CustomerId,
            DocumentType = docType,
            Status = isQuote ? "quote" : (docType == "invoice" ? "invoiced" : "fulfilled"),
            Notes = dto.Notes,
            TaxRate = taxRate,
            DiscountAmount = dto.DiscountAmount,
            LocationId = dto.LocationId ?? settings.DefaultLocationId,
            DueDate = DateTime.Today.AddDays(customer.PaymentTermsDays ?? 30)
        };
        decimal subtotal = 0;
        decimal cogs = 0;
        foreach (var line in dto.Lines)
        {
            var product = db.Products.Include(p => p.TaxCode).FirstOrDefault(p => p.Id == line.ProductId);
            if (product is null || !product.IsActive) return BadRequest($"Product {line.ProductId} not found");
            if (!isQuote && product.QuantityOnHand < line.Quantity)
                return BadRequest(
                    $"Insufficient stock for {product.Sku} ({product.Name}). " +
                    $"Requested {line.Quantity}, available {product.QuantityOnHand}. " +
                    "Receive a PO or adjust inventory first, or save as a Quote.");
            var price = line.UnitPrice
                        ?? customer.PriceList?.Items.FirstOrDefault(i => i.ProductId == product.Id)?.UnitPrice
                        ?? product.SellPrice;
            var cost = product.AverageCost > 0 ? product.AverageCost : product.UnitCost;
            so.Lines.Add(new SalesOrderLine
            {
                ProductId = product.Id, Quantity = line.Quantity, UnitPrice = price,
                QuantityShipped = isQuote ? 0 : line.Quantity, UnitCostSnapshot = cost
            });
            if (!isQuote)
            {
                InventoryService.ApplyDelta(db, product, -line.Quantity, "sale", "sales-order", null, so.OrderNumber, locationId: so.LocationId);
                cogs += cost * line.Quantity;
            }
            subtotal += line.Quantity * price;
            if (!customer.TaxExempt && product.TaxCode != null)
                taxRate = product.TaxCode.Rate; // last non-exempt product tax wins for simplicity; settings used if none
        }
        if (!customer.TaxExempt && dto.TaxRate == null)
        {
            var firstTax = dto.Lines.Select(l => db.Products.Find(l.ProductId)?.TaxCodeId).FirstOrDefault(id => id != null);
            if (firstTax != null)
            {
                var tc = db.TaxCodes.Find(firstTax.Value);
                if (tc != null) taxRate = tc.Rate;
            }
        }
        so.TaxRate = taxRate;
        so.Subtotal = subtotal;
        so.TaxAmount = Math.Round(Math.Max(0, subtotal - dto.DiscountAmount) * (taxRate / 100m), 2);
        so.Total = Math.Max(0, subtotal - dto.DiscountAmount) + so.TaxAmount;
        db.SalesOrders.Add(so);
        db.SaveChanges();
        foreach (var m in db.StockMovements.Where(x =>
                     x.ReferenceType == "sales-order" && x.Notes == so.OrderNumber && x.ReferenceId == null))
            m.ReferenceId = so.Id;
        if (!isQuote)
        {
            try
            {
                GlPostingService.PostSale(db, so);
                GlPostingService.PostCogs(db, so, cogs);
            }
            catch { /* ignore GL failures so sale still posts */ }
            foreach (var wh in db.Webhooks.Where(w => w.IsActive && w.EventName == "sales.created"))
            {
                db.IntegrationLogs.Add(new IntegrationLog
                {
                    SystemName = "webhook", Action = wh.EventName, Status = "queued-stub",
                    Details = $"{wh.TargetUrl} :: {so.OrderNumber}"
                });
            }
        }
        db.SaveChanges();
        ReminderScanner.Scan(db);
        var loaded = db.SalesOrders.Include(s => s.Customer).Include(s => s.Lines).ThenInclude(l => l.Product)
            .First(s => s.Id == so.Id);
        return Ok(loaded.ToDto());
    }

    [HttpPut, Route("sales-orders/{id:int}")]
    public IHttpActionResult UpdateSo(int id, [FromBody] SalesOrderUpdateDto dto)
    {
        if (dto == null) return BadRequest("Body required");
        using var db = Db.Create();
        var so = db.SalesOrders.Include(s => s.Customer).Include(s => s.Lines).ThenInclude(l => l.Product)
            .FirstOrDefault(s => s.Id == id);
        if (so is null) return NotFound();
        if (so.Status == "cancelled") return BadRequest("Cannot modify a cancelled sales order");
        if (!db.Customers.Any(c => c.Id == dto.CustomerId && c.IsActive)) return BadRequest("Customer not found");

        so.CustomerId = dto.CustomerId;
        so.Notes = dto.Notes;
        if (dto.TaxRate.HasValue) so.TaxRate = dto.TaxRate.Value;

        var wasInventory = AffectsInventory(so);
        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var newStatus = dto.Status.Trim().ToLowerInvariant();
            if (newStatus == "cancelled" && so.Status != "cancelled" && wasInventory && dto.Lines == null)
            {
                foreach (var old in so.Lines)
                    InventoryService.ApplyDelta(db, old.Product, old.Quantity, "sale-void", "sales-order", so.Id, so.OrderNumber);
            }
            so.Status = newStatus;
        }

        var affectsInventory = AffectsInventory(so);
        if (dto.Lines != null)
        {
            if (dto.Lines.Count == 0) return BadRequest("At least one line is required");
            if (wasInventory)
            {
                foreach (var old in so.Lines)
                    InventoryService.ApplyDelta(db, old.Product, old.Quantity, "sale-void", "sales-order", so.Id, so.OrderNumber);
            }
            db.SalesOrderLines.RemoveRange(so.Lines);
            so.Lines.Clear();
            decimal subtotal = 0;
            foreach (var line in dto.Lines)
            {
                var product = db.Products.Find(line.ProductId);
                if (product is null || !product.IsActive) return BadRequest($"Product {line.ProductId} not found");
                if (affectsInventory && product.QuantityOnHand < line.Quantity)
                    return BadRequest(
                        $"Insufficient stock for {product.Sku} ({product.Name}). " +
                        $"Requested {line.Quantity}, available {product.QuantityOnHand}.");
                var price = line.UnitPrice ?? product.SellPrice;
                so.Lines.Add(new SalesOrderLine { ProductId = product.Id, Quantity = line.Quantity, UnitPrice = price });
                if (affectsInventory)
                    InventoryService.ApplyDelta(db, product, -line.Quantity, "sale", "sales-order", so.Id, so.OrderNumber);
                subtotal += line.Quantity * price;
            }
            so.Subtotal = subtotal;
            so.TaxAmount = Math.Round(subtotal * (so.TaxRate / 100m), 2);
            so.Total = so.Subtotal + so.TaxAmount;
            if (so.Status != "cancelled" && so.DocumentType != "quote") so.Status = "fulfilled";
        }
        else
        {
            so.TaxAmount = Math.Round(so.Subtotal * (so.TaxRate / 100m), 2);
            so.Total = so.Subtotal + so.TaxAmount;
        }

        db.SaveChanges();
        ReminderScanner.Scan(db);
        var loaded = db.SalesOrders.Include(s => s.Customer).Include(s => s.Lines).ThenInclude(l => l.Product)
            .First(s => s.Id == id);
        return Ok(loaded.ToDto());
    }

    [HttpDelete, Route("sales-orders/{id:int}")]
    public IHttpActionResult DeleteSo(int id)
    {
        using var db = Db.Create();
        var so = db.SalesOrders.Include(s => s.Lines).ThenInclude(l => l.Product)
            .FirstOrDefault(s => s.Id == id);
        if (so is null) return NotFound();
        if (AffectsInventory(so))
        {
            foreach (var line in so.Lines)
                InventoryService.ApplyDelta(db, line.Product, line.Quantity, "sale-void", "sales-order", so.Id, so.OrderNumber);
        }
        db.SalesOrderLines.RemoveRange(so.Lines);
        db.SalesOrders.Remove(so);
        db.SaveChanges();
        ReminderScanner.Scan(db);
        return Ok(new { deleted = true });
    }

    private static bool AffectsInventory(SalesOrder so) =>
        !string.Equals(so.DocumentType, "quote", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(so.Status, "cancelled", StringComparison.OrdinalIgnoreCase);

    [HttpGet, Route("reminders")]
    public IHttpActionResult Reminders(bool unresolvedOnly = true)
    {
        using var db = Db.Create();
        var q = db.Reminders.AsQueryable();
        if (unresolvedOnly) q = q.Where(r => !r.IsResolved);
        return Ok(q.OrderByDescending(r => r.CreatedAt).ToList().Select(r => r.ToDto()).ToList());
    }

    [HttpPost, Route("reminders")]
    public IHttpActionResult CreateReminder([FromBody] ReminderCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required");
        using var db = Db.Create();
        var r = new Reminder
        {
            ReminderType = string.IsNullOrWhiteSpace(dto.ReminderType) ? "manual" : dto.ReminderType.Trim(),
            Severity = string.IsNullOrWhiteSpace(dto.Severity) ? "info" : dto.Severity.Trim(),
            Title = dto.Title.Trim(),
            Message = dto.Message ?? "",
            CreatedAt = DateTime.UtcNow
        };
        db.Reminders.Add(r);
        db.SaveChanges();
        return Ok(r.ToDto());
    }

    [HttpPut, Route("reminders/{id:int}")]
    public IHttpActionResult UpdateReminder(int id, [FromBody] ReminderCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required");
        using var db = Db.Create();
        var r = db.Reminders.Find(id);
        if (r is null) return NotFound();
        r.ReminderType = string.IsNullOrWhiteSpace(dto.ReminderType) ? r.ReminderType : dto.ReminderType.Trim();
        r.Severity = string.IsNullOrWhiteSpace(dto.Severity) ? r.Severity : dto.Severity.Trim();
        r.Title = dto.Title.Trim();
        r.Message = dto.Message ?? "";
        db.SaveChanges();
        return Ok(r.ToDto());
    }

    [HttpDelete, Route("reminders/{id:int}")]
    public IHttpActionResult DeleteReminder(int id)
    {
        using var db = Db.Create();
        var r = db.Reminders.Find(id);
        if (r is null) return NotFound();
        db.Reminders.Remove(r);
        db.SaveChanges();
        return Ok(new { deleted = true });
    }

    [HttpPost, Route("reminders/run"), RequirePermission("reminders")]
    public IHttpActionResult RunReminders()
    {
        using var db = Db.Create();
        ReminderScanner.Scan(db);
        var open = db.Reminders.Count(r => !r.IsResolved);
        return Ok(new { open_reminders = open, emails_sent = 0 });
    }

    [HttpPost, Route("reminders/{id:int}/resolve")]
    public IHttpActionResult Resolve(int id)
    {
        using var db = Db.Create();
        var r = db.Reminders.Find(id);
        if (r is null) return NotFound();
        r.IsResolved = true;
        r.IsRead = true;
        db.SaveChanges();
        return Ok(r.ToDto());
    }

    private static CompanySettings GetOrCreateSettings(ErpDbContext db)
    {
        var s = db.Settings.FirstOrDefault();
        if (s != null) return s;
        s = new CompanySettings { Id = 1, CompanyName = "Coalesce.ERP.CRM", Currency = "USD", ReceiptFooter = "Thank you for your business." };
        db.Settings.Add(s);
        db.SaveChanges();
        return s;
    }

    private static Product? FindByCode(ErpDbContext db, string code)
    {
        var normalized = NormalizeCode(code);
        if (normalized is null) return null;
        return db.Products.FirstOrDefault(p => p.IsActive &&
            (p.Upc == normalized || p.Sku.ToLower() == normalized.ToLower()));
    }

    private static string? NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return new string(code.Trim().Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    private static PurchaseOrder? LoadPo(ErpDbContext db, int id) =>
        db.PurchaseOrders.Include(p => p.Supplier).Include(p => p.Lines).ThenInclude(l => l.Product)
            .FirstOrDefault(p => p.Id == id);
}
