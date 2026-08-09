using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class CrmContactsView : UserControl
{
    private List<CrmAccountDto> _accounts = new();

    public CrmContactsView() => InitializeComponent();

    public static async Task<CrmContactsView> CreateAsync()
    {
        var view = new CrmContactsView();
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow;

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dto = EntityDialogs.EditCrmContact(OwnerWindow, null, _accounts);
            if (dto is null) return;
            await App.Api.CreateCrmContactAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmContactDto selected)
        {
            MessageBox.Show("Select a contact first.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dto = EntityDialogs.EditCrmContact(OwnerWindow, selected, _accounts);
            if (dto is null) return;
            await App.Api.UpdateCrmContactAsync(selected.Id, dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmContactDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.DisplayName)) return;
        try
        {
            await App.Api.DeleteCrmContactAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async Task LoadAsync()
    {
        _accounts = await App.Api.GetCrmAccountsAsync() ?? new List<CrmAccountDto>();
        Grid.ItemsSource = await App.Api.GetCrmContactsAsync();
    }
}
