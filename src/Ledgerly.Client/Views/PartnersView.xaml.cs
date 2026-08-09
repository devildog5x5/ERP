using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class PartnersView : UserControl
{
    private bool _suppliers;

    public PartnersView() => InitializeComponent();

    public static async Task<PartnersView> CreateAsync(bool suppliers)
    {
        var view = new PartnersView { _suppliers = suppliers };
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow;
    private string Kind => _suppliers ? "supplier" : "customer";

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dto = EntityDialogs.EditPartner(OwnerWindow, Kind, null);
            if (dto is null) return;
            if (_suppliers) await App.Api.CreateSupplierAsync(dto);
            else await App.Api.CreateCustomerAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not PartnerDto selected)
        {
            MessageBox.Show($"Select a {Kind} first.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dto = EntityDialogs.EditPartner(OwnerWindow, Kind, selected);
            if (dto is null) return;
            if (_suppliers) await App.Api.UpdateSupplierAsync(selected.Id, dto);
            else await App.Api.UpdateCustomerAsync(selected.Id, dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not PartnerDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.Name)) return;
        try
        {
            if (_suppliers) await App.Api.DeleteSupplierAsync(selected.Id);
            else await App.Api.DeleteCustomerAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async Task LoadAsync() =>
        Grid.ItemsSource = _suppliers
            ? await App.Api.GetSuppliersAsync()
            : await App.Api.GetCustomersAsync();
}
