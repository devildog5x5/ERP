using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Server.Data;

public static class SchemaMigrator
{
    public static void Apply(ErpDbContext db)
    {
        db.Database.EnsureCreated();

        if (Db.Provider == DatabaseProvider.Sqlite)
        {
            ApplySqliteColumnUpgrades(db);
            // EnsureCreated only runs on brand-new DBs; create any missing enterprise tables.
            EnsureSqliteEnterpriseTables(db);
            EnsureSqliteCrmTables(db);
        }

        if (!db.Settings.Any())
        {
            db.Settings.Add(new CompanySettings
            {
                Id = 1,
                CompanyName = "Coalesce.ERP.CRM",
                DefaultTaxRate = 0,
                Currency = "USD",
                ReceiptFooter = "Thank you for your business.",
                PoApprovalThreshold = 1000,
                RequireLogin = true
            });
            db.SaveChanges();
        }

        foreach (var so in db.SalesOrders.AsEnumerable().Where(s => s.Subtotal == 0 && s.Total != 0).ToList())
        {
            so.Subtotal = so.Total;
            so.TaxRate = 0;
            so.TaxAmount = 0;
        }
        db.SaveChanges();
    }

    private static void ApplySqliteColumnUpgrades(ErpDbContext db)
    {
        EnsureSqliteColumn(db, "Products", "Upc", "TEXT NULL");
        EnsureSqliteColumn(db, "Products", "AverageCost", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "Products", "TaxCodeId", "INTEGER NULL");
        EnsureSqliteColumn(db, "Products", "CostingMethod", "TEXT NOT NULL DEFAULT 'average'");
        EnsureSqliteColumn(db, "Products", "TrackLots", "INTEGER NOT NULL DEFAULT 0");
        EnsureSqliteColumn(db, "Products", "TrackSerials", "INTEGER NOT NULL DEFAULT 0");
        EnsureSqliteColumn(db, "Products", "IsKit", "INTEGER NOT NULL DEFAULT 0");
        EnsureSqliteColumn(db, "SalesOrders", "Subtotal", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "SalesOrders", "TaxRate", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "SalesOrders", "TaxAmount", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "SalesOrders", "DocumentType", "TEXT NOT NULL DEFAULT 'order'");
        EnsureSqliteColumn(db, "SalesOrders", "DiscountAmount", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "SalesOrders", "AmountPaid", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "SalesOrders", "CurrencyCode", "TEXT NOT NULL DEFAULT 'USD'");
        EnsureSqliteColumn(db, "SalesOrders", "ExchangeRate", "TEXT NOT NULL DEFAULT '1'");
        EnsureSqliteColumn(db, "SalesOrders", "LocationId", "INTEGER NULL");
        EnsureSqliteColumn(db, "SalesOrders", "TrackingNumber", "TEXT NULL");
        EnsureSqliteColumn(db, "SalesOrders", "Carrier", "TEXT NULL");
        EnsureSqliteColumn(db, "SalesOrders", "DueDate", "TEXT NULL");
        EnsureSqliteColumn(db, "SalesOrders", "ShipDate", "TEXT NULL");
        EnsureSqliteColumn(db, "SalesOrders", "ConvertedFromQuoteId", "INTEGER NULL");
        EnsureSqliteColumn(db, "SalesOrderLines", "QuantityShipped", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "SalesOrderLines", "DiscountPercent", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "SalesOrderLines", "UnitCostSnapshot", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "SalesOrderLines", "LotNumber", "TEXT NULL");
        EnsureSqliteColumn(db, "SalesOrderLines", "SerialNumber", "TEXT NULL");
        EnsureSqliteColumn(db, "PurchaseOrders", "LandedCost", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "PurchaseOrders", "CurrencyCode", "TEXT NOT NULL DEFAULT 'USD'");
        EnsureSqliteColumn(db, "PurchaseOrders", "ExchangeRate", "TEXT NOT NULL DEFAULT '1'");
        EnsureSqliteColumn(db, "PurchaseOrders", "LocationId", "INTEGER NULL");
        EnsureSqliteColumn(db, "PurchaseOrders", "ApprovalRequired", "INTEGER NOT NULL DEFAULT 0");
        EnsureSqliteColumn(db, "PurchaseOrders", "IsApproved", "INTEGER NOT NULL DEFAULT 0");
        EnsureSqliteColumn(db, "PurchaseOrders", "ApprovedByUserId", "INTEGER NULL");
        EnsureSqliteColumn(db, "PurchaseOrders", "ApprovedAt", "TEXT NULL");
        EnsureSqliteColumn(db, "PurchaseOrderLines", "LotNumber", "TEXT NULL");
        EnsureSqliteColumn(db, "Customers", "TaxExempt", "INTEGER NOT NULL DEFAULT 0");
        EnsureSqliteColumn(db, "Customers", "PriceListId", "INTEGER NULL");
        EnsureSqliteColumn(db, "Customers", "CurrencyCode", "TEXT NULL");
        EnsureSqliteColumn(db, "Customers", "PaymentTermsDays", "INTEGER NULL");
        EnsureSqliteColumn(db, "Customers", "CreditLimit", "TEXT NOT NULL DEFAULT '0'");
        EnsureSqliteColumn(db, "Suppliers", "CurrencyCode", "TEXT NULL");
        EnsureSqliteColumn(db, "Suppliers", "PaymentTermsDays", "INTEGER NULL");
        EnsureSqliteColumn(db, "StockMovements", "LocationId", "INTEGER NULL");
        EnsureSqliteColumn(db, "StockMovements", "ReasonCode", "TEXT NULL");
        EnsureSqliteColumn(db, "StockMovements", "LotNumber", "TEXT NULL");
        EnsureSqliteColumn(db, "StockMovements", "SerialNumber", "TEXT NULL");
        EnsureSqliteColumn(db, "StockMovements", "UserId", "INTEGER NULL");
        EnsureSqliteColumn(db, "CompanySettings", "Address", "TEXT NULL");
        EnsureSqliteColumn(db, "CompanySettings", "Phone", "TEXT NULL");
        EnsureSqliteColumn(db, "CompanySettings", "Email", "TEXT NULL");
        EnsureSqliteColumn(db, "CompanySettings", "SmtpHost", "TEXT NULL");
        EnsureSqliteColumn(db, "CompanySettings", "SmtpPort", "INTEGER NOT NULL DEFAULT 587");
        EnsureSqliteColumn(db, "CompanySettings", "SmtpUsername", "TEXT NULL");
        EnsureSqliteColumn(db, "CompanySettings", "SmtpPassword", "TEXT NULL");
        EnsureSqliteColumn(db, "CompanySettings", "SmtpEnableSsl", "INTEGER NOT NULL DEFAULT 1");
        EnsureSqliteColumn(db, "CompanySettings", "SmtpFrom", "TEXT NULL");
        EnsureSqliteColumn(db, "CompanySettings", "PoApprovalThreshold", "TEXT NOT NULL DEFAULT '1000'");
        EnsureSqliteColumn(db, "CompanySettings", "RequireLogin", "INTEGER NOT NULL DEFAULT 1");
        EnsureSqliteColumn(db, "CompanySettings", "DefaultLocationId", "INTEGER NULL");
        EnsureSqliteColumn(db, "CompanySettings", "FiscalYearStart", "TEXT NULL");
    }

