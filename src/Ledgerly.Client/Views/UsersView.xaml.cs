using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
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

    private async Task LoadAsync() => Grid.ItemsSource = await App.Api.GetUsersAsync();

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var roles = await App.Api.GetRolesAsync() ?? new();
            if (roles.Count == 0) return;
            var user = EntityDialogs.FieldPrompt(Window.GetWindow(this)!, "Username", "admin2");
            var display = EntityDialogs.FieldPrompt(Window.GetWindow(this)!, "Display name", user ?? "");
            var pass = EntityDialogs.FieldPrompt(Window.GetWindow(this)!, "Password", "changeme");
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass)) return;
            await App.Api.CreateUserAsync(new UserCreateDto
            {
                UserName = user!,
                DisplayName = display ?? user!,
                Password = pass!,
                RoleId = roles.First().Id
            });
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }
}
