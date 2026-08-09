using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Server.Data;

public class ErpDbContext : DbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options) { }

    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = null!;
    public DbSet<SalesOrder> SalesOrders { get; set; } = null!;
    public DbSet<SalesOrderLine> SalesOrderLines { get; set; } = null!;
    public DbSet<Reminder> Reminders { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;
    public DbSet<CompanySettings> Settings { get; set; } = null!;
    public DbSet<AppUser> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<AuthToken> AuthTokens { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<ProductLocation> ProductLocations { get; set; } = null!;
    public DbSet<StockTransfer> StockTransfers { get; set; } = null!;
    public DbSet<CycleCount> CycleCounts { get; set; } = null!;
    public DbSet<Bom> Boms { get; set; } = null!;
    public DbSet<BomLine> BomLines { get; set; } = null!;
    public DbSet<TaxCode> TaxCodes { get; set; } = null!;
    public DbSet<PriceList> PriceLists { get; set; } = null!;
    public DbSet<PriceListItem> PriceListItems { get; set; } = null!;
    public DbSet<SalesReturn> SalesReturns { get; set; } = null!;
    public DbSet<SalesReturnLine> SalesReturnLines { get; set; } = null!;
    public DbSet<CustomerPayment> CustomerPayments { get; set; } = null!;
    public DbSet<VendorBill> VendorBills { get; set; } = null!;
    public DbSet<VendorPayment> VendorPayments { get; set; } = null!;
    public DbSet<GlAccount> GlAccounts { get; set; } = null!;
    public DbSet<JournalEntry> JournalEntries { get; set; } = null!;
    public DbSet<JournalLine> JournalLines { get; set; } = null!;
    public DbSet<FiscalPeriod> FiscalPeriods { get; set; } = null!;
    public DbSet<BankAccount> BankAccounts { get; set; } = null!;
    public DbSet<BankTransaction> BankTransactions { get; set; } = null!;
    public DbSet<CurrencyRate> CurrencyRates { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<WebhookSubscription> Webhooks { get; set; } = null!;
    public DbSet<IntegrationLog> IntegrationLogs { get; set; } = null!;
    public DbSet<NumberSequence> NumberSequences { get; set; } = null!;
    public DbSet<CrmLead> CrmLeads { get; set; } = null!;
    public DbSet<CrmAccount> CrmAccounts { get; set; } = null!;
    public DbSet<CrmContact> CrmContacts { get; set; } = null!;
    public DbSet<CrmOpportunity> CrmOpportunities { get; set; } = null!;
    public DbSet<CrmActivity> CrmActivities { get; set; } = null!;
    public DbSet<CrmNote> CrmNotes { get; set; } = null!;
    public DbSet<CrmCommunicationLog> CrmCommunications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(p => p.Upc);
        modelBuilder.Entity<PurchaseOrder>().HasIndex(p => p.PoNumber).IsUnique();
        modelBuilder.Entity<SalesOrder>().HasIndex(s => s.OrderNumber).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(u => u.UserName).IsUnique();
        modelBuilder.Entity<Location>().HasIndex(l => l.Code).IsUnique();
        modelBuilder.Entity<ProductLocation>().HasIndex(x => new { x.ProductId, x.LocationId }).IsUnique();
        modelBuilder.Entity<TaxCode>().HasIndex(t => t.Code).IsUnique();
        modelBuilder.Entity<GlAccount>().HasIndex(a => a.AccountNumber).IsUnique();
        modelBuilder.Entity<CompanySettings>().ToTable("CompanySettings");
        modelBuilder.Entity<StockMovement>().ToTable("StockMovements");
        modelBuilder.Entity<AuthToken>().HasIndex(t => t.Token).IsUnique();
        modelBuilder.Entity<CrmCommunicationLog>().ToTable("CrmCommunications");

        // Generous string sizes for SQL Server (SQLite stores these as TEXT anyway).
        modelBuilder.Entity<Supplier>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(64);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.CurrencyCode).HasMaxLength(8);
        });
        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(64);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.CurrencyCode).HasMaxLength(8);
        });
        modelBuilder.Entity<Product>(e =>
        {
            e.Property(x => x.Sku).HasMaxLength(64);
            e.Property(x => x.Upc).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(300);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Unit).HasMaxLength(32);
            e.Property(x => x.CostingMethod).HasMaxLength(32);
        });
        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.Property(x => x.PoNumber).HasMaxLength(64);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.CurrencyCode).HasMaxLength(8);
        });
        modelBuilder.Entity<PurchaseOrderLine>(e =>
        {
            e.Property(x => x.LotNumber).HasMaxLength(64);
        });
        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.Property(x => x.OrderNumber).HasMaxLength(64);
            e.Property(x => x.DocumentType).HasMaxLength(32);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.CurrencyCode).HasMaxLength(8);
            e.Property(x => x.TrackingNumber).HasMaxLength(128);
            e.Property(x => x.Carrier).HasMaxLength(128);
        });
        modelBuilder.Entity<SalesOrderLine>(e =>
        {
            e.Property(x => x.LotNumber).HasMaxLength(64);
            e.Property(x => x.SerialNumber).HasMaxLength(128);
        });
        modelBuilder.Entity<Reminder>(e =>
        {
            e.Property(x => x.ReminderType).HasMaxLength(64);
            e.Property(x => x.Severity).HasMaxLength(32);
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.Message).HasMaxLength(4000);
            e.Property(x => x.RelatedEntityType).HasMaxLength(64);
        });
        modelBuilder.Entity<StockMovement>(e =>
        {
            e.Property(x => x.Reason).HasMaxLength(64);
            e.Property(x => x.ReasonCode).HasMaxLength(64);
            e.Property(x => x.ReferenceType).HasMaxLength(64);
            e.Property(x => x.LotNumber).HasMaxLength(64);
            e.Property(x => x.SerialNumber).HasMaxLength(128);
            e.Property(x => x.Notes).HasMaxLength(2000);
        });
        modelBuilder.Entity<CompanySettings>(e =>
        {
            e.Property(x => x.CompanyName).HasMaxLength(200);
            e.Property(x => x.Currency).HasMaxLength(8);
            e.Property(x => x.ReceiptFooter).HasMaxLength(2000);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Phone).HasMaxLength(64);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.SmtpHost).HasMaxLength(256);
            e.Property(x => x.SmtpUsername).HasMaxLength(256);
            e.Property(x => x.SmtpPassword).HasMaxLength(512);
            e.Property(x => x.SmtpFrom).HasMaxLength(256);
            e.Property(x => x.FiscalYearStart).HasMaxLength(16);
        });
        modelBuilder.Entity<AppUser>(e =>
        {
            e.Property(x => x.UserName).HasMaxLength(128);
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.PasswordHash).HasMaxLength(256);
            e.Property(x => x.PasswordSalt).HasMaxLength(256);
        });
        modelBuilder.Entity<Role>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Permissions).HasMaxLength(2000);
        });
        modelBuilder.Entity<AuthToken>(e =>
        {
            e.Property(x => x.Token).HasMaxLength(512);
        });
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.UserName).HasMaxLength(128);
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.EntityType).HasMaxLength(64);
            e.Property(x => x.Details).HasMaxLength(4000);
        });
        modelBuilder.Entity<Location>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Bin).HasMaxLength(64);
        });
    }
}

public static class Db
{
    public static DatabaseProvider Provider { get; private set; } = DatabaseProvider.Sqlite;
    public static string ConnectionString { get; private set; } = "Data Source=ledgerly.db";
    public static string ListenUrl { get; private set; } = "http://127.0.0.1:8000/";
    public static string ConfigPath { get; private set; } = ServerConfig.ConfigPath;

    public static void Configure(ServerConfig config)
    {
        Provider = config.Provider;
        ConnectionString = config.ConnectionString;
        ListenUrl = string.IsNullOrWhiteSpace(config.ListenUrl) ? "http://127.0.0.1:8000/" : config.ListenUrl;
        ConfigPath = ServerConfig.ConfigPath;
    }

    public static ErpDbContext Create() => Create(Provider, ConnectionString);

    public static ErpDbContext Create(DatabaseProvider provider, string connectionString)
    {
        var builder = new DbContextOptionsBuilder<ErpDbContext>();
        if (provider == DatabaseProvider.SqlServer)
            builder.UseSqlServer(connectionString);
        else
            builder.UseSqlite(connectionString);
        return new ErpDbContext(builder.Options);
    }
}