    private static void EnsureSqliteCrmTables(ErpDbContext db)
    {
        foreach (var sql in CrmSqliteDdl.Statements)
            db.Database.ExecuteSqlRaw(sql);
    }

    private static void EnsureSqliteEnterpriseTables(ErpDbContext db)
    {
        // If Roles is missing, recreate DB schema pieces EF didn't add.
        if (TableExists(db, "Roles")) return;

        // Drop and recreate is too destructive; instead delete file instruction —
        // For in-place: use EnsureCreated on empty is done. For existing DBs without Roles,
        // recreate by copying: simplest path used here is to close and rebuild via EnsureDeleted only when empty of enterprise.
        // Practical approach: Execute EnsureCreated already done; force model creation by deleting SQLite and reseeding if no Roles.
        try
        {
            db.Roles.Count();
        }
        catch
        {
            RebuildSqliteWithDataPreserve(db);
        }
    }

    private static void RebuildSqliteWithDataPreserve(ErpDbContext db)
    {
        // Fallback: if enterprise tables are missing, EnsureDeleted+EnsureCreated loses data.
        // Create tables using EF Core's EnsureCreated by cloning connection to a new file is complex.
        // Use raw SQL batch for critical auth/location tables so the app can boot; remaining tables via EnsureCreated-like SQL.
        foreach (var sql in EnterpriseSqliteDdl.Statements)
        {
            try { db.Database.ExecuteSqlRaw(sql); } catch { /* already exists */ }
        }
    }

