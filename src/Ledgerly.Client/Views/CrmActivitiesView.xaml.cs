using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class CrmActivitiesView : UserControl
{
    public CrmActivitiesView() => InitializeComponent();

    public static async Task<CrmActivitiesView> CreateAsync()
    {
        var view = new CrmActivitiesView();
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow;

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dto = EntityDialogs.EditCrmActivity(OwnerWindow, null);
            if (dto is null) return;
            await App.Api.CreateCrmActivityAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmActivityDto selected)
        {
            MessageBox.Show("Select an activity first.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dto = EntityDialogs.EditCrmActivity(OwnerWindow, selected);
            if (dto is null) return;
            await App.Api.UpdateCrmActivityAsync(selected.Id, dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Done_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmActivityDto selected) return;
        try
        {
            selected.Status = "done";
            await App.Api.UpdateCrmActivityAsync(selected.Id, selected);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmActivityDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.Subject)) return;
        try
        {
            await App.Api.DeleteCrmActivityAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async Task LoadAsync() => Grid.ItemsSource = await App.Api.GetCrmActivitiesAsync();
}
