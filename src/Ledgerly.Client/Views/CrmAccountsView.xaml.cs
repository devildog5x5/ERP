using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class CrmAccountsView : UserControl
{
    private List<PartnerDto> _customers = new();

    public CrmAccountsView() => InitializeComponent();

    public static async Task<CrmAccountsView> CreateAsync()
    {
        var view = new CrmAccountsView();
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow;

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dto = EntityDialogs.EditCrmAccount(OwnerWindow, null, _customers);
            if (dto is null) return;
            await App.Api.CreateCrmAccountAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmAccountDto selected)
        {
            MessageBox.Show("Select an account first.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dto = EntityDialogs.EditCrmAccount(OwnerWindow, selected, _customers);
            if (dto is null) return;
            await App.Api.UpdateCrmAccountAsync(selected.Id, dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Link_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmAccountDto selected) return;
        try
        {
            await App.Api.LinkCrmAccountCustomerAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmAccountDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.Name)) return;
        try
        {
            await App.Api.DeleteCrmAccountAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async Task LoadAsync()
    {
        _customers = await App.Api.GetCustomersAsync() ?? new List<PartnerDto>();
        Grid.ItemsSource = await App.Api.GetCrmAccountsAsync();
    }
}