    private static bool TableExists(ErpDbContext db, string table)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@n";
        var p = cmd.CreateParameter();
        p.ParameterName = "@n";
        p.Value = table;
        cmd.Parameters.Add(p);
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }

    private static void EnsureSqliteColumn(ErpDbContext db, string table, string column, string sqlType)
    {
        if (!TableExists(db, table)) return;
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info('{table}')";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) cols.Add(reader.GetString(1));
        reader.Close();
        if (!cols.Contains(column))
            db.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {sqlType}");
    }
}

internal static class EnterpriseSqliteDdl
{
    public static readonly string[] Statements =
    {
        @"CREATE TABLE IF NOT EXISTS Roles (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Permissions TEXT NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserName TEXT NOT NULL, DisplayName TEXT NOT NULL, PasswordHash TEXT NOT NULL, PasswordSalt TEXT NOT NULL, RoleId INTEGER NOT NULL, IsActive INTEGER NOT NULL, CreatedAt TEXT NOT NULL, FOREIGN KEY(RoleId) REFERENCES Roles(Id));",
        @"CREATE TABLE IF NOT EXISTS AuthTokens (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, Token TEXT NOT NULL, ExpiresAt TEXT NOT NULL, FOREIGN KEY(UserId) REFERENCES Users(Id));",
        @"CREATE TABLE IF NOT EXISTS AuditLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, CreatedAt TEXT NOT NULL, UserId INTEGER NULL, UserName TEXT NOT NULL, Action TEXT NOT NULL, EntityType TEXT NOT NULL, EntityId INTEGER NULL, Details TEXT NULL);",
        @"CREATE TABLE IF NOT EXISTS Locations (Id INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NOT NULL, Name TEXT NOT NULL, Bin TEXT NULL, IsActive INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS ProductLocations (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProductId INTEGER NOT NULL, LocationId INTEGER NOT NULL, Quantity TEXT NOT NULL, FOREIGN KEY(ProductId) REFERENCES Products(Id), FOREIGN KEY(LocationId) REFERENCES Locations(Id));",
        @"CREATE TABLE IF NOT EXISTS StockTransfers (Id INTEGER PRIMARY KEY AUTOINCREMENT, TransferNumber TEXT NOT NULL, FromLocationId INTEGER NOT NULL, ToLocationId INTEGER NOT NULL, ProductId INTEGER NOT NULL, Quantity TEXT NOT NULL, Status TEXT NOT NULL, TransferDate TEXT NOT NULL, Notes TEXT NULL, UserId INTEGER NULL);",
        @"CREATE TABLE IF NOT EXISTS CycleCounts (Id INTEGER PRIMARY KEY AUTOINCREMENT, CountNumber TEXT NOT NULL, LocationId INTEGER NOT NULL, ProductId INTEGER NOT NULL, SystemQty TEXT NOT NULL, CountedQty TEXT NOT NULL, ReasonCode TEXT NULL, Status TEXT NOT NULL, CountDate TEXT NOT NULL, UserId INTEGER NULL);",
        @"CREATE TABLE IF NOT EXISTS Boms (Id INTEGER PRIMARY KEY AUTOINCREMENT, ParentProductId INTEGER NOT NULL, Name TEXT NOT NULL, FOREIGN KEY(ParentProductId) REFERENCES Products(Id));",
        @"CREATE TABLE IF NOT EXISTS BomLines (Id INTEGER PRIMARY KEY AUTOINCREMENT, BomId INTEGER NOT NULL, ComponentProductId INTEGER NOT NULL, Quantity TEXT NOT NULL, FOREIGN KEY(BomId) REFERENCES Boms(Id), FOREIGN KEY(ComponentProductId) REFERENCES Products(Id));",
        @"CREATE TABLE IF NOT EXISTS TaxCodes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NOT NULL, Name TEXT NOT NULL, Rate TEXT NOT NULL, IsActive INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS PriceLists (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, IsActive INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS PriceListItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, PriceListId INTEGER NOT NULL, ProductId INTEGER NOT NULL, UnitPrice TEXT NOT NULL, MinQuantity TEXT NULL, FOREIGN KEY(PriceListId) REFERENCES PriceLists(Id), FOREIGN KEY(ProductId) REFERENCES Products(Id));",
        @"CREATE TABLE IF NOT EXISTS SalesReturns (Id INTEGER PRIMARY KEY AUTOINCREMENT, RmaNumber TEXT NOT NULL, CustomerId INTEGER NOT NULL, SalesOrderId INTEGER NULL, Status TEXT NOT NULL, ReturnDate TEXT NOT NULL, Total TEXT NOT NULL, Notes TEXT NULL);",
        @"CREATE TABLE IF NOT EXISTS SalesReturnLines (Id INTEGER PRIMARY KEY AUTOINCREMENT, SalesReturnId INTEGER NOT NULL, ProductId INTEGER NOT NULL, Quantity TEXT NOT NULL, UnitPrice TEXT NOT NULL, FOREIGN KEY(SalesReturnId) REFERENCES SalesReturns(Id));",
        @"CREATE TABLE IF NOT EXISTS CustomerPayments (Id INTEGER PRIMARY KEY AUTOINCREMENT, PaymentNumber TEXT NOT NULL, CustomerId INTEGER NOT NULL, SalesOrderId INTEGER NULL, PaymentDate TEXT NOT NULL, Amount TEXT NOT NULL, Method TEXT NOT NULL, Reference TEXT NULL, BankAccountId INTEGER NULL);",
        @"CREATE TABLE IF NOT EXISTS VendorBills (Id INTEGER PRIMARY KEY AUTOINCREMENT, BillNumber TEXT NOT NULL, SupplierId INTEGER NOT NULL, PurchaseOrderId INTEGER NULL, BillDate TEXT NOT NULL, DueDate TEXT NULL, Amount TEXT NOT NULL, AmountPaid TEXT NOT NULL, Status TEXT NOT NULL, Notes TEXT NULL);",
        @"CREATE TABLE IF NOT EXISTS VendorPayments (Id INTEGER PRIMARY KEY AUTOINCREMENT, PaymentNumber TEXT NOT NULL, SupplierId INTEGER NOT NULL, VendorBillId INTEGER NULL, PaymentDate TEXT NOT NULL, Amount TEXT NOT NULL, Method TEXT NOT NULL, Reference TEXT NULL, BankAccountId INTEGER NULL);",
        @"CREATE TABLE IF NOT EXISTS GlAccounts (Id INTEGER PRIMARY KEY AUTOINCREMENT, AccountNumber TEXT NOT NULL, Name TEXT NOT NULL, AccountType TEXT NOT NULL, IsActive INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS JournalEntries (Id INTEGER PRIMARY KEY AUTOINCREMENT, EntryNumber TEXT NOT NULL, EntryDate TEXT NOT NULL, Memo TEXT NOT NULL, SourceType TEXT NULL, SourceId INTEGER NULL, IsPosted INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS JournalLines (Id INTEGER PRIMARY KEY AUTOINCREMENT, JournalEntryId INTEGER NOT NULL, GlAccountId INTEGER NOT NULL, Debit TEXT NOT NULL, Credit TEXT NOT NULL, Memo TEXT NULL, FOREIGN KEY(JournalEntryId) REFERENCES JournalEntries(Id), FOREIGN KEY(GlAccountId) REFERENCES GlAccounts(Id));",
        @"CREATE TABLE IF NOT EXISTS FiscalPeriods (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, StartDate TEXT NOT NULL, EndDate TEXT NOT NULL, IsClosed INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS BankAccounts (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, AccountNumber TEXT NOT NULL, CurrencyCode TEXT NOT NULL, OpeningBalance TEXT NOT NULL, IsActive INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS BankTransactions (Id INTEGER PRIMARY KEY AUTOINCREMENT, BankAccountId INTEGER NOT NULL, TxnDate TEXT NOT NULL, Description TEXT NOT NULL, Amount TEXT NOT NULL, IsReconciled INTEGER NOT NULL, ReferenceType TEXT NULL, ReferenceId INTEGER NULL, FOREIGN KEY(BankAccountId) REFERENCES BankAccounts(Id));",
        @"CREATE TABLE IF NOT EXISTS CurrencyRates (Id INTEGER PRIMARY KEY AUTOINCREMENT, CurrencyCode TEXT NOT NULL, RateToBase TEXT NOT NULL, EffectiveDate TEXT NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS Companies (Id INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NOT NULL, Name TEXT NOT NULL, BaseCurrency TEXT NOT NULL, IsActive INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS ApiKeys (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, KeyHash TEXT NOT NULL, KeyPrefix TEXT NOT NULL, IsActive INTEGER NOT NULL, CreatedAt TEXT NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS Webhooks (Id INTEGER PRIMARY KEY AUTOINCREMENT, EventName TEXT NOT NULL, TargetUrl TEXT NOT NULL, IsActive INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS IntegrationLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, CreatedAt TEXT NOT NULL, SystemName TEXT NOT NULL, Action TEXT NOT NULL, Status TEXT NOT NULL, Details TEXT NULL);",
        @"CREATE TABLE IF NOT EXISTS NumberSequences (Id INTEGER PRIMARY KEY AUTOINCREMENT, DocumentType TEXT NOT NULL, Prefix TEXT NOT NULL, NextValue INTEGER NOT NULL);"
    };
}

