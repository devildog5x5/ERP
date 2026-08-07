using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    public static async Task<SettingsView> CreateAsync()
    {
        var view = new SettingsView();
        await view.LoadAsync();
        return view;
    }

    private async Task LoadAsync()
    {
        ApiUrlBox.Text = App.Config.ApiBaseUrl;
        await RefreshPlatformAsync();
        try
        {
            var s = await App.Api.GetSettingsAsync() ?? new SettingsDto();
            CompanyNameBox.Text = s.CompanyName;
            TaxRateBox.Text = s.DefaultTaxRate.ToString(CultureInfo.InvariantCulture);
            CurrencyBox.Text = s.Currency;
            FooterBox.Text = s.ReceiptFooter ?? "";
            ApprovalBox.Text = s.PoApprovalThreshold.ToString(CultureInfo.InvariantCulture);
            SmtpHostBox.Text = s.SmtpHost ?? "";
            SmtpFromBox.Text = s.SmtpFrom ?? "";
            StatusText.Text = "Loaded from server.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not load server settings: " + ex.Message;
        }
    }

    private async void RefreshPlatform_Click(object sender, RoutedEventArgs e) => await RefreshPlatformAsync();

    private async Task RefreshPlatformAsync()
    {
        try
        {
            var health = await App.Api.GetHealthAsync();
            if (health is null)
            {
                DbPlatformText.Text = "API offline — cannot read database platform.";
                return;
            }

            var scale = health.CanScaleOut
                ? "Ready for multi-user / larger workloads (SQL Server)."
                : "Local SQLite (great for single shop). Migrate to SQL Server when you need more concurrency.";
            DbPlatformText.Text =
                $"Provider: {health.DatabaseProvider}\n{health.Database}\n{scale}";
        }
        catch (Exception ex)
        {
            DbPlatformText.Text = "Could not read platform info: " + ex.Message;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Config.ApiBaseUrl = ApiUrlBox.Text.Trim();
            if (!App.Config.ApiBaseUrl.EndsWith("/")) App.Config.ApiBaseUrl += "/";
            App.Config.Save();
            App.Api.SetBaseAddress(App.Config.ApiBaseUrl);

            if (!decimal.TryParse(TaxRateBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var tax)
                && !decimal.TryParse(TaxRateBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out tax))
                tax = 0;

            if (!decimal.TryParse(ApprovalBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var threshold)
                && !decimal.TryParse(ApprovalBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out threshold))
                threshold = 1000;

            var updated = await App.Api.UpdateSettingsAsync(new SettingsDto
            {
                CompanyName = CompanyNameBox.Text.Trim(),
                DefaultTaxRate = tax,
                Currency = CurrencyBox.Text.Trim(),
                ReceiptFooter = FooterBox.Text.Trim(),
                PoApprovalThreshold = threshold,
                SmtpHost = SmtpHostBox.Text.Trim(),
                SmtpFrom = SmtpFromBox.Text.Trim(),
                RequireLogin = true
            });
            StatusText.Text = $"Saved. Company: {updated?.CompanyName}, tax {updated?.DefaultTaxRate}%.";
            await RefreshPlatformAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Api.SetBaseAddress(ApiUrlBox.Text.Trim().EndsWith("/") ? ApiUrlBox.Text.Trim() : ApiUrlBox.Text.Trim() + "/");
            var health = await App.Api.GetHealthAsync();
            StatusText.Text = health is null
                ? "No response."
                : $"Connected: {health.App} · {health.DatabaseProvider}";
            await RefreshPlatformAsync();
        }
        catch (Exception ex) { StatusText.Text = "Connection failed: " + ex.Message; }
    }
}
