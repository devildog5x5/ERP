using System;
using System.Windows;
using Ledgerly.Client.Services;
using Ledgerly.Shared;

namespace Ledgerly.Client;

public partial class LoginWindow : Window
{
    private bool _busy;

    public LoginWindow()
    {
        InitializeComponent();
        PassBox.Password = "admin";
        Loaded += (_, _) =>
        {
            PassBox.Focus();
            PassBox.SelectAll();
        };
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _busy = true;
        ErrorText.Text = "";
        SetBusy(true);
        try
        {
            var result = await App.Api.LoginAsync(new LoginRequestDto
            {
                UserName = UserBox.Text.Trim(),
                Password = PassBox.Password
            });
            if (!IsLoaded) return;
            if (result == null || string.IsNullOrWhiteSpace(result.Token))
            {
                ErrorText.Text = "Login failed.";
                return;
            }
            Session.Current = result;
            App.Api.SetAuthToken(result.Token);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            if (IsLoaded)
                ErrorText.Text = ex.Message;
        }
        finally
        {
            _busy = false;
            if (IsLoaded)
                SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        UserBox.IsEnabled = !busy;
        PassBox.IsEnabled = !busy;
        LoginBtn.IsEnabled = !busy;
    }
}
