using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ledgerly.Client.Dialogs;
using Ledgerly.Client.Services;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class InventoryView : UserControl
{
    private bool _lowOnly;
    private List<ProductDto> _items = new();

    public InventoryView() => InitializeComponent();

    public static async Task<InventoryView> CreateAsync()
    {
        var view = new InventoryView();
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow;

    private async void ToggleLow_Click(object sender, RoutedEventArgs e)
    {
        _lowOnly = !_lowOnly;
        ToggleLowBtn.Content = _lowOnly ? "Show all" : "Show low stock";
        try { await LoadAsync(); }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            try { await LoadAsync(); }
            catch (Exception ex) { EntityDialogs.ShowError(ex); }
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var suppliers = await App.Api.GetSuppliersAsync() ?? new();
            var dto = EntityDialogs.EditProduct(OwnerWindow, null, suppliers);
            if (dto is null) return;
            await App.Api.CreateProductAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not ProductDto selected)
        {
            MessageBox.Show("Select a product first.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var suppliers = await App.Api.GetSuppliersAsync() ?? new();
            var dto = EntityDialogs.EditProduct(OwnerWindow, selected, suppliers);
            if (dto is null) return;
            await App.Api.UpdateProductAsync(selected.Id, dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not ProductDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.Name)) return;
        try
        {
            await App.Api.DeleteProductAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Adjust_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not ProductDto selected)
        {
            MessageBox.Show("Select a product first.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var delta = EntityDialogs.PromptDecimal(OwnerWindow, "Adjust stock",
            $"Adjust quantity for {selected.Sku}. Enter delta (e.g. 5 or -2).", "1");
        if (delta is null or 0) return;
        try
        {
            await App.Api.AdjustProductAsync(selected.Id, new StockAdjustDto { QuantityDelta = delta.Value, Notes = "Manual adjust" });
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        CsvExport.Save("ledgerly-inventory.csv",
            new[] { "Sku", "Upc", "Name", "Category", "OnHand", "ReorderPoint", "ReorderQty", "UnitCost", "SellPrice" },
            _items.Select(p => new object?[]
            {
                p.Sku, p.Upc, p.Name, p.Category, p.QuantityOnHand, p.ReorderPoint, p.ReorderQuantity, p.UnitCost, p.SellPrice
            }));
    }

    private async Task LoadAsync()
    {
        _items = await App.Api.GetProductsAsync(_lowOnly, SearchBox.Text) ?? new List<ProductDto>();
        Grid.ItemsSource = _items;
    }
}
