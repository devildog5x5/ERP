using System.Windows;
using Ledgerly.Client.Services;

namespace Ledgerly.Client;

public partial class App : Application
{
    public static ApiClient Api { get; private set; } = null!;
    public static ClientConfig Config { get; private set; } = null!;

    private void App_OnStartup(object sender, StartupEventArgs e)
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
        main.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Api?.Dispose();
        base.OnExit(e);
    }
}
