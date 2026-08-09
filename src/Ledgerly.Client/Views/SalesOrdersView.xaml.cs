using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Client.Services;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class SalesOrdersView : UserControl
{
    public SalesOrdersView() => InitializeComponent();

    public static async Task<SalesOrdersView> CreateAsync()
    {
        var view = new SalesOrdersView();
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow!;

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not SalesOrderDto selected) return;
        try { DocumentPrint.OpenHtml(await App.Api.GetSalesDocumentAsync(selected.Id)); }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Convert_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not SalesOrderDto selected) return;
        try { await App.Api.ConvertQuoteAsync(selected.Id, invoice: false); await LoadAsync(); }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Pay_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not SalesOrderDto selected) return;
        var amt = EntityDialogs.PromptDecimal(OwnerWindow, "Payment", $"Amount for {selected.OrderNumber}", selected.Total.ToString());
        if (amt is null or <= 0) return;
        if (sender is UIElement btn) btn.IsEnabled = false;
        try
        {
            var banks = await App.Api.GetBankAccountsAsync();
            await App.Api.CustomerPaymentAsync(new PaymentCreateDto
            {
                CustomerId = selected.CustomerId,
                SalesOrderId = selected.Id,
                Amount = amt.Value,
                Method = "card",
                BankAccountId = banks?.FirstOrDefault()?.Id
            });
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
        finally { if (sender is UIElement b) b.IsEnabled = true; }
    }

    private async void Return_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not SalesOrderDto selected || selected.Lines.Count == 0) return;
        try
        {
            var line = selected.Lines[0];
            await App.Api.CreateSalesReturnAsync(new SalesReturnCreateDto
            {
                CustomerId = selected.CustomerId,
                SalesOrderId = selected.Id,
                Lines = { new SalesReturnLineDto { ProductId = line.ProductId, Quantity = 1, UnitPrice = line.UnitPrice } }
            });
            MessageBox.Show("RMA created and stock returned.", "Coalesce");
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var customers = await App.Api.GetCustomersAsync() ?? new();
            var products = await App.Api.GetProductsAsync() ?? new();
            var dto = EntityDialogs.EditSalesOrder(OwnerWindow, null, customers, products);
            if (dto is null) return;
            await App.Api.CreateSalesOrderAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not SalesOrderDto selected)
        {
            MessageBox.Show("Select a sales order first.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var customers = await App.Api.GetCustomersAsync() ?? new();
            var products = await App.Api.GetProductsAsync() ?? new();
            var dto = EntityDialogs.EditSalesOrder(OwnerWindow, selected, customers, products);
            if (dto is null) return;
            await App.Api.UpdateSalesOrderAsync(selected.Id, new SalesOrderUpdateDto
            {
                CustomerId = dto.CustomerId,
                Notes = dto.Notes,
                Lines = dto.Lines
            });
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not SalesOrderDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.OrderNumber)) return;
        try
        {
            await App.Api.DeleteSalesOrderAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var items = (Grid.ItemsSource as IEnumerable<SalesOrderDto>)?.ToList() ?? new List<SalesOrderDto>();
        CsvExport.Save("ledgerly-sales-orders.csv",
            new[] { "OrderNumber", "Customer", "Status", "Date", "Subtotal", "Tax", "Total" },
            items.Select(s => new object?[] { s.OrderNumber, s.CustomerName, s.Status, s.OrderDate, s.Subtotal, s.TaxAmount, s.Total }));
    }

    private async Task LoadAsync() => Grid.ItemsSource = await App.Api.GetSalesOrdersAsync();
}
