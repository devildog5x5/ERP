using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ledgerly.Shared;
using Newtonsoft.Json;

namespace Ledgerly.Client.Services;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly JsonSerializerSettings _json = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public ApiClient(string baseAddress = "http://127.0.0.1:8000/")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(30) };
    }

    public string BaseAddress => _http.BaseAddress?.ToString() ?? "";

    public void SetBaseAddress(string baseAddress)
    {
        _http.BaseAddress = new Uri(baseAddress.EndsWith("/") ? baseAddress : baseAddress + "/");
    }

    public void SetAuthToken(string? token)
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + token);
    }

    public Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto) => PostAsync<LoginResponseDto>("api/auth/login", dto);
    public Task<List<UserDto>?> GetUsersAsync() => GetAsync<List<UserDto>>("api/users");
    public Task<UserDto?> CreateUserAsync(UserCreateDto dto) => PostAsync<UserDto>("api/users", dto);
    public Task<List<RoleDto>?> GetRolesAsync() => GetAsync<List<RoleDto>>("api/roles");
    public Task<List<AuditLogDto>?> GetAuditLogsAsync() => GetAsync<List<AuditLogDto>>("api/audit-logs");
    public Task<List<LocationDto>?> GetLocationsAsync() => GetAsync<List<LocationDto>>("api/locations");
    public Task<LocationDto?> CreateLocationAsync(LocationDto dto) => PostAsync<LocationDto>("api/locations", dto);
    public Task<List<ProductLocationDto>?> GetProductLocationsAsync() => GetAsync<List<ProductLocationDto>>("api/product-locations");
    public Task TransferAsync(TransferCreateDto dto) => PostAsync<object>("api/transfers", dto);
    public Task CycleCountAsync(CycleCountCreateDto dto) => PostAsync<object>("api/cycle-counts", dto);
    public Task<List<BomDto>?> GetBomsAsync() => GetAsync<List<BomDto>>("api/boms");
    public Task CreateBomAsync(BomDto dto) => PostAsync<object>("api/boms", dto);
    public Task BuildBomAsync(BomBuildDto dto) => PostAsync<object>("api/boms/build", dto);
    public Task<List<TaxCodeDto>?> GetTaxCodesAsync() => GetAsync<List<TaxCodeDto>>("api/tax-codes");
    public Task<List<PriceListDto>?> GetPriceListsAsync() => GetAsync<List<PriceListDto>>("api/price-lists");
    public Task<List<SalesReturnDto>?> GetSalesReturnsAsync() => GetAsync<List<SalesReturnDto>>("api/sales-returns");
    public Task<SalesReturnDto?> CreateSalesReturnAsync(SalesReturnCreateDto dto) => PostAsync<SalesReturnDto>("api/sales-returns", dto);
    public Task CustomerPaymentAsync(PaymentCreateDto dto) => PostAsync<object>("api/customer-payments", dto);
    public Task<List<VendorBillDto>?> GetVendorBillsAsync() => GetAsync<List<VendorBillDto>>("api/vendor-bills");
    public Task CreateVendorBillAsync(VendorBillCreateDto dto) => PostAsync<object>("api/vendor-bills", dto);
    public Task VendorPaymentAsync(VendorPaymentCreateDto dto) => PostAsync<object>("api/vendor-payments", dto);
    public Task<List<GlAccountDto>?> GetGlAccountsAsync() => GetAsync<List<GlAccountDto>>("api/gl-accounts");
    public Task<List<JournalEntryDto>?> GetJournalsAsync() => GetAsync<List<JournalEntryDto>>("api/journal-entries");
    public Task<List<FiscalPeriodDto>?> GetFiscalPeriodsAsync() => GetAsync<List<FiscalPeriodDto>>("api/fiscal-periods");
    public Task ClosePeriodAsync(int id) => PostAsync<object>($"api/fiscal-periods/{id}/close", new { });
    public Task<List<BankAccountDto>?> GetBankAccountsAsync() => GetAsync<List<BankAccountDto>>("api/bank-accounts");
    public Task<List<BankTransactionDto>?> GetBankTransactionsAsync() => GetAsync<List<BankTransactionDto>>("api/bank-transactions");
    public Task ReconcileBankAsync(int id) => PostAsync<object>($"api/bank-transactions/{id}/reconcile", new { });
    public Task<List<CurrencyRateDto>?> GetCurrenciesAsync() => GetAsync<List<CurrencyRateDto>>("api/currencies");
    public Task<List<CompanyDto>?> GetCompaniesAsync() => GetAsync<List<CompanyDto>>("api/companies");
    public Task ApprovePoAsync(int id) => PostAsync<object>($"api/purchase-orders/{id}/approve", new { });
    public Task ConvertQuoteAsync(int id, bool invoice) => PostAsync<object>($"api/sales-orders/{id}/convert-quote", new QuoteConvertDto { CreateInvoice = invoice });
    public Task ShipOrderAsync(int id, ShipOrderDto dto) => PostAsync<object>($"api/sales-orders/{id}/ship", dto);
    public Task<DocumentHtmlDto?> GetSalesDocumentAsync(int id) => GetAsync<DocumentHtmlDto>($"api/documents/sales-orders/{id}");
    public Task<DocumentHtmlDto?> GetPurchaseDocumentAsync(int id) => GetAsync<DocumentHtmlDto>($"api/documents/purchase-orders/{id}");
    public Task EmailSalesDocumentAsync(int id) => PostAsync<object>($"api/documents/sales-orders/{id}/email", new { });
    public Task<ReportSummaryDto?> GetReportSummaryAsync() => GetAsync<ReportSummaryDto>("api/reports/summary");
    public Task<List<WebhookDto>?> GetWebhooksAsync() => GetAsync<List<WebhookDto>>("api/webhooks");
    public Task CreateWebhookAsync(WebhookDto dto) => PostAsync<object>("api/webhooks", dto);
    public Task<ApiKeyCreatedDto?> CreateApiKeyAsync(string name) => PostAsync<ApiKeyCreatedDto>("api/api-keys", new UserCreateDto { UserName = name, Password = "n/a", RoleId = 1 });
    public Task<List<IntegrationLogDto>?> GetIntegrationLogsAsync() => GetAsync<List<IntegrationLogDto>>("api/integration-logs");
    public Task ShopifySyncAsync() => PostAsync<object>("api/integrations/shopify/sync", new { });
    public Task<Newtonsoft.Json.Linq.JObject?> AccountingExportAsync() => GetAsync<Newtonsoft.Json.Linq.JObject>("api/integrations/accounting-export");
    public Task<BackupResultDto?> BackupAsync() => PostAsync<BackupResultDto>("api/backup", new { });
    public Task<List<BackupResultDto>?> ListBackupsAsync() => GetAsync<List<BackupResultDto>>("api/backup/list");
    public Task RestoreBackupAsync(string path) => PostAsync<object>("api/backup/restore", new BackupResultDto { Path = path });


    public Task<HealthDto?> GetHealthAsync() => GetAsync<HealthDto>("api/health");
    public Task<DashboardDto?> GetDashboardAsync() => GetAsync<DashboardDto>("api/dashboard");
    public Task<SettingsDto?> GetSettingsAsync() => GetAsync<SettingsDto>("api/settings");
    public Task<SettingsDto?> UpdateSettingsAsync(SettingsDto dto) => PutAsync<SettingsDto>("api/settings", dto);

    public Task<List<ProductDto>?> GetProductsAsync(bool lowStock = false, string? q = null)
    {
        var path = "api/products";
        var qs = new List<string>();
        if (lowStock) qs.Add("lowStock=true");
        if (!string.IsNullOrWhiteSpace(q)) qs.Add("q=" + Uri.EscapeDataString(q.Trim()));
        if (qs.Count > 0) path += "?" + string.Join("&", qs);
        return GetAsync<List<ProductDto>>(path);
    }

    public Task<ProductDto?> GetProductByCodeAsync(string code) =>
        GetAsync<ProductDto>("api/products/by-code/" + Uri.EscapeDataString(code.Trim()));

    public Task<List<PartnerDto>?> GetSuppliersAsync() => GetAsync<List<PartnerDto>>("api/suppliers");
    public Task<List<PartnerDto>?> GetCustomersAsync() => GetAsync<List<PartnerDto>>("api/customers");
    public Task<List<PurchaseOrderDto>?> GetPurchaseOrdersAsync() => GetAsync<List<PurchaseOrderDto>>("api/purchase-orders");
    public Task<List<SalesOrderDto>?> GetSalesOrdersAsync() => GetAsync<List<SalesOrderDto>>("api/sales-orders");
    public Task<List<ReminderDto>?> GetRemindersAsync() => GetAsync<List<ReminderDto>>("api/reminders?unresolvedOnly=true");
    public Task<List<StockMovementDto>?> GetStockMovementsAsync(int? productId = null) =>
        GetAsync<List<StockMovementDto>>(productId.HasValue
            ? $"api/stock-movements?productId={productId}&take=200"
            : "api/stock-movements?take=200");

    public Task<ProductDto?> CreateProductAsync(ProductCreateDto dto) => PostAsync<ProductDto>("api/products", dto);
    public Task<ProductDto?> UpdateProductAsync(int id, ProductCreateDto dto) => PutAsync<ProductDto>($"api/products/{id}", dto);
    public Task DeleteProductAsync(int id) => DeleteAsync($"api/products/{id}");
    public Task<ProductDto?> AdjustProductAsync(int id, StockAdjustDto dto) => PostAsync<ProductDto>($"api/products/{id}/adjust", dto);

    public Task<ProductDto?> ScanAdjustAsync(ScanAdjustDto dto) => PostAsync<ProductDto>("api/scan/adjust", dto);
    public Task<PurchaseOrderDto?> ScanReceiveAsync(ScanReceiveDto dto) => PostAsync<PurchaseOrderDto>("api/scan/receive", dto);

    public Task<PartnerDto?> CreateSupplierAsync(PartnerCreateDto dto) => PostAsync<PartnerDto>("api/suppliers", dto);
    public Task<PartnerDto?> UpdateSupplierAsync(int id, PartnerCreateDto dto) => PutAsync<PartnerDto>($"api/suppliers/{id}", dto);
    public Task DeleteSupplierAsync(int id) => DeleteAsync($"api/suppliers/{id}");

    public Task<PartnerDto?> CreateCustomerAsync(PartnerCreateDto dto) => PostAsync<PartnerDto>("api/customers", dto);
    public Task<PartnerDto?> UpdateCustomerAsync(int id, PartnerCreateDto dto) => PutAsync<PartnerDto>($"api/customers/{id}", dto);
    public Task DeleteCustomerAsync(int id) => DeleteAsync($"api/customers/{id}");

    public Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(PurchaseOrderCreateDto dto) =>
        PostAsync<PurchaseOrderDto>("api/purchase-orders", dto);
    public Task<PurchaseOrderDto?> UpdatePurchaseOrderAsync(int id, PurchaseOrderUpdateDto dto) =>
        PutAsync<PurchaseOrderDto>($"api/purchase-orders/{id}", dto);
    public Task DeletePurchaseOrderAsync(int id) => DeleteAsync($"api/purchase-orders/{id}");
    public Task<PurchaseOrderDto?> ReceivePurchaseOrderAsync(int id, ReceivePurchaseOrderDto dto) =>
        PostAsync<PurchaseOrderDto>($"api/purchase-orders/{id}/receive", dto);

    public Task<SalesOrderDto?> CreateSalesOrderAsync(SalesOrderCreateDto dto) =>
        PostAsync<SalesOrderDto>("api/sales-orders", dto);
    public Task<SalesOrderDto?> UpdateSalesOrderAsync(int id, SalesOrderUpdateDto dto) =>
        PutAsync<SalesOrderDto>($"api/sales-orders/{id}", dto);
    public Task DeleteSalesOrderAsync(int id) => DeleteAsync($"api/sales-orders/{id}");

    public Task<ReminderDto?> CreateReminderAsync(ReminderCreateDto dto) => PostAsync<ReminderDto>("api/reminders", dto);
    public Task<ReminderDto?> UpdateReminderAsync(int id, ReminderCreateDto dto) => PutAsync<ReminderDto>($"api/reminders/{id}", dto);
    public Task DeleteReminderAsync(int id) => DeleteAsync($"api/reminders/{id}");

    public async Task RunRemindersAsync()
    {
        var res = await _http.PostAsync("api/reminders/run", null).ConfigureAwait(false);
        await EnsureSuccessAsync(res).ConfigureAwait(false);
    }

    public async Task ResolveReminderAsync(int id)
    {
        var res = await _http.PostAsync($"api/reminders/{id}/resolve", null).ConfigureAwait(false);
        await EnsureSuccessAsync(res).ConfigureAwait(false);
    }

    private async Task<T?> GetAsync<T>(string path)
    {
        var res = await _http.GetAsync(path).ConfigureAwait(false);
        await EnsureSuccessAsync(res).ConfigureAwait(false);
        var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json, _json);
    }

    private async Task<T?> PostAsync<T>(string path, object body)
    {
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var res = await _http.PostAsync(path, content).ConfigureAwait(false);
        await EnsureSuccessAsync(res).ConfigureAwait(false);
        var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json, _json);
    }

    private async Task<T?> PutAsync<T>(string path, object body)
    {
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var res = await _http.PutAsync(path, content).ConfigureAwait(false);
        await EnsureSuccessAsync(res).ConfigureAwait(false);
        var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json, _json);
    }

    private async Task DeleteAsync(string path)
    {
        var res = await _http.DeleteAsync(path).ConfigureAwait(false);
        await EnsureSuccessAsync(res).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode) return;
        var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
        var detail = string.IsNullOrWhiteSpace(body) ? res.ReasonPhrase : body.Trim('"');
        throw new HttpRequestException($"{(int)res.StatusCode} {res.ReasonPhrase}: {detail}");
    }

    public void Dispose() => _http.Dispose();
}
