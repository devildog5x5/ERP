using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Server.Data;

/// <summary>
/// Copies a live Coalesce database onto another provider while preserving IDs
/// so relationships stay intact. Switches server.json when requested.
/// </summary>
public static class PlatformMigrator
{
    public static MigrationResult MigrateToSqlServer(string sqlServerConnectionString, bool switchConfig = true) =>
        MigrateTo(DatabaseProvider.SqlServer, sqlServerConnectionString, switchConfig);

    public static MigrationResult MigrateTo(
        DatabaseProvider targetProvider,
        string connectionString,
        bool switchConfig = true)
    {
        if (targetProvider == DatabaseProvider.Sqlite)
            throw new ArgumentException("Migrate targets a server database (SqlServer, MySql, or PostgreSql).", nameof(targetProvider));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        var sourceProvider = Db.Provider;
        var sourceCs = Db.ConnectionString;
        var targetCs = connectionString.Trim();

        using var source = Db.Create(sourceProvider, sourceCs);
        if (sourceProvider == DatabaseProvider.Sqlite)
            SchemaMigrator.Apply(source);
        else
            source.Database.EnsureCreated();

        using var dest = Db.Create(targetProvider, targetCs);
        dest.Database.EnsureCreated();
        EnsureTargetEmpty(dest, targetProvider);

        var existingSettings = dest.Settings.ToList();
        if (existingSettings.Count > 0 && !dest.Products.Any())
        {
            dest.Settings.RemoveRange(existingSettings);
            dest.SaveChanges();
        }

        CopyAll(source, dest, targetProvider);

        if (switchConfig)
            SwitchConfig(targetProvider, targetCs);

        return new MigrationResult
        {
            SourceProvider = sourceProvider.ToString(),
            TargetProvider = targetProvider.ToString(),
            ConfigUpdated = switchConfig,
            ConfigPath = ServerConfig.ConfigPath,
            Counts = CountSummary(dest)
        };
    }

    /// <summary>Create schema on an empty target and switch server.json (no data copy).</summary>
    public static MigrationResult PointToEmpty(
        DatabaseProvider targetProvider,
        string connectionString,
        bool switchConfig = true)
    {
        if (targetProvider == DatabaseProvider.Sqlite)
            throw new ArgumentException("Grow targets a server database (SqlServer, MySql, or PostgreSql).", nameof(targetProvider));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        var sourceProvider = Db.Provider.ToString();
        var targetCs = connectionString.Trim();
        using (var dest = Db.Create(targetProvider, targetCs))
        {
            dest.Database.EnsureCreated();
            EnsureTargetEmpty(dest, targetProvider);
        }

        if (switchConfig)
            SwitchConfig(targetProvider, targetCs);

        // Seed system defaults on the new empty store.
        DbSeeder.Seed(includeDemoData: false);

        using var verify = Db.Create();
        return new MigrationResult
        {
            SourceProvider = sourceProvider,
            TargetProvider = targetProvider.ToString(),
            ConfigUpdated = switchConfig,
            ConfigPath = ServerConfig.ConfigPath,
            Counts = CountSummary(verify)
        };
    }

    private static void EnsureTargetEmpty(ErpDbContext dest, DatabaseProvider targetProvider)
    {
        if (dest.Products.Any() || dest.Suppliers.Any() || dest.Customers.Any() || dest.Users.Any())
            throw new InvalidOperationException(
                $"Target {targetProvider} database already has Coalesce data. Create/use an empty database, then retry.");
    }

    private static void SwitchConfig(DatabaseProvider targetProvider, string targetCs)
    {
        var cfg = ServerConfig.LoadOrCreate();
        cfg.Provider = targetProvider;
        cfg.ConnectionString = targetCs;
        cfg.Save();
        Db.Configure(cfg);
    }

