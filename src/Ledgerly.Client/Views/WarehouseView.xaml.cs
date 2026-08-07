using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class WarehouseView : UserControl
{
    public WarehouseView() => InitializeComponent();

    public static async Task<WarehouseView> CreateAsync()
    {
        var v = new WarehouseView();
        await v.LoadAsync();
        return v;
    }

    private Window Owner => Window.GetWindow(this) ?? Application.Current.MainWindow!;

    private async Task LoadAsync()
    {
        LocGrid.ItemsSource = await App.Api.GetProductLocationsAsync();
        LocationsGrid.ItemsSource = await App.Api.GetLocationsAsync();
    }

    private async void Transfer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var locs = await App.Api.GetLocationsAsync() ?? new();
            var products = await App.Api.GetProductsAsync() ?? new();
            if (locs.Count < 2 || products.Count == 0) { MessageBox.Show("Need 2+ locations and products."); return; }
            var from = locs[0].Id;
            var to = locs[1].Id;
            var qty = EntityDialogs.PromptDecimal(Owner, "Transfer", $"From {locs[0].Code} to {locs[1].Code}\nQty for {products[0].Sku}", "1");
            if (qty is null or <= 0) return;
            await App.Api.TransferAsync(new TransferCreateDto
            {
                FromLocationId = from, ToLocationId = to, ProductId = products[0].Id, Quantity = qty.Value
            });
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Count_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var locs = await App.Api.GetLocationsAsync() ?? new();
            var products = await App.Api.GetProductsAsync() ?? new();
            if (locs.Count == 0 || products.Count == 0) return;
            var counted = EntityDialogs.PromptDecimal(Owner, "Cycle count", $"{products[0].Sku} at {locs[0].Code}", products[0].QuantityOnHand.ToString());
            if (counted is null) return;
            await App.Api.CycleCountAsync(new CycleCountCreateDto
            {
                LocationId = locs[0].Id, ProductId = products[0].Id, CountedQty = counted.Value, ReasonCode = "COUNT"
            });
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void AddLoc_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var code = EntityDialogs.FieldPrompt(Owner, "Location code", "BIN2");
            var name = EntityDialogs.FieldPrompt(Owner, "Location name", code ?? "");
            if (string.IsNullOrWhiteSpace(code)) return;
            await App.Api.CreateLocationAsync(new LocationDto { Code = code!, Name = name ?? code! });
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void BuildBom_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var boms = await App.Api.GetBomsAsync() ?? new();
            if (boms.Count == 0)
            {
                var products = await App.Api.GetProductsAsync() ?? new();
                if (products.Count < 2) { MessageBox.Show("Need products to define a BOM."); return; }
                await App.Api.CreateBomAsync(new BomDto
                {
                    ParentProductId = products[0].Id,
                    Name = products[0].Name + " kit",
                    Lines = { new BomLineDto { ComponentProductId = products[1].Id, Quantity = 1 } }
                });
                boms = await App.Api.GetBomsAsync() ?? new();
            }
            var bom = boms.First();
            var qty = EntityDialogs.PromptDecimal(Owner, "Build kit", $"Build {bom.Name}", "1");
            if (qty is null or <= 0) return;
            await App.Api.BuildBomAsync(new BomBuildDto { BomId = bom.Id, Quantity = qty.Value });
            await LoadAsync();
            MessageBox.Show("Kit built.", "Ledgerly");
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }
}
