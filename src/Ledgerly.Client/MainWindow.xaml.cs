using System;
using System.Linq;
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
    private int _navGeneration;

    public MainWindow()
    {
        WinCompat.RequireWindows7OrLater();
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            ApplyAccess();
            UserStatus.Text = $"{Session.DisplayName} · {Session.Role}";
            await RefreshConnectionAsync();
            var start = Session.Can("dashboard") ? "dashboard"
                : FirstAllowedPage() ?? "dashboard";
            await NavigateAsync(start);
        };
    }

    public void ApplyAccess()
    {
        Access.ApplyTree(this);

        // Security: change password for everyone; users/access for Administrators only.
        NavPasswordBtn.Visibility = Visibility.Visible;
        NavPasswordBtn.IsEnabled = true;
        NavUsersBtn.Visibility = Session.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
        NavUsersBtn.IsEnabled = Session.IsAdministrator;
        // Header stays visible whenever Change password (always) or Users is shown.
        NavSecurityHeader.Visibility = Visibility.Visible;

        UpdateSectionHeaders();
    }

    private void UpdateSectionHeaders()
    {
        SetHeaderVisible(NavOverviewHeader, "dashboard", "scan", "reports", "reminders");
        SetHeaderVisible(NavOperationsHeader, "inventory", "warehouse", "purchasing", "sales");
        SetHeaderVisible(NavDirectoryHeader, "partners");
        SetHeaderVisible(NavFinanceHeader, "finance");
        SetHeaderVisible(NavSystemHeader, "integrations", "settings");
        // SECURITY always has at least "Change password".
        NavSecurityHeader.Visibility = Visibility.Visible;
    }

    private static void SetHeaderVisible(UIElement header, params string[] permissions)
    {
        header.Visibility = permissions.Any(Session.Can) ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string? FirstAllowedPage()
    {
        foreach (var page in new[]
                 {
                     "dashboard", "scan", "inventory", "sales", "purchasing", "warehouse",
                     "partners", "reminders", "reports", "finance", "users", "integrations", "settings"
                 })
        {
            if (Session.Can(Access.PermissionForPage(page == "partners" ? "suppliers" : page)))
                return page == "partners" ? "suppliers" : page;
        }
        return null;
    }

    private async void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            await NavigateAsync(tag);
    }

    private async void PrimaryAction_Click(object sender, RoutedEventArgs e) => await NavigateAsync(_page);

    private async void SecondaryAction_Click(object sender, RoutedEventArgs e)
    {
        if (!Access.Ensure("reminders", "run reminder scans")) return;
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
        var required = Access.PermissionForPage(page);
        if (!string.IsNullOrEmpty(required) && !Session.Can(required))
        {
            ContentHost.Content = new TextBlock
            {
                Text = $"Access denied.\n\nYour role ({Session.Role}) does not include \"{required}\".",
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("MutedBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(24)
            };
            return;
        }

        var gen = ++_navGeneration;
        _page = page;
        try
        {
            object? content = null;
            switch (page)
            {
                case "dashboard":
                    SetHeader("OVERVIEW", "Operations dashboard", "Monitor stock health, open buying, and critical reminders.");
                    content = await DashboardView.CreateAsync();
                    break;
                case "scan":
                    SetHeader("OVERVIEW", "Scan station", "Scan UPC/SKU to look up, adjust stock, receive POs, or ring up a quick sale.");
                    content = await ScanStationView.CreateAsync();
                    break;
                case "reports":
                    SetHeader("OVERVIEW", "Reports", "Margins, AR/AP aging, dead stock, and operational KPIs.");
                    content = await ReportsView.CreateAsync();
                    break;
                case "inventory":
                    SetHeader("OPERATIONS", "Inventory", "Track quantities, UPC barcodes, reorder points, and suggested buy amounts.");
                    content = await InventoryView.CreateAsync();
                    break;
                case "warehouse":
                    SetHeader("OPERATIONS", "Warehouse", "Locations, transfers, cycle counts, and kit builds.");
                    content = await WarehouseView.CreateAsync();
                    break;
                case "purchasing":
                    SetHeader("OPERATIONS", "Purchasing", "Buy stock, track deliveries, and receive quantities.");
                    content = await PurchaseOrdersView.CreateAsync();
                    break;
                case "sales":
                    SetHeader("OPERATIONS", "Sales", "Fulfill customer orders and reduce inventory.");
                    content = await SalesOrdersView.CreateAsync();
                    break;
                case "suppliers":
                    SetHeader("DIRECTORY", "Suppliers", "Vendor contacts for purchase orders.");
                    content = await PartnersView.CreateAsync(suppliers: true);
                    break;
                case "customers":
                    SetHeader("DIRECTORY", "Customers", "Customer records for sales orders.");
                    content = await PartnersView.CreateAsync(suppliers: false);
                    break;
                case "reminders":
                    SetHeader("OVERVIEW", "Reminders & alerts", "Low stock, suggested buys, and overdue deliveries.");
                    content = await RemindersView.CreateAsync();
                    break;
                case "finance":
                    SetHeader("FINANCE", "Finance / GL", "Chart of accounts, journals, bank reconciliation, periods, multi-currency.");
                    content = await FinanceView.CreateAsync();
                    break;
                case "users":
                    if (!Session.IsAdministrator)
                    {
                        ContentHost.Content = new TextBlock
                        {
                            Text = "Access denied.\n\nOnly an Administrator can manage users and access levels.",
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = (Brush)FindResource("MutedBrush"),
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(24)
                        };
                        return;
                    }
                    SetHeader("SECURITY", "Users & access", "Add users, delete accounts, and assign roles that control element access.");
                    content = await UsersView.CreateAsync();
                    break;
                case "password":
                    SetHeader("SECURITY", "Change password", "Update the password for your signed-in account.");
                    content = await ChangePasswordView.CreateAsync();
                    break;
                case "integrations":
                    SetHeader("SYSTEM", "Integrations", "Backups, API keys, webhooks, accounting export, audit log.");
                    content = await IntegrationsView.CreateAsync();
                    break;
                case "settings":
                    SetHeader("SYSTEM", "Settings", "Company profile, tax, SMTP, and API connection.");
                    content = await SettingsView.CreateAsync();
                    break;
            }

            if (gen != _navGeneration) return;
            if (content != null)
            {
                ContentHost.Content = content;
                if (content is DependencyObject d)
                    Access.ApplyTree(d);
            }
            await RefreshConnectionAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            if (gen != _navGeneration) return;
            if (App.PromptRelogin(ex.Message))
            {
                ApplyAccess();
                UserStatus.Text = $"{Session.DisplayName} · {Session.Role}";
                await NavigateAsync(page);
            }
        }
        catch (Exception ex)
        {
            if (gen != _navGeneration) return;
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
