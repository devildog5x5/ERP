using System;
using System.Windows;
using System.Windows.Input;
using Ledgerly.Client.Services;
using Ledgerly.Shared;

namespace Ledgerly.Client;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        PassBox.Password = "admin";
        Loaded += (_, _) =>
        {
            PassBox.Focus();
            PassBox.SelectAll();
        };
        PassBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) Login_Click(this, new RoutedEventArgs()); };
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        try
        {
            var result = await App.Api.LoginAsync(new LoginRequestDto
            {
                UserName = UserBox.Text.Trim(),
                Password = PassBox.Password
            });
            if (result == null || string.IsNullOrWhiteSpace(result.Token))
            {
                ErrorText.Text = "Login failed.";
                return;
            }
            Session.Current = result;
            App.Api.SetAuthToken(result.Token);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }
}
