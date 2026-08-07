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

public partial class PurchaseOrdersView : UserControl
{
    public PurchaseOrdersView() => InitializeComponent();

    public static async Task<PurchaseOrdersView> CreateAsync()
    {
        var view = new PurchaseOrdersView();
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow!;

    private async void Approve_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not PurchaseOrderDto selected) return;
        try { await App.Api.ApprovePoAsync(selected.Id); await LoadAsync(); }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not PurchaseOrderDto selected) return;
        try { DocumentPrint.OpenHtml(await App.Api.GetPurchaseDocumentAsync(selected.Id)); }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Bill_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not PurchaseOrderDto selected) return;
        try
        {
            await App.Api.CreateVendorBillAsync(new VendorBillCreateDto
            {
                SupplierId = selected.SupplierId,
                PurchaseOrderId = selected.Id,
                Amount = selected.Total
            });
            MessageBox.Show("Vendor bill created.", "Ledgerly");
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var suppliers = await App.Api.GetSuppliersAsync() ?? new();
            var products = await App.Api.GetProductsAsync() ?? new();
            var dto = EntityDialogs.EditPurchaseOrder(OwnerWindow, null, suppliers, products);
            if (dto is null) return;
            await App.Api.CreatePurchaseOrderAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not PurchaseOrderDto selected)
        {
            MessageBox.Show("Select a purchase order first.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var suppliers = await App.Api.GetSuppliersAsync() ?? new();
            var products = await App.Api.GetProductsAsync() ?? new();
            var dto = EntityDialogs.EditPurchaseOrder(OwnerWindow, selected, suppliers, products);
            if (dto is null) return;
            await App.Api.UpdatePurchaseOrderAsync(selected.Id, new PurchaseOrderUpdateDto
            {
                SupplierId = dto.SupplierId,
                ExpectedDate = dto.ExpectedDate,
                Notes = dto.Notes,
                Lines = dto.Lines
            });
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Receive_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not PurchaseOrderDto selected)
        {
            MessageBox.Show("Select a purchase order first.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dto = EntityDialogs.ReceivePurchaseOrder(OwnerWindow, selected);
            if (dto is null || dto.Lines.Count == 0) return;
            await App.Api.ReceivePurchaseOrderAsync(selected.Id, dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not PurchaseOrderDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.PoNumber)) return;
        try
        {
            await App.Api.DeletePurchaseOrderAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var items = (Grid.ItemsSource as IEnumerable<PurchaseOrderDto>)?.ToList() ?? new List<PurchaseOrderDto>();
        CsvExport.Save("ledgerly-purchase-orders.csv",
            new[] { "PoNumber", "Supplier", "Status", "Expected", "Total" },
            items.Select(p => new object?[] { p.PoNumber, p.SupplierName, p.Status, p.ExpectedDate, p.Total }));
    }

    private async Task LoadAsync() => Grid.ItemsSource = await App.Api.GetPurchaseOrdersAsync();
}
