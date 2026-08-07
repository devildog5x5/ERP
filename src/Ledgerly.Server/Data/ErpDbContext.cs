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
