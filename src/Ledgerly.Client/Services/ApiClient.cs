using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ledgerly.Shared;
using Newtonsoft.Json;

namespace Ledgerly.Client.Services;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private Uri _baseUri;
    private string? _authToken;
    private readonly JsonSerializerSettings _json = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public ApiClient(string baseAddress = "http://127.0.0.1:8000/")
    {
        // Do not set/mutate HttpClient.BaseAddress after the first request —
        // .NET throws InvalidOperationException. Keep the base URL ourselves.
        _baseUri = new Uri(NormalizeBaseAddress(baseAddress));
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public string BaseAddress => _baseUri.ToString();

    public void SetBaseAddress(string baseAddress)
    {
        _baseUri = new Uri(NormalizeBaseAddress(baseAddress));
    }

    public static string NormalizeBaseAddress(string baseAddress)
    {
        var url = (baseAddress ?? "").Trim();
        if (string.IsNullOrWhiteSpace(url))
            url = "http://127.0.0.1:8000/";
        if (!url.EndsWith("/")) url += "/";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("API base URL must be an absolute http(s) address.", nameof(baseAddress));
        return uri.ToString();
    }

    public void SetAuthToken(string? token)
    {
        _authToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
    }

    private Uri Url(string path) => new Uri(_baseUri, path);

    private HttpRequestMessage Request(HttpMethod method, string path, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, Url(path)) { Content = content };
        if (!string.IsNullOrWhiteSpace(_authToken))
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _authToken);
        return req;
    }

    public Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto) => PostAsync<LoginResponseDto>("api/auth/login", dto);
    public Task<LoginResponseDto?> ChangePasswordAsync(ChangePasswordDto dto) =>
        PostAsync<LoginResponseDto>("api/auth/change-password", dto);
    public Task<List<UserDto>?> GetUsersAsync() => GetAsync<List<UserDto>>("api/users");
    public Task<UserDto?> CreateUserAsync(UserCreateDto dto) => PostAsync<UserDto>("api/users", dto);
    public Task ResetPasswordAsync(int id, ResetPasswordDto dto) =>
        PostAsync<object>($"api/users/{id}/reset-password", dto);
    public Task DeleteUserAsync(int id) => DeleteAsync($"api/users/{id}");
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
    public Task<DatabaseRefreshResultDto?> RefreshDatabaseAsync(DatabaseRefreshDto dto) =>
        PostAsync<DatabaseRefreshResultDto>("api/database/refresh", dto);
    public Task<DatabaseStatusDto?> GetDatabaseStatusAsync() => GetAsync<DatabaseStatusDto>("api/database/status");
    public Task<DatabasePurgeResultDto?> PurgeDatabaseMaintenanceAsync(DatabasePurgeDto dto) =>
        PostAsync<DatabasePurgeResultDto>("api/database/purge-maintenance", dto);

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

    // --- CRM ---
    public Task<List<CrmLeadDto>?> GetCrmLeadsAsync() => GetAsync<List<CrmLeadDto>>("api/crm/leads");
    public Task<CrmLeadDto?> CreateCrmLeadAsync(CrmLeadDto dto) => PostAsync<CrmLeadDto>("api/crm/leads", dto);
    public Task<CrmLeadDto?> UpdateCrmLeadAsync(int id, CrmLeadDto dto) => PutAsync<CrmLeadDto>($"api/crm/leads/{id}", dto);
    public Task DeleteCrmLeadAsync(int id) => DeleteAsync($"api/crm/leads/{id}");
    public async Task ConvertCrmLeadAsync(int id, bool createCustomer = true)
    {
        var content = new StringContent(
            JsonConvert.SerializeObject(new CrmLeadConvertDto { CreateCustomer = createCustomer }),
            Encoding.UTF8, "application/json");
        using var req = Request(HttpMethod.Post, $"api/crm/leads/{id}/convert", content);
        using var res = await _http.SendAsync(req);
        await EnsureSuccessAsync(res);
    }

    public Task<List<CrmAccountDto>?> GetCrmAccountsAsync() => GetAsync<List<CrmAccountDto>>("api/crm/accounts");
    public Task<CrmAccountDto?> CreateCrmAccountAsync(CrmAccountDto dto) => PostAsync<CrmAccountDto>("api/crm/accounts", dto);
    public Task<CrmAccountDto?> UpdateCrmAccountAsync(int id, CrmAccountDto dto) => PutAsync<CrmAccountDto>($"api/crm/accounts/{id}", dto);
    public Task DeleteCrmAccountAsync(int id) => DeleteAsync($"api/crm/accounts/{id}");
    public Task<CrmAccountDto?> LinkCrmAccountCustomerAsync(int id) =>
        PostAsync<CrmAccountDto>($"api/crm/accounts/{id}/link-customer", new { });

    public Task<List<CrmContactDto>?> GetCrmContactsAsync(int? accountId = null) =>
        GetAsync<List<CrmContactDto>>(accountId is int a ? $"api/crm/contacts?accountId={a}" : "api/crm/contacts");
    public Task<CrmContactDto?> CreateCrmContactAsync(CrmContactDto dto) => PostAsync<CrmContactDto>("api/crm/contacts", dto);
    public Task<CrmContactDto?> UpdateCrmContactAsync(int id, CrmContactDto dto) => PutAsync<CrmContactDto>($"api/crm/contacts/{id}", dto);
    public Task DeleteCrmContactAsync(int id) => DeleteAsync($"api/crm/contacts/{id}");

    public Task<List<CrmOpportunityDto>?> GetCrmOpportunitiesAsync(string? stage = null) =>
        GetAsync<List<CrmOpportunityDto>>(string.IsNullOrWhiteSpace(stage) ? "api/crm/opportunities" : $"api/crm/opportunities?stage={Uri.EscapeDataString(stage!)}");
    public Task<CrmOpportunityDto?> CreateCrmOpportunityAsync(CrmOpportunityDto dto) => PostAsync<CrmOpportunityDto>("api/crm/opportunities", dto);
    public Task<CrmOpportunityDto?> UpdateCrmOpportunityAsync(int id, CrmOpportunityDto dto) => PutAsync<CrmOpportunityDto>($"api/crm/opportunities/{id}", dto);
    public Task DeleteCrmOpportunityAsync(int id) => DeleteAsync($"api/crm/opportunities/{id}");
    public Task<CrmOpportunityDto?> WinCrmOpportunityAsync(int id, string documentType = "quote") =>
        PostAsync<CrmOpportunityDto>($"api/crm/opportunities/{id}/win", new CrmOpportunityWinDto { DocumentType = documentType });

    public Task<List<CrmActivityDto>?> GetCrmActivitiesAsync(string? status = null) =>
        GetAsync<List<CrmActivityDto>>(string.IsNullOrWhiteSpace(status) ? "api/crm/activities" : $"api/crm/activities?status={Uri.EscapeDataString(status!)}");
    public Task<CrmActivityDto?> CreateCrmActivityAsync(CrmActivityDto dto) => PostAsync<CrmActivityDto>("api/crm/activities", dto);
    public Task<CrmActivityDto?> UpdateCrmActivityAsync(int id, CrmActivityDto dto) => PutAsync<CrmActivityDto>($"api/crm/activities/{id}", dto);
    public Task DeleteCrmActivityAsync(int id) => DeleteAsync($"api/crm/activities/{id}");

    public Task<List<CrmNoteDto>?> GetCrmNotesAsync(int? accountId = null) =>
        GetAsync<List<CrmNoteDto>>(accountId is int a ? $"api/crm/notes?accountId={a}" : "api/crm/notes");
    public Task<CrmNoteDto?> CreateCrmNoteAsync(CrmNoteDto dto) => PostAsync<CrmNoteDto>("api/crm/notes", dto);

    public Task<List<CrmCommunicationDto>?> GetCrmCommunicationsAsync(int? accountId = null) =>
        GetAsync<List<CrmCommunicationDto>>(accountId is int a ? $"api/crm/communications?accountId={a}" : "api/crm/communications");
    public Task<CrmCommunicationDto?> CreateCrmCommunicationAsync(CrmCommunicationDto dto) =>
        PostAsync<CrmCommunicationDto>("api/crm/communications", dto);

    public Task<ReminderDto?> CreateReminderAsync(ReminderCreateDto dto) => PostAsync<ReminderDto>("api/reminders", dto);
    public Task<ReminderDto?> UpdateReminderAsync(int id, ReminderCreateDto dto) => PutAsync<ReminderDto>($"api/reminders/{id}", dto);
    public Task DeleteReminderAsync(int id) => DeleteAsync($"api/reminders/{id}");

    public async Task RunRemindersAsync()
    {
        using var req = Request(HttpMethod.Post, "api/reminders/run");
        using var res = await _http.SendAsync(req);
        await EnsureSuccessAsync(res);
    }

    public async Task ResolveReminderAsync(int id)
    {
        using var req = Request(HttpMethod.Post, $"api/reminders/{id}/resolve");
        using var res = await _http.SendAsync(req);
        await EnsureSuccessAsync(res);
    }

    /// <summary>GET health against an arbitrary base URL without mutating this client.</summary>
    public static async Task<HealthDto?> ProbeHealthAsync(string baseAddress)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var uri = new Uri(new Uri(NormalizeBaseAddress(baseAddress)), "api/health");
        using var res = await http.GetAsync(uri);
        if (!res.IsSuccessStatusCode) return null;
        var json = await res.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<HealthDto>(json);
    }

    private async Task<T?> GetAsync<T>(string path)
    {
        using var req = Request(HttpMethod.Get, path);
        using var res = await _http.SendAsync(req);
        await EnsureSuccessAsync(res);
        var json = await res.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(json, _json);
    }

    private async Task<T?> PostAsync<T>(string path, object body)
    {
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        using var req = Request(HttpMethod.Post, path, content);
        using var res = await _http.SendAsync(req);
        await EnsureSuccessAsync(res);
        var json = await res.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(json, _json);
    }

    private async Task<T?> PutAsync<T>(string path, object body)
    {
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        using var req = Request(HttpMethod.Put, path, content);
        using var res = await _http.SendAsync(req);
        await EnsureSuccessAsync(res);
        var json = await res.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(json, _json);
    }

    private async Task DeleteAsync(string path)
    {
        using var req = Request(HttpMethod.Delete, path);
        using var res = await _http.SendAsync(req);
        await EnsureSuccessAsync(res);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode) return;
        var body = await res.Content.ReadAsStringAsync();
        var detail = string.IsNullOrWhiteSpace(body) ? res.ReasonPhrase : body.Trim('"');
        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            SetAuthToken(null);
            Session.Clear();
            throw new UnauthorizedAccessException("Session expired or unauthorized. Sign in again.");
        }
        throw new HttpRequestException($"{(int)res.StatusCode} {res.ReasonPhrase}: {detail}");
    }

    public void Dispose() => _http.Dispose();
}