    public static void CopyAll(ErpDbContext source, ErpDbContext dest, DatabaseProvider destProvider)
    {
        CopyTable(dest, destProvider, "Roles",
            source.Roles.AsNoTracking().ToList(),
            rows => dest.Roles.AddRange(rows.Select(r => new Role
            {
                Id = r.Id, Name = r.Name, Permissions = r.Permissions
            })));

        CopyTable(dest, destProvider, "Users",
            source.Users.AsNoTracking().ToList(),
            rows => dest.Users.AddRange(rows.Select(u => new AppUser
            {
                Id = u.Id, UserName = u.UserName, DisplayName = u.DisplayName,
                PasswordHash = u.PasswordHash, PasswordSalt = u.PasswordSalt,
                RoleId = u.RoleId, IsActive = u.IsActive, CreatedAt = u.CreatedAt
            })));

        CopyTable(dest, destProvider, "CompanySettings",
            source.Settings.AsNoTracking().ToList(),
            rows => dest.Settings.AddRange(CloneSettings(rows)));

        CopyTable(dest, destProvider, "Locations",
            source.Locations.AsNoTracking().ToList(),
            rows => dest.Locations.AddRange(rows.Select(l => new Location
            {
                Id = l.Id, Code = l.Code, Name = l.Name, Bin = l.Bin, IsActive = l.IsActive
            })));

        CopyTable(dest, destProvider, "TaxCodes",
            source.TaxCodes.AsNoTracking().ToList(),
            rows => dest.TaxCodes.AddRange(rows.Select(t => new TaxCode
            {
                Id = t.Id, Code = t.Code, Name = t.Name, Rate = t.Rate, IsActive = t.IsActive
            })));

        CopyTable(dest, destProvider, "Suppliers",
            source.Suppliers.AsNoTracking().ToList(),
            rows => dest.Suppliers.AddRange(rows.Select(CloneSupplier)));

        CopyTable(dest, destProvider, "Customers",
            source.Customers.AsNoTracking().ToList(),
            rows => dest.Customers.AddRange(rows.Select(CloneCustomer)));

        CopyTable(dest, destProvider, "Products",
            source.Products.AsNoTracking().ToList(),
            rows => dest.Products.AddRange(rows.Select(CloneProduct)));

        CopyTable(dest, destProvider, "PurchaseOrders",
            source.PurchaseOrders.AsNoTracking().ToList(),
            rows => dest.PurchaseOrders.AddRange(rows.Select(ClonePo)));

        CopyTable(dest, destProvider, "PurchaseOrderLines",
            source.PurchaseOrderLines.AsNoTracking().ToList(),
            rows => dest.PurchaseOrderLines.AddRange(rows.Select(ClonePoLine)));

        CopyTable(dest, destProvider, "SalesOrders",
            source.SalesOrders.AsNoTracking().ToList(),
            rows => dest.SalesOrders.AddRange(rows.Select(CloneSo)));

        CopyTable(dest, destProvider, "SalesOrderLines",
            source.SalesOrderLines.AsNoTracking().ToList(),
            rows => dest.SalesOrderLines.AddRange(rows.Select(CloneSoLine)));

        CopyTable(dest, destProvider, "Reminders",
            source.Reminders.AsNoTracking().ToList(),
            rows => dest.Reminders.AddRange(rows.Select(CloneReminder)));

        CopyTable(dest, destProvider, "StockMovements",
            source.StockMovements.AsNoTracking().ToList(),
            rows => dest.StockMovements.AddRange(rows.Select(CloneMovement)));

        CopyTable(dest, destProvider, "CrmLeads",
            source.CrmLeads.AsNoTracking().ToList(),
            rows => dest.CrmLeads.AddRange(rows.Select(CloneLead)));

        CopyTable(dest, destProvider, "CrmAccounts",
            source.CrmAccounts.AsNoTracking().ToList(),
            rows => dest.CrmAccounts.AddRange(rows.Select(CloneCrmAccount)));

        CopyTable(dest, destProvider, "CrmContacts",
            source.CrmContacts.AsNoTracking().ToList(),
            rows => dest.CrmContacts.AddRange(rows.Select(CloneCrmContact)));

        CopyTable(dest, destProvider, "CrmOpportunities",
            source.CrmOpportunities.AsNoTracking().ToList(),
            rows => dest.CrmOpportunities.AddRange(rows.Select(CloneOpportunity)));

        CopyTable(dest, destProvider, "CrmActivities",
            source.CrmActivities.AsNoTracking().ToList(),
            rows => dest.CrmActivities.AddRange(rows.Select(CloneActivity)));

        CopyTable(dest, destProvider, "CrmNotes",
            source.CrmNotes.AsNoTracking().ToList(),
            rows => dest.CrmNotes.AddRange(rows.Select(CloneNote)));

        if (destProvider == DatabaseProvider.PostgreSql)
            ResetPostgresSequences(dest);
    }

