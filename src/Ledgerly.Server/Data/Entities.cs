using System;
using System.Collections.Generic;

namespace Ledgerly.Server.Data;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CurrencyCode { get; set; }
    public int? PaymentTermsDays { get; set; } = 30;
    public List<Product> Products { get; set; } = new();
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public bool TaxExempt { get; set; }
    public int? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
    public string? CurrencyCode { get; set; }
    public int? PaymentTermsDays { get; set; } = 30;
    public decimal CreditLimit { get; set; }
    public List<SalesOrder> SalesOrders { get; set; } = new();
}

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string? Upc { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "ea";
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderPoint { get; set; } = 10;
    public decimal ReorderQuantity { get; set; } = 25;
    public decimal UnitCost { get; set; }
    public decimal AverageCost { get; set; }
    public decimal SellPrice { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int? TaxCodeId { get; set; }
    public TaxCode? TaxCode { get; set; }
    public string CostingMethod { get; set; } = "average"; // average | fifo
    public bool TrackLots { get; set; }
    public bool TrackSerials { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsKit { get; set; }
}

public class PurchaseOrder
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = "";
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public string Status { get; set; } = "ordered"; // draft|pending_approval|ordered|partial|received|cancelled
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime? ExpectedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? Notes { get; set; }
    public decimal Total { get; set; }
    public decimal LandedCost { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1;
    public int? LocationId { get; set; }
    public bool ApprovalRequired { get; set; }
    public bool IsApproved { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = new();
}

public class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public string? LotNumber { get; set; }
}

public class SalesOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string DocumentType { get; set; } = "order"; // quote|order|invoice
    public string Status { get; set; } = "fulfilled"; // quote|confirmed|partial|fulfilled|invoiced|cancelled|returned
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime? ShipDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1;
    public int? LocationId { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public int? ConvertedFromQuoteId { get; set; }
    public List<SalesOrderLine> Lines { get; set; } = new();
}

public class SalesOrderLine
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal QuantityShipped { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
}

public class Reminder
{
    public int Id { get; set; }
    public string ReminderType { get; set; } = "";
    public string Severity { get; set; } = "warning";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public int? ProductId { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public bool IsResolved { get; set; }
    public bool EmailSent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? LocationId { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal QuantityAfter { get; set; }
    public string Reason { get; set; } = "";
    public string? ReasonCode { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CompanySettings
{
    public int Id { get; set; } = 1;
    public string CompanyName { get; set; } = "Coalesce";
    public decimal DefaultTaxRate { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ReceiptFooter { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpEnableSsl { get; set; } = true;
    public string? SmtpFrom { get; set; }
    public decimal PoApprovalThreshold { get; set; } = 1000;
    public bool RequireLogin { get; set; } = true;
    public int? DefaultLocationId { get; set; }
    public string? FiscalYearStart { get; set; } = "01-01";
}

// --- Auth / audit ---
public class AppUser
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Permissions { get; set; } = ""; // comma-separated
    public List<AppUser> Users { get; set; } = new();
}

public class AuthToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public int? EntityId { get; set; }
    public string? Details { get; set; }
}

// --- Inventory advanced ---
public class Location
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Bin { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductLocation
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public decimal Quantity { get; set; }
}

public class StockTransfer
{
    public int Id { get; set; }
    public string TransferNumber { get; set; } = "";
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = "completed";
    public DateTime TransferDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public int? UserId { get; set; }
}

public class CycleCount
{
    public int Id { get; set; }
    public string CountNumber { get; set; } = "";
    public int LocationId { get; set; }
    public int ProductId { get; set; }
    public decimal SystemQty { get; set; }
    public decimal CountedQty { get; set; }
    public string? ReasonCode { get; set; }
    public string Status { get; set; } = "posted";
    public DateTime CountDate { get; set; } = DateTime.Today;
    public int? UserId { get; set; }
}

public class Bom
{
    public int Id { get; set; }
    public int ParentProductId { get; set; }
    public Product ParentProduct { get; set; } = null!;
    public string Name { get; set; } = "";
    public List<BomLine> Lines { get; set; } = new();
}

public class BomLine
{
    public int Id { get; set; }
    public int BomId { get; set; }
    public Bom Bom { get; set; } = null!;
    public int ComponentProductId { get; set; }
    public Product ComponentProduct { get; set; } = null!;
    public decimal Quantity { get; set; }
}

// --- Tax / pricing ---
public class TaxCode
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PriceList
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<PriceListItem> Items { get; set; } = new();
}

public class PriceListItem
{
    public int Id { get; set; }
    public int PriceListId { get; set; }
    public PriceList PriceList { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public decimal? MinQuantity { get; set; }
}

// --- Returns / AR / AP ---
public class SalesReturn
{
    public int Id { get; set; }
    public string RmaNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public int? SalesOrderId { get; set; }
    public string Status { get; set; } = "received";
    public DateTime ReturnDate { get; set; } = DateTime.Today;
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public List<SalesReturnLine> Lines { get; set; } = new();
}

public class SalesReturnLine
{
    public int Id { get; set; }
    public int SalesReturnId { get; set; }
    public SalesReturn SalesReturn { get; set; } = null!;
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CustomerPayment
{
    public int Id { get; set; }
    public string PaymentNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public int? SalesOrderId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "cash";
    public string? Reference { get; set; }
    public int? BankAccountId { get; set; }
}

public class VendorBill
{
    public int Id { get; set; }
    public string BillNumber { get; set; } = "";
    public int SupplierId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public DateTime BillDate { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = "open";
    public string? Notes { get; set; }
}

public class VendorPayment
{
    public int Id { get; set; }
    public string PaymentNumber { get; set; } = "";
    public int SupplierId { get; set; }
    public int? VendorBillId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "check";
    public string? Reference { get; set; }
    public int? BankAccountId { get; set; }
}

// --- Finance ---
public class GlAccount
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string AccountType { get; set; } = ""; // asset|liability|equity|revenue|expense
    public bool IsActive { get; set; } = true;
}

public class JournalEntry
{
    public int Id { get; set; }
    public string EntryNumber { get; set; } = "";
    public DateTime EntryDate { get; set; } = DateTime.Today;
    public string Memo { get; set; } = "";
    public string? SourceType { get; set; }
    public int? SourceId { get; set; }
    public bool IsPosted { get; set; } = true;
    public List<JournalLine> Lines { get; set; } = new();
}

public class JournalLine
{
    public int Id { get; set; }
    public int JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    public int GlAccountId { get; set; }
    public GlAccount GlAccount { get; set; } = null!;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
}

public class FiscalPeriod
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
}

public class BankAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string CurrencyCode { get; set; } = "USD";
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BankTransaction
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;
    public DateTime TxnDate { get; set; } = DateTime.Today;
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public bool IsReconciled { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
}

public class CurrencyRate
{
    public int Id { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal RateToBase { get; set; } = 1;
    public DateTime EffectiveDate { get; set; } = DateTime.Today;
}

public class Company
{
    public int Id { get; set; }
    public string Code { get; set; } = "MAIN";
    public string Name { get; set; } = "Main Company";
    public string BaseCurrency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
}

// --- Integrations ---
public class ApiKey
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string KeyHash { get; set; } = "";
    public string KeyPrefix { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class WebhookSubscription
{
    public int Id { get; set; }
    public string EventName { get; set; } = "";
    public string TargetUrl { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class IntegrationLog
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string SystemName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Details { get; set; }
}

public class NumberSequence
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = "";
    public string Prefix { get; set; } = "";
    public int NextValue { get; set; } = 1;
}

// --- CRM (complements ERP Customers / SalesOrders) ---

public class CrmLead
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = "new"; // new|working|qualified|disqualified|converted
    public int? OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? ConvertedAccountId { get; set; }
    public int? ConvertedCustomerId { get; set; }
}

public class CrmAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? BillingEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public int? OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CrmContact
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public CrmAccount? Account { get; set; }
    public int? LeadId { get; set; }
    public string FirstName { get; set; } = "";
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CrmOpportunity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public CrmAccount Account { get; set; } = null!;
    public int? PrimaryContactId { get; set; }
    public string Name { get; set; } = "";
    public string Stage { get; set; } = "prospecting"; // prospecting|qualified|proposal|negotiation|won|lost
    public decimal? Amount { get; set; }
    public DateTime? ExpectedClose { get; set; }
    public int? OwnerUserId { get; set; }
    public int? SalesOrderId { get; set; }
    public string? LostReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CrmActivity
{
    public int Id { get; set; }
    public string ActivityType { get; set; } = "task"; // call|meeting|email|task
    public string Subject { get; set; } = "";
    public string? Body { get; set; }
    public string Status { get; set; } = "open"; // open|done|cancelled
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? OwnerUserId { get; set; }
    public int? AccountId { get; set; }
    public int? ContactId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }
}

public class CrmNote
{
    public int Id { get; set; }
    public string Body { get; set; } = "";
    public int? AuthorUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? AccountId { get; set; }
    public int? ContactId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }
}

public class CrmCommunicationLog
{
    public int Id { get; set; }
    public string Channel { get; set; } = "other"; // phone|email|meeting|other
    public string Direction { get; set; } = "outbound"; // inbound|outbound
    public string? Subject { get; set; }
    public string Summary { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    public int? AccountId { get; set; }
    public int? ContactId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }
}
