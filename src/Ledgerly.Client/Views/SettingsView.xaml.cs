using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Ledgerly.Client.Dialogs;
using Ledgerly.Client.Services;
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

    private Window? OwnerWindow => Window.GetWindow(this);

    private async Task LoadAsync()
    {
        ApiUrlBox.Text = App.Config.ApiBaseUrl;
        AdminDangerPanel.Visibility = Session.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
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
            var apiUrl = ApiClient.NormalizeBaseAddress(ApiUrlBox.Text);
            ApiUrlBox.Text = apiUrl;
            if (!string.Equals(App.Config.ApiBaseUrl, apiUrl, StringComparison.OrdinalIgnoreCase))
            {
                App.Config.ApiBaseUrl = apiUrl;
                App.Config.Save();
                App.Api.SetBaseAddress(apiUrl);
            }

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
            var url = ApiUrlBox.Text.Trim();
            var health = await ApiClient.ProbeHealthAsync(url);
            StatusText.Text = health is null
                ? "No response."
                : $"Connected: {health.App} · {health.DatabaseProvider}";
        }
        catch (Exception ex) { StatusText.Text = "Connection failed: " + ex.Message; }
    }

    private async void RefreshDatabase_Click(object sender, RoutedEventArgs e)
    {
        if (!Session.IsAdministrator)
        {
            MessageBox.Show(OwnerWindow,
                "Only an Administrator can refresh the database.",
                "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var owner = OwnerWindow;
        // Modal requires all 3 confirmations before returning true.
        if (!EntityDialogs.ConfirmDatabaseRefresh(owner))
        {
            StatusText.Text = "Database refresh cancelled — triple confirmation was not completed.";
            return;
        }

        try
        {
            SetRefreshBusy(true);
            await SetProgressAsync(10, "Starting database refresh…");
            await SetProgressAsync(25, "Creating backup on server…");
            await SetProgressAsync(45, "Wiping database and reseeding…", indeterminate: true);

            var result = await App.Api.RefreshDatabaseAsync(new DatabaseRefreshDto
            {
                Confirmation = "REFRESH DATABASE"
            });

            await SetProgressAsync(90, "Finalizing…");
            App.Api.SetAuthToken(null);
            Session.Clear();
            await SetProgressAsync(100, "Database refreshed.");

            MessageBox.Show(owner,
                (result?.Message ?? "Database refreshed.") +
                (string.IsNullOrWhiteSpace(result?.BackupPath) ? "" : $"\n\nBackup: {result!.BackupPath}") +
                "\n\nSign in again with admin / admin.",
                "Database refreshed",
                MessageBoxButton.OK, MessageBoxImage.Information);

            if (!App.PromptRelogin("Database was refreshed. Sign in again."))
            {
                Application.Current.Shutdown();
                return;
            }

            await LoadAsync();
            StatusText.Text = "Database refreshed. Signed in again.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Database refresh failed.";
            EntityDialogs.ShowError(ex);
        }
        finally
        {
            SetRefreshBusy(false);
        }
    }

    private void SetRefreshBusy(bool busy)
    {
        RefreshDatabaseBtn.IsEnabled = !busy;
        RefreshProgressPanel.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        if (busy)
        {
            RefreshProgressBar.IsIndeterminate = true;
            RefreshProgressBar.Value = 0;
            RefreshProgressText.Text = "Working…";
        }
        else
        {
            RefreshProgressBar.IsIndeterminate = false;
            RefreshProgressBar.Value = 0;
        }
    }

    private async Task SetProgressAsync(double value, string message, bool indeterminate = false)
    {
        RefreshProgressBar.IsIndeterminate = indeterminate;
        if (!indeterminate)
            RefreshProgressBar.Value = Math.Max(0, Math.Min(100, value));
        RefreshProgressText.Text = message;
        StatusText.Text = message;
        await Dispatcher.Yield(DispatcherPriority.Background);
        await Task.Delay(50);
    }
}
