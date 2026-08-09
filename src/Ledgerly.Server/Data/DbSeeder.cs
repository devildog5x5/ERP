using System;
using System.Linq;
using Ledgerly.Server.Services;

namespace Ledgerly.Server.Data;

public static class DbSeeder
{
    public const string AllPermissions =
        "dashboard,inventory,purchasing,sales,partners,reminders,scan,settings,users,audit,locations,warehouse,finance,reports,integrations,approvals,backup,print";

    public static void Seed()
    {
        using var db = Db.Create();
        SchemaMigrator.Apply(db);
        SeedEnterpriseDefaults(db);

        if (db.Products.Any())
        {
            foreach (var p in db.Products.Where(x => x.Upc == null || x.Upc == "").ToList())
            {
                if (p.Sku == "COF-BEAN-1KG") p.Upc = "00012345678905";
                else if (p.Sku == "CUP-12OZ") p.Upc = "00012345678912";
                else if (p.Sku == "NAP-WHT") p.Upc = "00012345678929";
                else if (p.Sku == "SYR-VAN") p.Upc = "00012345678936";
                else if (p.Sku == "FLT-PAPER") p.Upc = "00012345678943";
            }
            foreach (var p in db.Products.AsEnumerable().Where(x => x.AverageCost == 0 && x.UnitCost > 0).ToList())
                p.AverageCost = p.UnitCost;

            // Demo syrup was seeded at 0 — restock so sales orders can include it while still low.
            var syrup = db.Products.FirstOrDefault(p => p.Sku == "SYR-VAN");
            if (syrup != null && syrup.QuantityOnHand <= 0)
            {
                InventoryService.ApplyDelta(db, syrup, 3, "seed-restock",
                    notes: "Demo restock so SYR-VAN is sellable");
            }

            SyncProductLocations(db);
            db.SaveChanges();
            ReminderScanner.Scan(db);
            return;
        }

        var suppliers = new[]
        {
            new Supplier { Name = "Northwind Supplies", Email = "orders@northwind.example", Phone = "555-0101", PaymentTermsDays = 30 },
            new Supplier { Name = "Summit Packaging Co", Email = "sales@summitpack.example", Phone = "555-0144", PaymentTermsDays = 30 },
        };
        var customers = new[]
        {
            new Customer { Name = "Cafe Lumen", Email = "hello@cafelumen.example", Phone = "555-0202", PaymentTermsDays = 15 },
            new Customer { Name = "Harbor Retail", Email = "buyer@harborretail.example", Phone = "555-0218", PaymentTermsDays = 30, CreditLimit = 5000 },
        };
        db.Suppliers.AddRange(suppliers);
        db.Customers.AddRange(customers);
        db.SaveChanges();

        var tax = db.TaxCodes.First();
        var products = new[]
        {
            new Product { Sku = "COF-BEAN-1KG", Upc = "00012345678905", Name = "Coffee Beans 1kg", Category = "Grocery", Unit = "bag", QuantityOnHand = 4, ReorderPoint = 12, ReorderQuantity = 24, UnitCost = 8.50m, AverageCost = 8.50m, SellPrice = 14m, SupplierId = suppliers[0].Id, TaxCodeId = tax.Id },
            new Product { Sku = "CUP-12OZ", Upc = "00012345678912", Name = "Paper Cups 12oz (50pk)", Category = "Packaging", Unit = "pack", QuantityOnHand = 2, ReorderPoint = 10, ReorderQuantity = 20, UnitCost = 3.25m, AverageCost = 3.25m, SellPrice = 6.50m, SupplierId = suppliers[1].Id, TaxCodeId = tax.Id },
            new Product { Sku = "NAP-WHT", Upc = "00012345678929", Name = "White Napkins", Category = "Packaging", Unit = "pack", QuantityOnHand = 40, ReorderPoint = 15, ReorderQuantity = 30, UnitCost = 1.10m, AverageCost = 1.10m, SellPrice = 2.50m, SupplierId = suppliers[1].Id, TaxCodeId = tax.Id },
            // Keep below reorder point for reminders, but sellable for demo sales orders.
            new Product { Sku = "SYR-VAN", Upc = "00012345678936", Name = "Vanilla Syrup", Category = "Grocery", Unit = "bottle", QuantityOnHand = 3, ReorderPoint = 6, ReorderQuantity = 12, UnitCost = 4.75m, AverageCost = 4.75m, SellPrice = 9m, SupplierId = suppliers[0].Id, TaxCodeId = tax.Id },
            new Product { Sku = "FLT-PAPER", Upc = "00012345678943", Name = "Coffee Filters", Category = "Consumables", Unit = "box", QuantityOnHand = 18, ReorderPoint = 8, ReorderQuantity = 16, UnitCost = 2m, AverageCost = 2m, SellPrice = 4.25m, SupplierId = suppliers[0].Id, TaxCodeId = tax.Id },
        };
        db.Products.AddRange(products);
        db.SaveChanges();
        SyncProductLocations(db);

        var pl = new PriceList { Name = "Retail", IsActive = true };
        db.PriceLists.Add(pl);
        db.SaveChanges();
        foreach (var p in products)
            db.PriceListItems.Add(new PriceListItem { PriceListId = pl.Id, ProductId = p.Id, UnitPrice = p.SellPrice });
        customers[1].PriceListId = pl.Id;

        var today = DateTime.Today;
        var loc = db.Locations.First().Id;
        db.PurchaseOrders.AddRange(
            new PurchaseOrder
            {
                PoNumber = $"PO-{today:yyyyMMdd}-0001",
                SupplierId = suppliers[0].Id,
                Status = "ordered",
                OrderDate = today.AddDays(-10),
                ExpectedDate = today.AddDays(-2),
                Notes = "Demo overdue PO",
                Total = 102m,
                LocationId = loc,
                IsApproved = true,
                Lines = { new PurchaseOrderLine { ProductId = products[0].Id, QuantityOrdered = 12, UnitCost = 8.50m } }
            },
            new PurchaseOrder
            {
                PoNumber = $"PO-{today:yyyyMMdd}-0002",
                SupplierId = suppliers[1].Id,
                Status = "ordered",
                OrderDate = today.AddDays(-3),
                ExpectedDate = today.AddDays(1),
                Notes = "Demo incoming delivery",
                Total = 65m,
                LocationId = loc,
                IsApproved = true,
                Lines = { new PurchaseOrderLine { ProductId = products[1].Id, QuantityOrdered = 20, UnitCost = 3.25m } }
            });
        db.SaveChanges();
        ReminderScanner.Scan(db);
    }

