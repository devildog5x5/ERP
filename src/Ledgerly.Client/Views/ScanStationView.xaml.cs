using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class ScanStationView : UserControl
{
    private readonly ObservableCollection<CartLine> _cart = new();
    private List<PoOption> _openPos = new();
    private decimal _taxRate;
    private bool _scanning;
    private bool _checkingOut;

    public ScanStationView()
    {
        InitializeComponent();
        CartGrid.ItemsSource = _cart;
    }

    public static async Task<ScanStationView> CreateAsync()
    {
        var view = new ScanStationView();
        await view.LoadAsync();
        return view;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ScanBox.Focus();
        Keyboard.Focus(ScanBox);
    }

    private async Task LoadAsync()
    {
        var settings = await App.Api.GetSettingsAsync();
        _taxRate = settings?.DefaultTaxRate ?? 0;

        var customers = await App.Api.GetCustomersAsync() ?? new List<PartnerDto>();
        CustomerCombo.ItemsSource = customers;
        if (customers.Count > 0) CustomerCombo.SelectedIndex = 0;

        var pos = await App.Api.GetPurchaseOrdersAsync() ?? new List<PurchaseOrderDto>();
        _openPos = pos
            .Where(p => p.Status == "ordered" || p.Status == "partial" || p.Status == "draft")
            .Select(p => new PoOption(p))
            .ToList();
        PoCombo.ItemsSource = _openPos;
        if (_openPos.Count > 0) PoCombo.SelectedIndex = 0;

        MovementGrid.ItemsSource = await App.Api.GetStockMovementsAsync();
        UpdateCartTotals();
    }

    private async void ScanBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || _scanning) return;
        e.Handled = true;
        var code = ScanBox.Text.Trim();
        ScanBox.Clear();
        if (string.IsNullOrWhiteSpace(code)) return;
        await HandleScanAsync(code);
        ScanBox.Focus();
    }

    private async Task HandleScanAsync(string code)
    {
        _scanning = true;
        ScanBox.IsEnabled = false;
        try
        {
            if (ModeLookup.IsChecked == true)
            {
                var product = await App.Api.GetProductByCodeAsync(code);
                ShowResult(true, product.Name, $"SKU {product.Sku} · UPC {product.Upc ?? "—"} · On hand {product.QuantityOnHand} · Price {product.SellPrice:C}");
            }
            else if (ModeAdjustIn.IsChecked == true)
            {
                var product = await App.Api.ScanAdjustAsync(new ScanAdjustDto { Code = code, QuantityDelta = 1 });
                ShowResult(true, $"+1 {product.Name}", $"On hand now {product.QuantityOnHand}");
                MovementGrid.ItemsSource = await App.Api.GetStockMovementsAsync();
            }
            else if (ModeAdjustOut.IsChecked == true)
            {
                var product = await App.Api.ScanAdjustAsync(new ScanAdjustDto { Code = code, QuantityDelta = -1 });
                ShowResult(true, $"-1 {product.Name}", $"On hand now {product.QuantityOnHand}");
                MovementGrid.ItemsSource = await App.Api.GetStockMovementsAsync();
            }
            else if (ModeReceive.IsChecked == true)
            {
                if (PoCombo.SelectedItem is not PoOption po)
                {
                    ShowResult(false, "Select an open PO", "Choose a purchase order before scanning to receive.");
                    return;
                }
                var updated = await App.Api.ScanReceiveAsync(new ScanReceiveDto
                {
                    PurchaseOrderId = po.Id,
                    Code = code,
                    Quantity = 1
                });
                ShowResult(true, $"Received on {updated.PoNumber}", $"Status {updated.Status} · Total {updated.Total:C}");
                await LoadAsync();
            }
            else if (ModeSale.IsChecked == true)
            {
                var product = await App.Api.GetProductByCodeAsync(code);
                var existing = _cart.FirstOrDefault(c => c.ProductId == product.Id);
                if (existing != null) existing.Quantity += 1;
                else
                {
                    _cart.Add(new CartLine
                    {
                        ProductId = product.Id,
                        Sku = product.Sku,
                        Name = product.Name,
                        Quantity = 1,
                        UnitPrice = product.SellPrice
                    });
                }
                CartGrid.Items.Refresh();
                UpdateCartTotals();
                ShowResult(true, $"Added {product.Name}", $"Cart lines {_cart.Count} · On hand {product.QuantityOnHand}");
            }
        }
        catch (Exception ex)
        {
            ShowResult(false, "Scan failed", ex.Message);
            System.Media.SystemSounds.Hand.Play();
        }
        finally
        {
            _scanning = false;
            ScanBox.IsEnabled = true;
        }
    }

    private async void Checkout_Click(object sender, RoutedEventArgs e)
    {
        if (_checkingOut) return;
        if (_cart.Count == 0)
        {
            MessageBox.Show("Cart is empty. Scan items in Quick sale mode.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (CustomerCombo.SelectedValue is not int customerId)
        {
            MessageBox.Show("Select a customer.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _checkingOut = true;
        if (sender is UIElement btn) btn.IsEnabled = false;
        try
        {
            var order = await App.Api.CreateSalesOrderAsync(new SalesOrderCreateDto
            {
                CustomerId = customerId,
                TaxRate = _taxRate,
                Notes = "Quick sale (scan station)",
                Lines = _cart.Select(c => new SalesOrderLineCreateDto
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = c.UnitPrice
                }).ToList()
            });
            if (order == null)
                throw new InvalidOperationException("Sale was not created.");
            _cart.Clear();
            UpdateCartTotals();
            MovementGrid.ItemsSource = await App.Api.GetStockMovementsAsync();
            ShowResult(true, $"Sold {order.OrderNumber}", $"Subtotal {order.Subtotal:C} · Tax {order.TaxAmount:C} · Total {order.Total:C}");
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
        finally
        {
            _checkingOut = false;
            if (sender is UIElement b) b.IsEnabled = true;
        }
    }

    private void ClearCart_Click(object sender, RoutedEventArgs e)
    {
        _cart.Clear();
        UpdateCartTotals();
    }

    private void UpdateCartTotals()
    {
        var sub = _cart.Sum(c => c.LineTotal);
        var tax = Math.Round(sub * (_taxRate / 100m), 2);
        CartTotals.Text = $"Subtotal {sub:C}  ·  Tax ({_taxRate:0.##}%) {tax:C}  ·  Total {sub + tax:C}";
    }

    private void ShowResult(bool ok, string title, string detail)
    {
        ResultPanel.Visibility = Visibility.Visible;
        ResultPanel.BorderBrush = ok
            ? new SolidColorBrush(Color.FromRgb(0x0D, 0x6B, 0x66))
            : new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
        ResultTitle.Text = title;
        ResultDetail.Text = detail;
        if (ok) System.Media.SystemSounds.Asterisk.Play();
    }

    private sealed class PoOption
    {
        public PoOption(PurchaseOrderDto po)
        {
            Id = po.Id;
            Label = $"{po.PoNumber} — {po.SupplierName} ({po.Status})";
        }
        public int Id { get; }
        public string Label { get; }
    }

    private sealed class CartLine
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
