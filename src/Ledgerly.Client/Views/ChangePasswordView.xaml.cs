using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Client.Services;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class ChangePasswordView : UserControl
{
    public ChangePasswordView() => InitializeComponent();

    public static Task<ChangePasswordView> CreateAsync() =>
        Task.FromResult(new ChangePasswordView());

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "";
        if (string.IsNullOrEmpty(CurrentBox.Password))
        {
            StatusText.Text = "Enter your current password.";
            return;
        }
        if (NewBox.Password.Trim().Length < 4)
        {
            StatusText.Text = "New password must be at least 4 characters.";
            return;
        }
        if (NewBox.Password != ConfirmBox.Password)
        {
            StatusText.Text = "New password and confirmation do not match.";
            return;
        }

        try
        {
            var result = await App.Api.ChangePasswordAsync(new ChangePasswordDto
            {
                CurrentPassword = CurrentBox.Password,
                NewPassword = NewBox.Password.Trim()
            });
            if (result == null || string.IsNullOrWhiteSpace(result.Token))
            {
                StatusText.Text = "Password change failed.";
                return;
            }

            Session.Current = result;
            App.Api.SetAuthToken(result.Token);
            CurrentBox.Password = "";
            NewBox.Password = "";
            ConfirmBox.Password = "";
            StatusText.Text = "Password updated.";
            MessageBox.Show("Your password was changed successfully.", "Coalesce",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }
}
