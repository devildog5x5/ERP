using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class RemindersView : UserControl
{
    public RemindersView() => InitializeComponent();

    public static async Task<RemindersView> CreateAsync()
    {
        var view = new RemindersView();
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow;

    private async Task LoadAsync() => List.ItemsSource = await App.Api.GetRemindersAsync();

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dto = EntityDialogs.EditReminder(OwnerWindow, null);
            if (dto is null) return;
            await App.Api.CreateReminderAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is not ReminderDto selected)
        {
            MessageBox.Show("Select a reminder first.", "Coalesce.ERP.CRM", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dto = EntityDialogs.EditReminder(OwnerWindow, selected);
            if (dto is null) return;
            await App.Api.UpdateReminderAsync(selected.Id, dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is not ReminderDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.Title)) return;
        try
        {
            await App.Api.DeleteReminderAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Resolve_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is not null
            && int.TryParse(btn.Tag.ToString(), out var id))
        {
            try
            {
                await App.Api.ResolveReminderAsync(id);
                await LoadAsync();
            }
            catch (Exception ex) { EntityDialogs.ShowError(ex); }
        }
    }
}
