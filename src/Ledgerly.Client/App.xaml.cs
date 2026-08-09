using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Ledgerly.Client.Services;

namespace Ledgerly.Client;

public partial class App : Application
{
    public static ApiClient Api { get; private set; } = null!;
    public static ClientConfig Config { get; private set; } = null!;

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Keep the app alive while the login dialog is the only window; otherwise
        // OnLastWindowClose shuts down as soon as login succeeds and closes.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            Config = ClientConfig.Load();
            Api = new ApiClient(Config.ApiBaseUrl);

            var login = new LoginWindow();
            if (login.ShowDialog() != true || !Session.IsLoggedIn)
            {
                Shutdown();
                return;
            }

            var main = new MainWindow();
            MainWindow = main;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Ledgerly failed to start.\n\n" + ex.Message, "Coalesce.ERP.CRM",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "An unexpected error occurred.\n\n" + e.Exception.Message, "Coalesce.ERP.CRM",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var msg = e.ExceptionObject is Exception ex ? ex.Message : e.ExceptionObject?.ToString();
        MessageBox.Show(
            "A fatal error occurred.\n\n" + msg, "Coalesce.ERP.CRM",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        try
        {
            var detail = e.Exception.InnerException?.Message ?? e.Exception.Message;
            Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(
                    "A background error occurred.\n\n" + detail, "Coalesce.ERP.CRM",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }));
        }
        catch { /* ignore */ }
    }

    /// <summary>Show login again after a 401. Returns true if the user signed in.</summary>
    public static bool PromptRelogin(string? message = null)
    {
        MessageBox.Show(
            message ?? "Your session expired. Please sign in again.", "Coalesce.ERP.CRM",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        var previousMode = Current.ShutdownMode;
        Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            var login = new LoginWindow { Owner = Current.MainWindow };
            return login.ShowDialog() == true && Session.IsLoggedIn;
        }
        finally
        {
            if (Current.MainWindow != null)
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            else
                Current.ShutdownMode = previousMode;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Session.Clear();
        Api?.Dispose();
        base.OnExit(e);
    }
}
