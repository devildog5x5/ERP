using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class CrmLeadsView : UserControl
{
    public CrmLeadsView() => InitializeComponent();

    public static async Task<CrmLeadsView> CreateAsync()
    {
        var view = new CrmLeadsView();
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow;

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dto = EntityDialogs.EditCrmLead(OwnerWindow, null);
            if (dto is null) return;
            await App.Api.CreateCrmLeadAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmLeadDto selected)
        {
            MessageBox.Show("Select a lead first.", "Coalesce.ERP.CRM", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dto = EntityDialogs.EditCrmLead(OwnerWindow, selected);
            if (dto is null) return;
            await App.Api.UpdateCrmLeadAsync(selected.Id, dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Convert_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmLeadDto selected) return;
        if (selected.Status == "converted")
        {
            MessageBox.Show("This lead is already converted.", "Coalesce.ERP.CRM", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show(
                $"Convert \"{selected.Name}\" to a CRM account and ERP customer?",
                "Coalesce.ERP.CRM", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await App.Api.ConvertCrmLeadAsync(selected.Id, createCustomer: true);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmLeadDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.Name)) return;
        try
        {
            await App.Api.DeleteCrmLeadAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async Task LoadAsync() => Grid.ItemsSource = await App.Api.GetCrmLeadsAsync();
}
