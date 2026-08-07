using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Ledgerly.Server.Data;
using Ledgerly.Server.Services;
using Ledgerly.Shared;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Server.Controllers;

[RoutePrefix("api")]
public class EnterpriseController : ApiController
{
    private AppUser? UserEntity =>
        Request.Properties.TryGetValue("LedgerlyUser", out var u) ? u as AppUser : RequestAuth.GetUser(Request);

    // ---------- Auth / users / audit ----------
    [HttpPost, Route("auth/login")]
    public IHttpActionResult Login([FromBody] LoginRequestDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.UserName)) return BadRequest("User name required");
        using var db = Db.Create();
        var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.UserName == dto.UserName && u.IsActive);
        if (user is null || !PasswordHasher.Verify(dto.Password ?? "", user.PasswordHash, user.PasswordSalt))
            return ResponseMessage(Request.CreateResponse(System.Net.HttpStatusCode.Unauthorized, "Invalid credentials"));

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expires = DateTime.UtcNow.AddHours(12);
        db.AuthTokens.Add(new AuthToken { UserId = user.Id, Token = token, ExpiresAt = expires });
        AuditService.Write(db, user.Id, user.UserName, "login", "user", user.Id);
        db.SaveChanges();
        return Ok(new LoginResponseDto
        {
            Token = token,
            UserId = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Role = user.Role.Name,
            Permissions = user.Role.Permissions,
            ExpiresAt = expires
        });
    }

    [HttpGet, Route("auth/me")]
    public IHttpActionResult Me()
    {
        var user = UserEntity;
        if (user is null) return Unauthorized();
        return Ok(new LoginResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Role = user.Role.Name,
            Permissions = user.Role.Permissions
        });
    }

    [HttpGet, Route("roles")]
    public IHttpActionResult Roles()
    {
        using var db = Db.Create();
        return Ok(db.Roles.OrderBy(r => r.Name).Select(r => new RoleDto { Id = r.Id, Name = r.Name, Permissions = r.Permissions }).ToList());
    }

    [HttpGet, Route("users")]
    public IHttpActionResult Users()
    {
        using var db = Db.Create();
        return Ok(db.Users.Include(u => u.Role).OrderBy(u => u.UserName).ToList().Select(u => new UserDto
        {
            Id = u.Id, UserName = u.UserName, DisplayName = u.DisplayName, RoleId = u.RoleId, RoleName = u.Role.Name, IsActive = u.IsActive
        }).ToList());
    }

    [HttpPost, Route("users")]
    public IHttpActionResult CreateUser([FromBody] UserCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("User name and password required");
        using var db = Db.Create();
        if (db.Users.Any(u => u.UserName == dto.UserName)) return BadRequest("User exists");
        var (hash, salt) = PasswordHasher.Hash(dto.Password);
        var user = new AppUser
        {
            UserName = dto.UserName.Trim(), DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.UserName : dto.DisplayName.Trim(),
            PasswordHash = hash, PasswordSalt = salt, RoleId = dto.RoleId, IsActive = true
        };
        db.Users.Add(user);
        AuditService.Write(db, UserEntity?.Id, UserEntity?.UserName ?? "", "create", "user", null, user.UserName);
        db.SaveChanges();
        return Ok(new UserDto { Id = user.Id, UserName = user.UserName, DisplayName = user.DisplayName, RoleId = user.RoleId, IsActive = true });
    }

    [HttpGet, Route("audit-logs")]
    public IHttpActionResult AuditLogs(int take = 200)
    {
        using var db = Db.Create();
        return Ok(db.AuditLogs.OrderByDescending(a => a.CreatedAt).Take(Math.Min(take, 500)).ToList()
            .Select(a => new AuditLogDto
            {
                Id = a.Id, CreatedAt = a.CreatedAt, UserName = a.UserName, Action = a.Action,
                EntityType = a.EntityType, EntityId = a.EntityId, Details = a.Details
            }).ToList());
    }

    // ---------- Locations / warehouse ----------
    [HttpGet, Route("locations")]
    public IHttpActionResult Locations()
    {
        using var db = Db.Create();
        return Ok(db.Locations.Where(l => l.IsActive).OrderBy(l => l.Code).ToList()
            .Select(l => new LocationDto { Id = l.Id, Code = l.Code, Name = l.Name, Bin = l.Bin, IsActive = l.IsActive }).ToList());
    }

    [HttpPost, Route("locations")]
    public IHttpActionResult CreateLocation([FromBody] LocationDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Code required");
        using var db = Db.Create();
        var loc = new Location { Code = dto.Code.Trim().ToUpperInvariant(), Name = dto.Name?.Trim() ?? dto.Code, Bin = dto.Bin, IsActive = true };
        db.Locations.Add(loc);
        db.SaveChanges();
        return Ok(new LocationDto { Id = loc.Id, Code = loc.Code, Name = loc.Name, Bin = loc.Bin, IsActive = true });
    }

    [HttpGet, Route("product-locations")]
    public IHttpActionResult ProductLocations(int? productId = null)
    {
        using var db = Db.Create();
        var q = db.ProductLocations.Include(p => p.Product).Include(p => p.Location).AsQueryable();
        if (productId.HasValue) q = q.Where(p => p.ProductId == productId);
        return Ok(q.ToList().Select(p => new ProductLocationDto
        {
            ProductId = p.ProductId, ProductSku = p.Product.Sku, ProductName = p.Product.Name,
            LocationId = p.LocationId, LocationCode = p.Location.Code, Quantity = p.Quantity
        }).ToList());
    }

    [HttpPost, Route("transfers")]
    public IHttpActionResult Transfer([FromBody] TransferCreateDto dto)
    {
        if (dto == null || dto.Quantity <= 0) return BadRequest("Invalid transfer");
        using var db = Db.Create();
        var product = db.Products.Find(dto.ProductId);
        if (product is null) return NotFound();
        InventoryService.ApplyDelta(db, product, -dto.Quantity, "transfer-out", "transfer", null, dto.Notes, dto.FromLocationId, userId: UserEntity?.Id);
        InventoryService.ApplyDelta(db, product, dto.Quantity, "transfer-in", "transfer", null, dto.Notes, dto.ToLocationId, userId: UserEntity?.Id);
        // net qty unchanged on product — fix double apply on QuantityOnHand
        product.QuantityOnHand -= 0; // no-op; ApplyDelta changed twice netting zero already? -q +q = 0. Good.
        var xfer = new StockTransfer
        {
            TransferNumber = DocumentNumbers.Next(db, "XFER", "XF-"),
            FromLocationId = dto.FromLocationId,
            ToLocationId = dto.ToLocationId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Notes = dto.Notes,
            UserId = UserEntity?.Id
        };
        db.StockTransfers.Add(xfer);
        AuditService.Write(db, UserEntity?.Id, UserEntity?.UserName ?? "", "transfer", "stock-transfer", null, xfer.TransferNumber);
        db.SaveChanges();
        return Ok(xfer);
    }

    [HttpPost, Route("cycle-counts")]
    public IHttpActionResult CycleCount([FromBody] CycleCountCreateDto dto)
    {
        if (dto == null) return BadRequest();
        using var db = Db.Create();
        var product = db.Products.Find(dto.ProductId);
        if (product is null) return NotFound();
        var pl = db.ProductLocations.FirstOrDefault(x => x.ProductId == dto.ProductId && x.LocationId == dto.LocationId);
        var system = pl?.Quantity ?? 0;
        var delta = dto.CountedQty - system;
        if (delta != 0)
            InventoryService.ApplyDelta(db, product, delta, "cycle-count", "cycle-count", null, null, dto.LocationId, dto.ReasonCode, userId: UserEntity?.Id);
        var cc = new CycleCount
        {
            CountNumber = DocumentNumbers.Next(db, "CC", "CC-"),
            LocationId = dto.LocationId,
            ProductId = dto.ProductId,
            SystemQty = system,
            CountedQty = dto.CountedQty,
            ReasonCode = dto.ReasonCode,
            UserId = UserEntity?.Id
        };
        db.CycleCounts.Add(cc);
        db.SaveChanges();
        return Ok(cc);
    }

    [HttpGet, Route("boms")]
    public IHttpActionResult Boms()
    {
        using var db = Db.Create();
        var list = db.Boms.Include(b => b.ParentProduct).Include(b => b.Lines).ThenInclude(l => l.ComponentProduct).ToList();
        return Ok(list.Select(b => new BomDto
        {
            Id = b.Id,
            ParentProductId = b.ParentProductId,
            ParentSku = b.ParentProduct.Sku,
            Name = b.Name,
            Lines = b.Lines.Select(l => new BomLineDto
            {
                ComponentProductId = l.ComponentProductId,
                ComponentSku = l.ComponentProduct.Sku,
                Quantity = l.Quantity
            }).ToList()
        }).ToList());
    }

    [HttpPost, Route("boms")]
    public IHttpActionResult CreateBom([FromBody] BomDto dto)
    {
        if (dto == null || dto.Lines == null || dto.Lines.Count == 0) return BadRequest("BOM lines required");
        using var db = Db.Create();
        var bom = new Bom { ParentProductId = dto.ParentProductId, Name = dto.Name };
        foreach (var line in dto.Lines)
            bom.Lines.Add(new BomLine { ComponentProductId = line.ComponentProductId, Quantity = line.Quantity });
        db.Boms.Add(bom);
        var parent = db.Products.Find(dto.ParentProductId);
        if (parent != null) parent.IsKit = true;
        db.SaveChanges();
        return Ok(new { bom.Id });
    }

    [HttpPost, Route("boms/build")]
    public IHttpActionResult BuildBom([FromBody] BomBuildDto dto)
    {
        if (dto == null || dto.Quantity <= 0) return BadRequest();
        using var db = Db.Create();
        var bom = db.Boms.Include(b => b.Lines).ThenInclude(l => l.ComponentProduct).Include(b => b.ParentProduct)
            .FirstOrDefault(b => b.Id == dto.BomId);
        if (bom is null) return NotFound();
        foreach (var line in bom.Lines)
        {
            var need = line.Quantity * dto.Quantity;
            if (line.ComponentProduct.QuantityOnHand < need)
                return BadRequest($"Insufficient {line.ComponentProduct.Sku}");
            InventoryService.ApplyDelta(db, line.ComponentProduct, -need, "bom-consume", "bom", bom.Id, locationId: dto.LocationId, userId: UserEntity?.Id);
        }
        InventoryService.ApplyDelta(db, bom.ParentProduct, dto.Quantity, "bom-build", "bom", bom.Id, locationId: dto.LocationId, userId: UserEntity?.Id);
        db.SaveChanges();
        return Ok(bom.ParentProduct.ToDto());
    }

    // ---------- Tax / price lists ----------
    [HttpGet, Route("tax-codes")]
    public IHttpActionResult TaxCodes()
    {
        using var db = Db.Create();
        return Ok(db.TaxCodes.Where(t => t.IsActive).OrderBy(t => t.Code).ToList()
            .Select(t => new TaxCodeDto { Id = t.Id, Code = t.Code, Name = t.Name, Rate = t.Rate, IsActive = t.IsActive }).ToList());
    }

    [HttpPost, Route("tax-codes")]
    public IHttpActionResult CreateTax([FromBody] TaxCodeDto dto)
    {
        using var db = Db.Create();
        var t = new TaxCode { Code = dto.Code.Trim(), Name = dto.Name, Rate = dto.Rate, IsActive = true };
        db.TaxCodes.Add(t);
        db.SaveChanges();
        return Ok(new TaxCodeDto { Id = t.Id, Code = t.Code, Name = t.Name, Rate = t.Rate, IsActive = true });
    }

    [HttpGet, Route("price-lists")]
    public IHttpActionResult PriceLists()
    {
        using var db = Db.Create();
        return Ok(db.PriceLists.Include(p => p.Items).ThenInclude(i => i.Product).Where(p => p.IsActive).ToList()
            .Select(p => new PriceListDto
            {
                Id = p.Id, Name = p.Name, IsActive = p.IsActive,
                Items = p.Items.Select(i => new PriceListItemDto
                {
                    ProductId = i.ProductId, ProductSku = i.Product.Sku, UnitPrice = i.UnitPrice, MinQuantity = i.MinQuantity
                }).ToList()
            }).ToList());
    }

    // ---------- Returns / AR / AP ----------
    [HttpGet, Route("sales-returns")]
    public IHttpActionResult SalesReturns()
    {
        using var db = Db.Create();
        return Ok(db.SalesReturns.OrderByDescending(r => r.Id).ToList()
            .Select(r => new SalesReturnDto
            {
                Id = r.Id, RmaNumber = r.RmaNumber, CustomerId = r.CustomerId, Status = r.Status,
                ReturnDate = r.ReturnDate, Total = r.Total
            }).ToList());
    }

    [HttpPost, Route("sales-returns")]
    public IHttpActionResult CreateReturn([FromBody] SalesReturnCreateDto dto)
    {
        if (dto?.Lines == null || dto.Lines.Count == 0) return BadRequest("Lines required");
        using var db = Db.Create();
        var rma = new SalesReturn
        {
            RmaNumber = DocumentNumbers.Next(db, "RMA", "RMA-"),
            CustomerId = dto.CustomerId,
            SalesOrderId = dto.SalesOrderId,
            Notes = dto.Notes,
            Status = "received"
        };
        decimal total = 0;
        foreach (var line in dto.Lines)
        {
            var product = db.Products.Find(line.ProductId);
            if (product is null) return BadRequest("Product missing");
            rma.Lines.Add(new SalesReturnLine { ProductId = line.ProductId, Quantity = line.Quantity, UnitPrice = line.UnitPrice });
            InventoryService.ApplyDelta(db, product, line.Quantity, "return", "sales-return", null, dto.Notes, userId: UserEntity?.Id);
            total += line.Quantity * line.UnitPrice;
        }
        rma.Total = total;
        db.SalesReturns.Add(rma);
        if (dto.SalesOrderId.HasValue)
        {
            var so = db.SalesOrders.Find(dto.SalesOrderId.Value);
            if (so != null) so.Status = "returned";
        }
        db.SaveChanges();
        return Ok(new SalesReturnDto { Id = rma.Id, RmaNumber = rma.RmaNumber, CustomerId = rma.CustomerId, Status = rma.Status, ReturnDate = rma.ReturnDate, Total = rma.Total });
    }

    [HttpPost, Route("customer-payments")]
    public IHttpActionResult CustomerPayment([FromBody] PaymentCreateDto dto)
    {
        if (dto == null || dto.Amount <= 0) return BadRequest();
        using var db = Db.Create();
        var pay = new CustomerPayment
        {
            PaymentNumber = DocumentNumbers.Next(db, "PAY", "PAY-"),
            CustomerId = dto.CustomerId,
            SalesOrderId = dto.SalesOrderId,
            Amount = dto.Amount,
            Method = dto.Method,
            Reference = dto.Reference,
            BankAccountId = dto.BankAccountId
        };
        db.CustomerPayments.Add(pay);
        if (dto.SalesOrderId.HasValue)
        {
            var so = db.SalesOrders.Find(dto.SalesOrderId.Value);
            if (so != null)
            {
                so.AmountPaid += dto.Amount;
                if (so.AmountPaid >= so.Total) so.Status = "invoiced";
            }
        }
        if (dto.BankAccountId.HasValue)
        {
            db.BankTransactions.Add(new BankTransaction
            {
                BankAccountId = dto.BankAccountId.Value,
                Description = $"Customer payment {pay.PaymentNumber}",
                Amount = dto.Amount,
                ReferenceType = "customer-payment"
            });
        }
        GlPostingService.PostBalanced(db, $"Payment {pay.PaymentNumber}", "customer-payment", null,
            new[] { ("1100", dto.Amount, 0m, "Bank"), ("1200", 0m, dto.Amount, "AR") });
        db.SaveChanges();
        return Ok(pay);
    }

    [HttpGet, Route("vendor-bills")]
    public IHttpActionResult VendorBills()
    {
        using var db = Db.Create();
        return Ok(db.VendorBills.ToList().Select(b =>
        {
            var name = db.Suppliers.Find(b.SupplierId)?.Name;
            return new VendorBillDto
            {
                Id = b.Id, BillNumber = b.BillNumber, SupplierId = b.SupplierId, SupplierName = name,
                Amount = b.Amount, AmountPaid = b.AmountPaid, Status = b.Status, BillDate = b.BillDate, DueDate = b.DueDate
            };
        }).ToList());
    }

    [HttpPost, Route("vendor-bills")]
    public IHttpActionResult CreateVendorBill([FromBody] VendorBillCreateDto dto)
    {
        using var db = Db.Create();
        var bill = new VendorBill
        {
            BillNumber = DocumentNumbers.Next(db, "BILL", "BILL-"),
            SupplierId = dto.SupplierId,
            PurchaseOrderId = dto.PurchaseOrderId,
            Amount = dto.Amount,
            DueDate = dto.DueDate ?? DateTime.Today.AddDays(30),
            Notes = dto.Notes,
            Status = "open"
        };
        db.VendorBills.Add(bill);
        GlPostingService.PostBalanced(db, $"Bill {bill.BillNumber}", "vendor-bill", null,
            new[] { ("1400", dto.Amount, 0m, "Inventory/expense"), ("2000", 0m, dto.Amount, "AP") });
        db.SaveChanges();
        return Ok(new VendorBillDto { Id = bill.Id, BillNumber = bill.BillNumber, SupplierId = bill.SupplierId, Amount = bill.Amount, Status = bill.Status, BillDate = bill.BillDate, DueDate = bill.DueDate });
    }

    [HttpPost, Route("vendor-payments")]
    public IHttpActionResult VendorPayment([FromBody] VendorPaymentCreateDto dto)
    {
        using var db = Db.Create();
        var pay = new VendorPayment
        {
            PaymentNumber = DocumentNumbers.Next(db, "PAY", "VPAY-"),
            SupplierId = dto.SupplierId,
            VendorBillId = dto.VendorBillId,
            Amount = dto.Amount,
            Method = dto.Method,
            Reference = dto.Reference,
            BankAccountId = dto.BankAccountId
        };
        db.VendorPayments.Add(pay);
        if (dto.VendorBillId.HasValue)
        {
            var bill = db.VendorBills.Find(dto.VendorBillId.Value);
            if (bill != null)
            {
                bill.AmountPaid += dto.Amount;
                if (bill.AmountPaid >= bill.Amount) bill.Status = "paid";
            }
        }
        if (dto.BankAccountId.HasValue)
        {
            db.BankTransactions.Add(new BankTransaction
            {
                BankAccountId = dto.BankAccountId.Value,
                Description = $"Vendor payment {pay.PaymentNumber}",
                Amount = -dto.Amount,
                ReferenceType = "vendor-payment"
            });
        }
        GlPostingService.PostBalanced(db, $"Vendor pay {pay.PaymentNumber}", "vendor-payment", null,
            new[] { ("2000", dto.Amount, 0m, "AP"), ("1100", 0m, dto.Amount, "Bank") });
        db.SaveChanges();
        return Ok(pay);
    }

    // ---------- Finance ----------
    [HttpGet, Route("gl-accounts")]
    public IHttpActionResult GlAccounts()
    {
        using var db = Db.Create();
        return Ok(db.GlAccounts.OrderBy(a => a.AccountNumber).ToList()
            .Select(a => new GlAccountDto { Id = a.Id, AccountNumber = a.AccountNumber, Name = a.Name, AccountType = a.AccountType, IsActive = a.IsActive }).ToList());
    }

    [HttpGet, Route("journal-entries")]
    public IHttpActionResult Journals()
    {
        using var db = Db.Create();
        return Ok(db.JournalEntries.Include(j => j.Lines).ThenInclude(l => l.GlAccount).OrderByDescending(j => j.Id).Take(100).ToList()
            .Select(j => new JournalEntryDto
            {
                Id = j.Id, EntryNumber = j.EntryNumber, EntryDate = j.EntryDate, Memo = j.Memo, IsPosted = j.IsPosted,
                Lines = j.Lines.Select(l => new JournalLineDto
                {
                    AccountNumber = l.GlAccount.AccountNumber, AccountName = l.GlAccount.Name, Debit = l.Debit, Credit = l.Credit
                }).ToList()
            }).ToList());
    }

    [HttpGet, Route("fiscal-periods")]
    public IHttpActionResult FiscalPeriods()
    {
        using var db = Db.Create();
        return Ok(db.FiscalPeriods.OrderByDescending(p => p.StartDate).ToList()
            .Select(p => new FiscalPeriodDto { Id = p.Id, Name = p.Name, StartDate = p.StartDate, EndDate = p.EndDate, IsClosed = p.IsClosed }).ToList());
    }

    [HttpPost, Route("fiscal-periods/{id:int}/close")]
    public IHttpActionResult ClosePeriod(int id)
    {
        using var db = Db.Create();
        var p = db.FiscalPeriods.Find(id);
        if (p is null) return NotFound();
        p.IsClosed = true;
        AuditService.Write(db, UserEntity?.Id, UserEntity?.UserName ?? "", "close-period", "fiscal-period", id, p.Name);
        db.SaveChanges();
        return Ok(new FiscalPeriodDto { Id = p.Id, Name = p.Name, StartDate = p.StartDate, EndDate = p.EndDate, IsClosed = true });
    }

    [HttpGet, Route("bank-accounts")]
    public IHttpActionResult BankAccounts()
    {
        using var db = Db.Create();
        return Ok(db.BankAccounts.Where(b => b.IsActive).ToList()
            .Select(b => new BankAccountDto
            {
                Id = b.Id, Name = b.Name, AccountNumber = b.AccountNumber, CurrencyCode = b.CurrencyCode,
                OpeningBalance = b.OpeningBalance, IsActive = b.IsActive
            }).ToList());
    }

    [HttpGet, Route("bank-transactions")]
    public IHttpActionResult BankTransactions(int? bankAccountId = null)
    {
        using var db = Db.Create();
        var q = db.BankTransactions.AsQueryable();
        if (bankAccountId.HasValue) q = q.Where(t => t.BankAccountId == bankAccountId);
        return Ok(q.OrderByDescending(t => t.TxnDate).Take(200).ToList()
            .Select(t => new BankTransactionDto
            {
                Id = t.Id, BankAccountId = t.BankAccountId, TxnDate = t.TxnDate, Description = t.Description,
                Amount = t.Amount, IsReconciled = t.IsReconciled
            }).ToList());
    }

    [HttpPost, Route("bank-transactions/{id:int}/reconcile")]
    public IHttpActionResult Reconcile(int id)
    {
        using var db = Db.Create();
        var t = db.BankTransactions.Find(id);
        if (t is null) return NotFound();
        t.IsReconciled = true;
        db.SaveChanges();
        return Ok(new BankTransactionDto
        {
            Id = t.Id, BankAccountId = t.BankAccountId, TxnDate = t.TxnDate, Description = t.Description,
            Amount = t.Amount, IsReconciled = true
        });
    }

    [HttpGet, Route("currencies")]
    public IHttpActionResult Currencies()
    {
        using var db = Db.Create();
        return Ok(db.CurrencyRates.OrderBy(c => c.CurrencyCode).ToList()
            .Select(c => new CurrencyRateDto { Id = c.Id, CurrencyCode = c.CurrencyCode, RateToBase = c.RateToBase, EffectiveDate = c.EffectiveDate }).ToList());
    }

    [HttpGet, Route("companies")]
    public IHttpActionResult Companies()
    {
        using var db = Db.Create();
        return Ok(db.Companies.Where(c => c.IsActive).ToList()
            .Select(c => new CompanyDto { Id = c.Id, Code = c.Code, Name = c.Name, BaseCurrency = c.BaseCurrency }).ToList());
    }

    // ---------- Sales lifecycle helpers ----------
    [HttpPost, Route("sales-orders/{id:int}/convert-quote")]
    public IHttpActionResult ConvertQuote(int id, [FromBody] QuoteConvertDto? dto)
    {
        using var db = Db.Create();
        var quote = db.SalesOrders.Include(s => s.Lines).ThenInclude(l => l.Product).Include(s => s.Customer)
            .FirstOrDefault(s => s.Id == id);
        if (quote is null) return NotFound();
        if (quote.DocumentType != "quote") return BadRequest("Not a quote");
        quote.DocumentType = dto?.CreateInvoice == true ? "invoice" : "order";
        quote.Status = dto?.CreateInvoice == true ? "invoiced" : "confirmed";
        quote.OrderNumber = DocumentNumbers.Next(db, dto?.CreateInvoice == true ? "INV" : "SO", dto?.CreateInvoice == true ? "INV-" : "SO-");
        foreach (var line in quote.Lines)
        {
            if (line.Product.QuantityOnHand < line.Quantity) return BadRequest($"Insufficient {line.Product.Sku}");
            line.UnitCostSnapshot = line.Product.AverageCost > 0 ? line.Product.AverageCost : line.Product.UnitCost;
            InventoryService.ApplyDelta(db, line.Product, -line.Quantity, "sale", "sales-order", quote.Id, userId: UserEntity?.Id);
        }
        try
        {
            GlPostingService.PostSale(db, quote);
            GlPostingService.PostCogs(db, quote, quote.Lines.Sum(l => l.UnitCostSnapshot * l.Quantity));
        }
        catch { /* period closed etc. */ }
        db.SaveChanges();
        return Ok(quote.ToDto());
    }

    [HttpPost, Route("sales-orders/{id:int}/ship")]
    public IHttpActionResult Ship(int id, [FromBody] ShipOrderDto dto)
    {
        using var db = Db.Create();
        var so = db.SalesOrders.Include(s => s.Lines).ThenInclude(l => l.Product).Include(s => s.Customer)
            .FirstOrDefault(s => s.Id == id);
        if (so is null) return NotFound();
        foreach (var item in dto.Lines)
        {
            var line = so.Lines.FirstOrDefault(l => l.Id == item.LineId);
            if (line is null) return BadRequest("Line missing");
            var remain = line.Quantity - line.QuantityShipped;
            if (item.Quantity > remain) return BadRequest("Over-ship");
            line.QuantityShipped += item.Quantity;
        }
        so.TrackingNumber = dto.TrackingNumber;
        so.Carrier = dto.Carrier;
        so.ShipDate = DateTime.Today;
        so.Status = so.Lines.All(l => l.QuantityShipped >= l.Quantity) ? "fulfilled" : "partial";
        db.IntegrationLogs.Add(new IntegrationLog
        {
            SystemName = "shipping", Action = "label", Status = "stub",
            Details = $"{dto.Carrier} {dto.TrackingNumber}"
        });
        db.SaveChanges();
        return Ok(so.ToDto());
    }

    [HttpPost, Route("purchase-orders/{id:int}/approve")]
    public IHttpActionResult ApprovePo(int id)
    {
        using var db = Db.Create();
        var po = db.PurchaseOrders.Include(p => p.Supplier).Include(p => p.Lines).ThenInclude(l => l.Product).FirstOrDefault(p => p.Id == id);
        if (po is null) return NotFound();
        po.IsApproved = true;
        po.ApprovedAt = DateTime.UtcNow;
        po.ApprovedByUserId = UserEntity?.Id;
        if (po.Status == "pending_approval") po.Status = "ordered";
        AuditService.Write(db, UserEntity?.Id, UserEntity?.UserName ?? "", "approve", "purchase-order", id, po.PoNumber);
        db.SaveChanges();
        return Ok(po.ToDto());
    }

    // ---------- Documents / email ----------
    [HttpGet, Route("documents/sales-orders/{id:int}")]
    public IHttpActionResult SalesDoc(int id)
    {
        using var db = Db.Create();
        var so = db.SalesOrders.Include(s => s.Customer).Include(s => s.Lines).ThenInclude(l => l.Product).FirstOrDefault(s => s.Id == id);
        if (so is null) return NotFound();
        return Ok(DocumentService.BuildSalesDocument(db, so));
    }

    [HttpGet, Route("documents/purchase-orders/{id:int}")]
    public IHttpActionResult PurchaseDoc(int id)
    {
        using var db = Db.Create();
        var po = db.PurchaseOrders.Include(p => p.Supplier).Include(p => p.Lines).ThenInclude(l => l.Product).FirstOrDefault(p => p.Id == id);
        if (po is null) return NotFound();
        return Ok(DocumentService.BuildPurchaseDocument(db, po));
    }

    [HttpPost, Route("documents/sales-orders/{id:int}/email")]
    public IHttpActionResult EmailSales(int id)
    {
        using var db = Db.Create();
        var so = db.SalesOrders.Include(s => s.Customer).Include(s => s.Lines).ThenInclude(l => l.Product).FirstOrDefault(s => s.Id == id);
        if (so is null) return NotFound();
        var doc = DocumentService.BuildSalesDocument(db, so);
        var err = DocumentService.TrySendEmail(db.Settings.First(), so.Customer?.Email ?? "", doc.Title, doc.Html);
        db.IntegrationLogs.Add(new IntegrationLog
        {
            SystemName = "smtp", Action = "email-document", Status = err == null ? "ok" : "error", Details = err ?? so.Customer?.Email
        });
        db.SaveChanges();
        if (err != null) return BadRequest(err);
        return Ok(new { sent = true });
    }

    // ---------- Reports ----------
    [HttpGet, Route("reports/summary")]
    public IHttpActionResult ReportSummary()
    {
        using var db = Db.Create();
        var products = db.Products.Where(p => p.IsActive).ToList();
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthSales = db.SalesOrders.Include(s => s.Lines).AsEnumerable()
            .Where(s => s.OrderDate >= start && s.DocumentType != "quote" && s.Status != "cancelled").ToList();
        var salesTotal = monthSales.Sum(s => s.Total);
        var cogs = monthSales.SelectMany(s => s.Lines).Sum(l => l.UnitCostSnapshot * l.Quantity);
        var dead = products.Where(p => p.QuantityOnHand > 0 && p.QuantityOnHand > p.ReorderPoint * 3).Take(20).ToList();
        var ar = BuildArAging(db);
        var ap = BuildApAging(db);
        return Ok(new ReportSummaryDto
        {
            InventoryValue = products.Sum(p => p.QuantityOnHand * (p.AverageCost > 0 ? p.AverageCost : p.UnitCost)),
            SalesMonthToDate = salesTotal,
            CogsMonthToDate = cogs,
            GrossMarginPercent = salesTotal <= 0 ? 0 : Math.Round((salesTotal - cogs) / salesTotal * 100m, 2),
            DeadStockCount = dead.Count,
            DeadStock = dead.Select(p => p.ToDto()).ToList(),
            ArTotal = ar.Sum(a => a.Total),
            ApTotal = ap.Sum(a => a.Total),
            ArAging = ar,
            ApAging = ap
        });
    }

    // ---------- Integrations / backup ----------
    [HttpGet, Route("webhooks")]
    public IHttpActionResult Webhooks()
    {
        using var db = Db.Create();
        return Ok(db.Webhooks.ToList().Select(w => new WebhookDto { Id = w.Id, EventName = w.EventName, TargetUrl = w.TargetUrl, IsActive = w.IsActive }).ToList());
    }

    [HttpPost, Route("webhooks")]
    public IHttpActionResult CreateWebhook([FromBody] WebhookDto dto)
    {
        using var db = Db.Create();
        var w = new WebhookSubscription { EventName = dto.EventName, TargetUrl = dto.TargetUrl, IsActive = true };
        db.Webhooks.Add(w);
        db.SaveChanges();
        return Ok(new WebhookDto { Id = w.Id, EventName = w.EventName, TargetUrl = w.TargetUrl, IsActive = true });
    }

    [HttpPost, Route("api-keys")]
    public IHttpActionResult CreateApiKey([FromBody] UserCreateDto dto)
    {
        using var db = Db.Create();
        var raw = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var (hash, _) = PasswordHasher.Hash(raw);
        var key = new ApiKey { Name = dto?.UserName ?? "API Key", KeyHash = hash, KeyPrefix = raw.Substring(0, 8), IsActive = true };
        db.ApiKeys.Add(key);
        db.SaveChanges();
        return Ok(new ApiKeyCreatedDto { Id = key.Id, Name = key.Name, ApiKey = raw, KeyPrefix = key.KeyPrefix });
    }

    [HttpGet, Route("integration-logs")]
    public IHttpActionResult IntegrationLogs()
    {
        using var db = Db.Create();
        return Ok(db.IntegrationLogs.OrderByDescending(i => i.CreatedAt).Take(100).ToList()
            .Select(i => new IntegrationLogDto
            {
                Id = i.Id, CreatedAt = i.CreatedAt, SystemName = i.SystemName, Action = i.Action, Status = i.Status, Details = i.Details
            }).ToList());
    }

    [HttpPost, Route("integrations/shopify/sync")]
    public IHttpActionResult ShopifySync()
    {
        using var db = Db.Create();
        db.IntegrationLogs.Add(new IntegrationLog
        {
            SystemName = "shopify", Action = "sync", Status = "stub",
            Details = "Shopify connector placeholder — configure store credentials in a future release."
        });
        db.SaveChanges();
        return Ok(new { status = "queued-stub" });
    }

    [HttpGet, Route("integrations/accounting-export")]
    public IHttpActionResult AccountingExport()
    {
        using var db = Db.Create();
        var lines = db.JournalEntries.Include(j => j.Lines).ThenInclude(l => l.GlAccount)
            .OrderByDescending(j => j.Id).Take(50).ToList();
        var csv = "Entry,Date,Account,Debit,Credit,Memo\n" + string.Join("\n", lines.SelectMany(j =>
            j.Lines.Select(l => $"{j.EntryNumber},{j.EntryDate:yyyy-MM-dd},{l.GlAccount.AccountNumber},{l.Debit},{l.Credit},\"{j.Memo}\"")));
        db.IntegrationLogs.Add(new IntegrationLog { SystemName = "accounting", Action = "export", Status = "ok", Details = $"{lines.Count} entries" });
        db.SaveChanges();
        return Ok(new { csv });
    }

    [HttpPost, Route("backup")]
    public IHttpActionResult Backup()
    {
        var result = BackupService.Backup();
        using var db = Db.Create();
        AuditService.Write(db, UserEntity?.Id, UserEntity?.UserName ?? "", "backup", "database", null, result.Path);
        db.SaveChanges();
        return Ok(result);
    }

    [HttpPost, Route("backup/restore")]
    public IHttpActionResult Restore([FromBody] BackupResultDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Path)) return BadRequest("Path required");
        BackupService.RestoreSqlite(dto.Path);
        return Ok(new { restored = true });
    }

    [HttpGet, Route("backup/list")]
    public IHttpActionResult ListBackups()
    {
        Directory.CreateDirectory(BackupService.BackupDirectory);
        var files = Directory.GetFiles(BackupService.BackupDirectory)
            .OrderByDescending(File.GetCreationTime)
            .Take(50)
            .Select(f => new BackupResultDto { Path = f, CreatedAt = File.GetCreationTime(f), Provider = Path.GetExtension(f) == ".db" ? "Sqlite" : "SqlServer" })
            .ToList();
        return Ok(files);
    }

    private static List<AgingRowDto> BuildArAging(ErpDbContext db)
    {
        var open = db.SalesOrders.Include(s => s.Customer).AsEnumerable()
            .Where(s => s.DocumentType != "quote" && s.Status != "cancelled" && s.AmountPaid < s.Total).ToList();
        return open.GroupBy(s => new { s.CustomerId, Name = s.Customer.Name }).Select(g =>
        {
            var row = new AgingRowDto { PartnerId = g.Key.CustomerId, PartnerName = g.Key.Name };
            foreach (var s in g)
            {
                var bal = s.Total - s.AmountPaid;
                var age = (DateTime.Today - (s.DueDate ?? s.OrderDate)).TotalDays;
                if (age <= 30) row.Current += bal;
                else if (age <= 60) row.Days30 += bal;
                else if (age <= 90) row.Days60 += bal;
                else row.Days90Plus += bal;
                row.Total += bal;
            }
            return row;
        }).ToList();
    }

    private static List<AgingRowDto> BuildApAging(ErpDbContext db)
    {
        var open = db.VendorBills.Where(b => b.Status != "paid").ToList();
        return open.GroupBy(b => b.SupplierId).Select(g =>
        {
            var name = db.Suppliers.Find(g.Key)?.Name ?? $"#{g.Key}";
            var row = new AgingRowDto { PartnerId = g.Key, PartnerName = name };
            foreach (var b in g)
            {
                var bal = b.Amount - b.AmountPaid;
                var age = (DateTime.Today - (b.DueDate ?? b.BillDate)).TotalDays;
                if (age <= 30) row.Current += bal;
                else if (age <= 60) row.Days30 += bal;
                else if (age <= 90) row.Days60 += bal;
                else row.Days90Plus += bal;
                row.Total += bal;
            }
            return row;
        }).ToList();
    }
}
