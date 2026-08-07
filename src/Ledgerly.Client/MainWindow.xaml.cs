using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ledgerly.Client.Services;
using Ledgerly.Client.Views;

namespace Ledgerly.Client;

public partial class MainWindow : Window
{
    private string _page = "dashboard";

    public MainWindow()
    {
        WinCompat.RequireWindows7OrLater();
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            UserStatus.Text = $"{Session.DisplayName} · {Session.Role}";
            await RefreshConnectionAsync();
            await NavigateAsync("dashboard");
        };
    }

    private async void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            await NavigateAsync(tag);
    }

    private async void PrimaryAction_Click(object sender, RoutedEventArgs e) => await NavigateAsync(_page);

    private async void SecondaryAction_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await App.Api.RunRemindersAsync();
            MessageBox.Show("Reminder scan complete.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Information);
            await NavigateAsync(_page);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not run reminders.\n{ex.Message}", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task NavigateAsync(string page)
    {
        _page = page;
        try
        {
            switch (page)
            {
                case "dashboard":
                    SetHeader("OVERVIEW", "Operations dashboard", "Monitor stock health, open buying, and critical reminders.");
                    ContentHost.Content = await DashboardView.CreateAsync();
                    break;
                case "scan":
                    SetHeader("OVERVIEW", "Scan station", "Scan UPC/SKU to look up, adjust stock, receive POs, or ring up a quick sale.");
                    ContentHost.Content = await ScanStationView.CreateAsync();
                    break;
                case "reports":
                    SetHeader("OVERVIEW", "Reports", "Margins, AR/AP aging, dead stock, and operational KPIs.");
                    ContentHost.Content = await ReportsView.CreateAsync();
                    break;
                case "inventory":
                    SetHeader("OPERATIONS", "Inventory", "Track quantities, UPC barcodes, reorder points, and suggested buy amounts.");
                    ContentHost.Content = await InventoryView.CreateAsync();
                    break;
                case "warehouse":
                    SetHeader("OPERATIONS", "Warehouse", "Locations, transfers, cycle counts, and kit builds.");
                    ContentHost.Content = await WarehouseView.CreateAsync();
                    break;
                case "purchasing":
                    SetHeader("OPERATIONS", "Purchasing", "Buy stock, track deliveries, and receive quantities.");
                    ContentHost.Content = await PurchaseOrdersView.CreateAsync();
                    break;
                case "sales":
                    SetHeader("OPERATIONS", "Sales", "Fulfill customer orders and reduce inventory.");
                    ContentHost.Content = await SalesOrdersView.CreateAsync();
                    break;
                case "suppliers":
                    SetHeader("DIRECTORY", "Suppliers", "Vendor contacts for purchase orders.");
                    ContentHost.Content = await PartnersView.CreateAsync(suppliers: true);
                    break;
                case "customers":
                    SetHeader("DIRECTORY", "Customers", "Customer records for sales orders.");
                    ContentHost.Content = await PartnersView.CreateAsync(suppliers: false);
                    break;
                case "reminders":
                    SetHeader("OVERVIEW", "Reminders & alerts", "Low stock, suggested buys, and overdue deliveries.");
                    ContentHost.Content = await RemindersView.CreateAsync();
                    break;
                case "finance":
                    SetHeader("FINANCE", "Finance / GL", "Chart of accounts, journals, bank reconciliation, periods, multi-currency.");
                    ContentHost.Content = await FinanceView.CreateAsync();
                    break;
                case "users":
                    SetHeader("SYSTEM", "Users & roles", "Sign-in accounts and permission roles.");
                    ContentHost.Content = await UsersView.CreateAsync();
                    break;
                case "integrations":
                    SetHeader("SYSTEM", "Integrations", "Backups, API keys, webhooks, accounting export, audit log.");
                    ContentHost.Content = await IntegrationsView.CreateAsync();
                    break;
                case "settings":
                    SetHeader("SYSTEM", "Settings", "Company profile, tax, SMTP, and API connection.");
                    ContentHost.Content = await SettingsView.CreateAsync();
                    break;
            }
            await RefreshConnectionAsync();
        }
        catch (Exception ex)
        {
            ContentHost.Content = new TextBlock
            {
                Text = $"Unable to load page.\n\nIs the Ledgerly Server running at {App.Api.BaseAddress}?\n\n{ex.Message}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("MutedBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(24)
            };
        }
    }

    private void SetHeader(string eyebrow, string title, string subtitle)
    {
        EyebrowText.Text = eyebrow;
        TitleText.Text = title;
        SubtitleText.Text = subtitle;
    }

    private async Task RefreshConnectionAsync()
    {
        try
        {
            var health = await App.Api.GetHealthAsync();
            ConnectionStatus.Text = health is null
                ? $"API offline · {App.Api.BaseAddress}"
                : $"Connected to {health.App}";
        }
        catch
        {
            ConnectionStatus.Text = $"API offline · {App.Api.BaseAddress}";
        }
    }
}