internal static class CrmSqliteDdl
{
    public static readonly string[] Statements =
    {
        @"CREATE TABLE IF NOT EXISTS CrmLeads (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, CompanyName TEXT NULL, Email TEXT NULL, Phone TEXT NULL, Source TEXT NULL, Status TEXT NOT NULL, OwnerUserId INTEGER NULL, CreatedAt TEXT NOT NULL, ConvertedAccountId INTEGER NULL, ConvertedCustomerId INTEGER NULL);",
        @"CREATE TABLE IF NOT EXISTS CrmAccounts (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, CustomerId INTEGER NULL, Industry TEXT NULL, Website TEXT NULL, BillingEmail TEXT NULL, IsActive INTEGER NOT NULL, OwnerUserId INTEGER NULL, CreatedAt TEXT NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS CrmContacts (Id INTEGER PRIMARY KEY AUTOINCREMENT, AccountId INTEGER NULL, LeadId INTEGER NULL, FirstName TEXT NOT NULL, LastName TEXT NULL, Email TEXT NULL, Phone TEXT NULL, Title TEXT NULL, IsPrimary INTEGER NOT NULL, IsActive INTEGER NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS CrmOpportunities (Id INTEGER PRIMARY KEY AUTOINCREMENT, AccountId INTEGER NOT NULL, PrimaryContactId INTEGER NULL, Name TEXT NOT NULL, Stage TEXT NOT NULL, Amount TEXT NULL, ExpectedClose TEXT NULL, OwnerUserId INTEGER NULL, SalesOrderId INTEGER NULL, LostReason TEXT NULL, CreatedAt TEXT NOT NULL);",
        @"CREATE TABLE IF NOT EXISTS CrmActivities (Id INTEGER PRIMARY KEY AUTOINCREMENT, ActivityType TEXT NOT NULL, Subject TEXT NOT NULL, Body TEXT NULL, Status TEXT NOT NULL, DueAt TEXT NULL, CompletedAt TEXT NULL, OwnerUserId INTEGER NULL, AccountId INTEGER NULL, ContactId INTEGER NULL, LeadId INTEGER NULL, OpportunityId INTEGER NULL);",
        @"CREATE TABLE IF NOT EXISTS CrmNotes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Body TEXT NOT NULL, AuthorUserId INTEGER NULL, CreatedAt TEXT NOT NULL, AccountId INTEGER NULL, ContactId INTEGER NULL, LeadId INTEGER NULL, OpportunityId INTEGER NULL);",
        @"CREATE TABLE IF NOT EXISTS CrmCommunications (Id INTEGER PRIMARY KEY AUTOINCREMENT, Channel TEXT NOT NULL, Direction TEXT NOT NULL, Subject TEXT NULL, Summary TEXT NOT NULL, OccurredAt TEXT NOT NULL, UserId INTEGER NULL, AccountId INTEGER NULL, ContactId INTEGER NULL, LeadId INTEGER NULL, OpportunityId INTEGER NULL);"
    };
}
