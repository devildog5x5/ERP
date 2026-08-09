using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Client.Services;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class UsersView : UserControl
{
    public UsersView() => InitializeComponent();

    public static async Task<UsersView> CreateAsync()
    {
        var v = new UsersView();
        await v.LoadAsync();
        return v;
    }

    private async Task LoadAsync()
    {
        Grid.ItemsSource = await App.Api.GetUsersAsync();
        RolesGrid.ItemsSource = await App.Api.GetRolesAsync();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!Session.IsAdministrator)
        {
            MessageBox.Show(
                "Only an Administrator can create users and assign access levels.",
                "Access denied",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
        try
        {
            var roles = await App.Api.GetRolesAsync() ?? new();
            var owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
            if (owner == null) return;
            var dto = EntityDialogs.EditUser(owner, roles);
            if (dto is null) return;
            await App.Api.CreateUserAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void ResetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (!Session.IsAdministrator)
        {
            MessageBox.Show("Only an Administrator can reset passwords.", "Access denied",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (Grid.SelectedItem is not UserDto selected)
        {
            MessageBox.Show("Select a user first.", "Coalesce.ERP.CRM", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
        if (owner == null) return;
        var password = EntityDialogs.FieldPrompt(owner, $"New password for {selected.UserName}", "changeme");
        if (string.IsNullOrWhiteSpace(password)) return;
        if (password.Trim().Length < 4)
        {
            MessageBox.Show("Password must be at least 4 characters.", "Coalesce.ERP.CRM",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await App.Api.ResetPasswordAsync(selected.Id, new ResetPasswordDto { NewPassword = password.Trim() });
            MessageBox.Show($"Password reset for {selected.UserName}.", "Coalesce.ERP.CRM",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (!Session.IsAdministrator)
        {
            MessageBox.Show(
                "Only an Administrator can delete users.",
                "Access denied",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
        if (Grid.SelectedItem is not UserDto selected)
        {
            MessageBox.Show("Select a user first.", "Coalesce.ERP.CRM", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (Session.Current != null && Session.Current.UserId == selected.Id)
        {
            MessageBox.Show("You cannot delete your own account while signed in.", "Coalesce.ERP.CRM",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
        if (owner == null) return;
        if (!EntityDialogs.ConfirmDelete(owner, $"user \"{selected.UserName}\"")) return;

        try
        {
            await App.Api.DeleteUserAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }
}