    private static void SeedEnterpriseDefaults(ErpDbContext db)
    {
        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Role { Name = "Administrator", Permissions = AllPermissions },
                new Role { Name = "Manager", Permissions = "dashboard,inventory,purchasing,sales,partners,reminders,scan,reports,approvals,print,warehouse,locations" },
                new Role { Name = "Clerk", Permissions = "dashboard,inventory,sales,partners,scan,print" },
                new Role { Name = "Warehouse", Permissions = "dashboard,inventory,scan,warehouse,locations,purchasing" },
                new Role { Name = "Accountant", Permissions = "dashboard,finance,reports,sales,purchasing,partners,print,backup" });
            db.SaveChanges();
        }
        else
        {
            // User administration must stay Administrator-only — strip "users" from other roles.
            foreach (var role in db.Roles.Where(r => r.Name != "Administrator").ToList())
            {
                var parts = (role.Permissions ?? "")
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 0 && !p.Equals("users", StringComparison.OrdinalIgnoreCase)
                                             && !p.Equals("admin", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var cleaned = string.Join(",", parts);
                if (!string.Equals(cleaned, role.Permissions, StringComparison.Ordinal))
                    role.Permissions = cleaned;
            }
            var adminRole = db.Roles.FirstOrDefault(r => r.Name == "Administrator");
            if (adminRole != null && !string.Equals(adminRole.Permissions, AllPermissions, StringComparison.Ordinal))
                adminRole.Permissions = AllPermissions;
            db.SaveChanges();
        }

        if (!db.Users.Any())
        {
            var adminRole = db.Roles.First(r => r.Name == "Administrator");
            var (hash, salt) = PasswordHasher.Hash("admin");
            db.Users.Add(new AppUser
            {
                UserName = "admin",
                DisplayName = "System Admin",
                PasswordHash = hash,
                PasswordSalt = salt,
                RoleId = adminRole.Id,
                IsActive = true
            });
            db.SaveChanges();
        }

        if (!db.Locations.Any())
        {
            db.Locations.AddRange(
                new Location { Code = "MAIN", Name = "Main Warehouse", Bin = "A-01" },
                new Location { Code = "FRONT", Name = "Front Counter", Bin = "F-01" });
            db.SaveChanges();
            var settings = db.Settings.First();
            settings.DefaultLocationId = db.Locations.First().Id;
            db.SaveChanges();
        }

        if (!db.TaxCodes.Any())
        {
            db.TaxCodes.AddRange(
                new TaxCode { Code = "TAX0", Name = "No tax", Rate = 0 },
                new TaxCode { Code = "TAXSTD", Name = "Standard sales tax", Rate = 8.25m });
            db.SaveChanges();
            var settings = db.Settings.First();
            settings.DefaultTaxRate = 8.25m;
            db.SaveChanges();
        }

        if (!db.Companies.Any())
        {
            db.Companies.Add(new Company { Code = "MAIN", Name = db.Settings.First().CompanyName, BaseCurrency = "USD" });
            db.SaveChanges();
        }

        if (!db.CurrencyRates.Any())
        {
            db.CurrencyRates.AddRange(
                new CurrencyRate { CurrencyCode = "USD", RateToBase = 1, EffectiveDate = DateTime.Today },
                new CurrencyRate { CurrencyCode = "EUR", RateToBase = 1.08m, EffectiveDate = DateTime.Today },
                new CurrencyRate { CurrencyCode = "CAD", RateToBase = 0.74m, EffectiveDate = DateTime.Today });
            db.SaveChanges();
        }

        if (!db.GlAccounts.Any())
        {
            db.GlAccounts.AddRange(
                new GlAccount { AccountNumber = "1000", Name = "Cash", AccountType = "asset" },
                new GlAccount { AccountNumber = "1100", Name = "Bank", AccountType = "asset" },
                new GlAccount { AccountNumber = "1200", Name = "Accounts Receivable", AccountType = "asset" },
                new GlAccount { AccountNumber = "1400", Name = "Inventory", AccountType = "asset" },
                new GlAccount { AccountNumber = "2000", Name = "Accounts Payable", AccountType = "liability" },
                new GlAccount { AccountNumber = "2200", Name = "Tax Payable", AccountType = "liability" },
                new GlAccount { AccountNumber = "3000", Name = "Owner Equity", AccountType = "equity" },
                new GlAccount { AccountNumber = "4000", Name = "Sales Revenue", AccountType = "revenue" },
                new GlAccount { AccountNumber = "5000", Name = "Cost of Goods Sold", AccountType = "expense" },
                new GlAccount { AccountNumber = "6000", Name = "Operating Expense", AccountType = "expense" });
            db.SaveChanges();
        }

        if (!db.BankAccounts.Any())
        {
            db.BankAccounts.Add(new BankAccount { Name = "Operating Checking", AccountNumber = "****1001", CurrencyCode = "USD", OpeningBalance = 10000 });
            db.SaveChanges();
        }

        if (!db.FiscalPeriods.Any())
        {
            var year = DateTime.Today.Year;
            db.FiscalPeriods.Add(new FiscalPeriod
            {
                Name = $"FY {year}",
                StartDate = new DateTime(year, 1, 1),
                EndDate = new DateTime(year, 12, 31),
                IsClosed = false
            });
            db.SaveChanges();
        }

        if (!db.NumberSequences.Any())
        {
            db.NumberSequences.AddRange(
                new NumberSequence { DocumentType = "PO", Prefix = "PO-", NextValue = 100 },
                new NumberSequence { DocumentType = "SO", Prefix = "SO-", NextValue = 100 },
                new NumberSequence { DocumentType = "QT", Prefix = "QT-", NextValue = 100 },
                new NumberSequence { DocumentType = "INV", Prefix = "INV-", NextValue = 100 },
                new NumberSequence { DocumentType = "RMA", Prefix = "RMA-", NextValue = 100 },
                new NumberSequence { DocumentType = "JE", Prefix = "JE-", NextValue = 100 },
                new NumberSequence { DocumentType = "XFER", Prefix = "XF-", NextValue = 100 },
                new NumberSequence { DocumentType = "CC", Prefix = "CC-", NextValue = 100 },
                new NumberSequence { DocumentType = "PAY", Prefix = "PAY-", NextValue = 100 },
                new NumberSequence { DocumentType = "BILL", Prefix = "BILL-", NextValue = 100 });
            db.SaveChanges();
        }
    }

    private static void SyncProductLocations(ErpDbContext db)
    {
        var locId = db.Settings.First().DefaultLocationId ?? db.Locations.First().Id;
        foreach (var p in db.Products.Where(p => p.IsActive).ToList())
        {
            var pl = db.ProductLocations.FirstOrDefault(x => x.ProductId == p.Id && x.LocationId == locId);
            if (pl is null)
                db.ProductLocations.Add(new ProductLocation { ProductId = p.Id, LocationId = locId, Quantity = p.QuantityOnHand });
            else if (pl.Quantity == 0 && p.QuantityOnHand != 0)
                pl.Quantity = p.QuantityOnHand;
        }
        db.SaveChanges();
    }
}
