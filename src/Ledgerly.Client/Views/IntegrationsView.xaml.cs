using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;
using Microsoft.Win32;

namespace Ledgerly.Client.Views;

public partial class IntegrationsView : UserControl
{
    public IntegrationsView() => InitializeComponent();

    public static async Task<IntegrationsView> CreateAsync()
    {
        var v = new IntegrationsView();
        await v.LoadAsync();
        return v;
    }

    private async Task LoadAsync()
    {
        LogGrid.ItemsSource = await App.Api.GetIntegrationLogsAsync();
        HookGrid.ItemsSource = await App.Api.GetWebhooksAsync();
        BackupGrid.ItemsSource = await App.Api.ListBackupsAsync();
        AuditGrid.ItemsSource = await App.Api.GetAuditLogsAsync();
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var r = await App.Api.BackupAsync();
            MessageBox.Show($"Backup created:\n{r?.Path}", "Ledgerly");
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Shopify_Click(object sender, RoutedEventArgs e)
    {
        try { await App.Api.ShopifySyncAsync(); await LoadAsync(); MessageBox.Show("Shopify sync stub logged."); }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var obj = await App.Api.AccountingExportAsync();
            var csv = obj?["csv"]?.ToString() ?? "";
            var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "ledgerly-gl-export.csv" };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, csv);
                MessageBox.Show("Exported.", "Ledgerly");
            }
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void ApiKey_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = EntityDialogs.FieldPrompt(Window.GetWindow(this)!, "API key name", "Integration");
            if (string.IsNullOrWhiteSpace(name)) return;
            var key = await App.Api.CreateApiKeyAsync(name!);
            MessageBox.Show($"API key created (copy now):\n{key?.ApiKey}", "Ledgerly");
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Webhook_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = EntityDialogs.FieldPrompt(Window.GetWindow(this)!, "Webhook URL", "https://example.com/hooks/ledgerly");
            if (string.IsNullOrWhiteSpace(url)) return;
            await App.Api.CreateWebhookAsync(new WebhookDto { EventName = "sales.created", TargetUrl = url! });
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }
}
