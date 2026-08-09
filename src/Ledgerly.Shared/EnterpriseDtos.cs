using System;
using System.Collections.Generic;

namespace Ledgerly.Shared;

public class LoginRequestDto
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public string Permissions { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public bool IsActive { get; set; }
}

public class UserCreateDto
{
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Password { get; set; } = "";
    public int RoleId { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = "";
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Permissions { get; set; } = "";
}

public class AuditLogDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public int? EntityId { get; set; }
    public string? Details { get; set; }
}

public class LocationDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Bin { get; set; }
    public bool IsActive { get; set; }
}

public class ProductLocationDto
{
    public int ProductId { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductName { get; set; }
    public int LocationId { get; set; }
    public string? LocationCode { get; set; }
    public decimal Quantity { get; set; }
}

public class TransferCreateDto
{
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

public class CycleCountCreateDto
{
    public int LocationId { get; set; }
    public int ProductId { get; set; }
    public decimal CountedQty { get; set; }
    public string? ReasonCode { get; set; }
}

public class BomDto
{
    public int Id { get; set; }
    public int ParentProductId { get; set; }
    public string? ParentSku { get; set; }
    public string Name { get; set; } = "";
    public List<BomLineDto> Lines { get; set; } = new();
}

public class BomLineDto
{
    public int ComponentProductId { get; set; }
    public string? ComponentSku { get; set; }
    public decimal Quantity { get; set; }
}

public class BomBuildDto
{
    public int BomId { get; set; }
    public decimal Quantity { get; set; }
    public int? LocationId { get; set; }
}

public class TaxCodeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }
    public bool IsActive { get; set; }
}

public class PriceListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public List<PriceListItemDto> Items { get; set; } = new();
}

public class PriceListItemDto
{
    public int ProductId { get; set; }
    public string? ProductSku { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? MinQuantity { get; set; }
}

public class SalesReturnCreateDto
{
    public int CustomerId { get; set; }
    public int? SalesOrderId { get; set; }
    public string? Notes { get; set; }
    public List<SalesReturnLineDto> Lines { get; set; } = new();
}

public class SalesReturnLineDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SalesReturnDto
{
    public int Id { get; set; }
    public string RmaNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public string Status { get; set; } = "";
    public DateTime ReturnDate { get; set; }
    public decimal Total { get; set; }
}

public class PaymentCreateDto
{
    public int CustomerId { get; set; }
    public int? SalesOrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "cash";
    public string? Reference { get; set; }
    public int? BankAccountId { get; set; }
}

public class VendorBillCreateDto
{
    public int SupplierId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public class VendorBillDto
{
    public int Id { get; set; }
    public string BillNumber { get; set; } = "";
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = "";
    public DateTime BillDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public class VendorPaymentCreateDto
{
    public int SupplierId { get; set; }
    public int? VendorBillId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "check";
    public string? Reference { get; set; }
    public int? BankAccountId { get; set; }
}

public class GlAccountDto
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string AccountType { get; set; } = "";
    public bool IsActive { get; set; }
}

public class JournalEntryDto
{
    public int Id { get; set; }
    public string EntryNumber { get; set; } = "";
    public DateTime EntryDate { get; set; }
    public string Memo { get; set; } = "";
    public bool IsPosted { get; set; }
    public List<JournalLineDto> Lines { get; set; } = new();
}

public class JournalLineDto
{
    public string AccountNumber { get; set; } = "";
    public string? AccountName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class FiscalPeriodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
}

public class BankAccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; }
}

public class BankTransactionDto
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public DateTime TxnDate { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public bool IsReconciled { get; set; }
}

public class CurrencyRateDto
{
    public int Id { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal RateToBase { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class CompanyDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string BaseCurrency { get; set; } = "";
}

public class AgingRowDto
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = "";
    public decimal Current { get; set; }
    public decimal Days30 { get; set; }
    public decimal Days60 { get; set; }
    public decimal Days90Plus { get; set; }
    public decimal Total { get; set; }
}

public class ReportSummaryDto
{
    public decimal InventoryValue { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public decimal SalesMonthToDate { get; set; }
    public decimal CogsMonthToDate { get; set; }
    public int DeadStockCount { get; set; }
    public decimal ArTotal { get; set; }
    public decimal ApTotal { get; set; }
    public List<ProductDto> DeadStock { get; set; } = new();
    public List<AgingRowDto> ArAging { get; set; } = new();
    public List<AgingRowDto> ApAging { get; set; } = new();
}

public class DocumentHtmlDto
{
    public string Title { get; set; } = "";
    public string Html { get; set; } = "";
}

public class WebhookDto
{
    public int Id { get; set; }
    public string EventName { get; set; } = "";
    public string TargetUrl { get; set; } = "";
    public bool IsActive { get; set; }
}

public class ApiKeyCreatedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string KeyPrefix { get; set; } = "";
}

public class IntegrationLogDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string SystemName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Details { get; set; }
}

public class BackupResultDto
{
    public string Path { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Provider { get; set; } = "";
}

public class DatabaseRefreshDto
{
    /// <summary>Must be exactly "REFRESH DATABASE".</summary>
    public string Confirmation { get; set; } = "";
}

public class DatabaseRefreshResultDto
{
    public bool Refreshed { get; set; }
    public string? BackupPath { get; set; }
    public string Message { get; set; } = "";
}

public class ShipOrderDto
{
    public List<ShipLineDto> Lines { get; set; } = new();
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
}

public class ShipLineDto
{
    public int LineId { get; set; }
    public decimal Quantity { get; set; }
}

public class QuoteConvertDto
{
    public bool CreateInvoice { get; set; }
}
