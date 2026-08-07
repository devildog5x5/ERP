using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class FinanceView : UserControl
{
    public FinanceView() => InitializeComponent();

    public static async Task<FinanceView> CreateAsync()
    {
        var v = new FinanceView();
        await v.LoadAsync();
        return v;
    }

    private async Task LoadAsync()
    {
        GlGrid.ItemsSource = await App.Api.GetGlAccountsAsync();
        JeGrid.ItemsSource = await App.Api.GetJournalsAsync();
        BankGrid.ItemsSource = await App.Api.GetBankTransactionsAsync();
        PeriodGrid.ItemsSource = await App.Api.GetFiscalPeriodsAsync();
        FxGrid.ItemsSource = await App.Api.GetCurrenciesAsync();
        CoGrid.ItemsSource = await App.Api.GetCompaniesAsync();
    }

    private async void Reconcile_Click(object sender, RoutedEventArgs e)
    {
        if (BankGrid.SelectedItem is not BankTransactionDto t) return;
        try { await App.Api.ReconcileBankAsync(t.Id); await LoadAsync(); }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void ClosePeriod_Click(object sender, RoutedEventArgs e)
    {
        if (PeriodGrid.SelectedItem is not FiscalPeriodDto p) return;
        if (!EntityDialogs.ConfirmDelete(Window.GetWindow(this)!, $"close period {p.Name}")) return;
        try { await App.Api.ClosePeriodAsync(p.Id); await LoadAsync(); }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }
}