    private static void CopyTable<T>(
        ErpDbContext dest,
        DatabaseProvider destProvider,
        string table,
        List<T> rows,
        Action<List<T>> add) where T : class
    {
        if (rows.Count == 0) return;

        var identityInsert = destProvider == DatabaseProvider.SqlServer;
        if (identityInsert)
            dest.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT [{table}] ON");
        try
        {
            add(rows);
            dest.SaveChanges();
            foreach (var entry in dest.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;
        }
        finally
        {
            if (identityInsert)
                dest.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT [{table}] OFF");
        }
    }

    private static void ResetPostgresSequences(ErpDbContext dest)
    {
        foreach (var table in new[]
                 {
                     "Roles", "Users", "CompanySettings", "Locations", "TaxCodes",
                     "Suppliers", "Customers", "Products",
                     "PurchaseOrders", "PurchaseOrderLines", "SalesOrders", "SalesOrderLines",
                     "Reminders", "StockMovements",
                     "CrmLeads", "CrmAccounts", "CrmContacts", "CrmOpportunities", "CrmActivities", "CrmNotes"
                 })
        {
            try
            {
                dest.Database.ExecuteSqlRaw(
                    $@"SELECT setval(pg_get_serial_sequence('""{table}""', 'Id'), COALESCE((SELECT MAX(""Id"") FROM ""{table}""), 1));");
            }
            catch
            {
                // Sequence name may differ; next insert will still work for empty tables.
            }
        }
    }

    private static List<CompanySettings> CloneSettings(List<CompanySettings> rows) =>
        rows.Select(s => new CompanySettings
        {
            Id = s.Id,
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
            SmtpPassword = s.SmtpPassword,
            SmtpEnableSsl = s.SmtpEnableSsl,
            SmtpFrom = s.SmtpFrom,
            PoApprovalThreshold = s.PoApprovalThreshold,
            RequireLogin = s.RequireLogin,
            DefaultLocationId = s.DefaultLocationId,
            FiscalYearStart = s.FiscalYearStart
        }).ToList();

    private static Supplier CloneSupplier(Supplier s) => new()
    {
        Id = s.Id, Name = s.Name, Email = s.Email, Phone = s.Phone, Address = s.Address, IsActive = s.IsActive
    };

    private static Customer CloneCustomer(Customer c) => new()
    {
        Id = c.Id, Name = c.Name, Email = c.Email, Phone = c.Phone, Address = c.Address, IsActive = c.IsActive
    };

    private static Product CloneProduct(Product p) => new()
    {
        Id = p.Id, Sku = p.Sku, Upc = p.Upc, Name = p.Name, Description = p.Description, Category = p.Category,
        Unit = p.Unit, QuantityOnHand = p.QuantityOnHand, ReorderPoint = p.ReorderPoint,
        ReorderQuantity = p.ReorderQuantity, UnitCost = p.UnitCost, SellPrice = p.SellPrice,
        SupplierId = p.SupplierId, IsActive = p.IsActive
    };

    private static PurchaseOrder ClonePo(PurchaseOrder p) => new()
    {
        Id = p.Id, PoNumber = p.PoNumber, SupplierId = p.SupplierId, Status = p.Status,
        OrderDate = p.OrderDate, ExpectedDate = p.ExpectedDate, ReceivedDate = p.ReceivedDate,
        Notes = p.Notes, Total = p.Total
    };

    private static PurchaseOrderLine ClonePoLine(PurchaseOrderLine l) => new()
    {
        Id = l.Id, PurchaseOrderId = l.PurchaseOrderId, ProductId = l.ProductId,
        QuantityOrdered = l.QuantityOrdered, QuantityReceived = l.QuantityReceived, UnitCost = l.UnitCost
    };

    private static SalesOrder CloneSo(SalesOrder s) => new()
    {
        Id = s.Id, OrderNumber = s.OrderNumber, CustomerId = s.CustomerId, Status = s.Status,
        OrderDate = s.OrderDate, Notes = s.Notes, Subtotal = s.Subtotal, TaxRate = s.TaxRate,
        TaxAmount = s.TaxAmount, Total = s.Total
    };

    private static SalesOrderLine CloneSoLine(SalesOrderLine l) => new()
    {
        Id = l.Id, SalesOrderId = l.SalesOrderId, ProductId = l.ProductId,
        Quantity = l.Quantity, UnitPrice = l.UnitPrice
    };

    private static Reminder CloneReminder(Reminder r) => new()
    {
        Id = r.Id, ReminderType = r.ReminderType, Severity = r.Severity, Title = r.Title,
        Message = r.Message, ProductId = r.ProductId, RelatedEntityType = r.RelatedEntityType,
        RelatedEntityId = r.RelatedEntityId, IsRead = r.IsRead, IsResolved = r.IsResolved,
        EmailSent = r.EmailSent, CreatedAt = r.CreatedAt
    };

    private static StockMovement CloneMovement(StockMovement m) => new()
    {
        Id = m.Id, ProductId = m.ProductId, QuantityDelta = m.QuantityDelta, QuantityAfter = m.QuantityAfter,
        Reason = m.Reason, ReferenceType = m.ReferenceType, ReferenceId = m.ReferenceId,
        Notes = m.Notes, CreatedAt = m.CreatedAt
    };

    private static CrmLead CloneLead(CrmLead e) => new()
    {
        Id = e.Id, Name = e.Name, CompanyName = e.CompanyName, Email = e.Email, Phone = e.Phone,
        Source = e.Source, Status = e.Status, OwnerUserId = e.OwnerUserId, CreatedAt = e.CreatedAt,
        ConvertedAccountId = e.ConvertedAccountId, ConvertedCustomerId = e.ConvertedCustomerId
    };

    private static CrmAccount CloneCrmAccount(CrmAccount e) => new()
    {
        Id = e.Id, Name = e.Name, CustomerId = e.CustomerId, Industry = e.Industry, Website = e.Website,
        BillingEmail = e.BillingEmail, IsActive = e.IsActive, OwnerUserId = e.OwnerUserId, CreatedAt = e.CreatedAt
    };

    private static CrmContact CloneCrmContact(CrmContact e) => new()
    {
        Id = e.Id, AccountId = e.AccountId, LeadId = e.LeadId, FirstName = e.FirstName, LastName = e.LastName,
        Email = e.Email, Phone = e.Phone, Title = e.Title, IsPrimary = e.IsPrimary, IsActive = e.IsActive
    };

    private static CrmOpportunity CloneOpportunity(CrmOpportunity e) => new()
    {
        Id = e.Id, AccountId = e.AccountId, PrimaryContactId = e.PrimaryContactId, Name = e.Name, Stage = e.Stage,
        Amount = e.Amount, ExpectedClose = e.ExpectedClose, OwnerUserId = e.OwnerUserId,
        SalesOrderId = e.SalesOrderId, LostReason = e.LostReason, CreatedAt = e.CreatedAt
    };

    private static CrmActivity CloneActivity(CrmActivity e) => new()
    {
        Id = e.Id, ActivityType = e.ActivityType, Subject = e.Subject, Body = e.Body, Status = e.Status,
        DueAt = e.DueAt, CompletedAt = e.CompletedAt, OwnerUserId = e.OwnerUserId,
        AccountId = e.AccountId, ContactId = e.ContactId, LeadId = e.LeadId, OpportunityId = e.OpportunityId
    };

    private static CrmNote CloneNote(CrmNote e) => new()
    {
        Id = e.Id, Body = e.Body, AuthorUserId = e.AuthorUserId, CreatedAt = e.CreatedAt,
        AccountId = e.AccountId, ContactId = e.ContactId, LeadId = e.LeadId, OpportunityId = e.OpportunityId
    };

    private static Dictionary<string, int> CountSummary(ErpDbContext db) => new()
    {
        ["users"] = db.Users.Count(),
        ["suppliers"] = db.Suppliers.Count(),
        ["customers"] = db.Customers.Count(),
        ["products"] = db.Products.Count(),
        ["purchaseOrders"] = db.PurchaseOrders.Count(),
        ["salesOrders"] = db.SalesOrders.Count(),
        ["crmLeads"] = SafeCount(() => db.CrmLeads.Count()),
        ["crmAccounts"] = SafeCount(() => db.CrmAccounts.Count()),
        ["reminders"] = db.Reminders.Count(),
        ["stockMovements"] = db.StockMovements.Count()
    };

    private static int SafeCount(Func<int> count)
    {
        try { return count(); }
        catch { return 0; }
    }
}

public sealed class MigrationResult
{
    public string SourceProvider { get; set; } = "";
    public string TargetProvider { get; set; } = "";
    public bool ConfigUpdated { get; set; }
    public string ConfigPath { get; set; } = "";
    public Dictionary<string, int> Counts { get; set; } = new();
}
