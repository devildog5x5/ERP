using System;
using System.Windows;
using Ledgerly.Shared;

namespace Ledgerly.Client;

public static class WinCompat
{
    public static void RequireWindows7OrLater()
    {
        var v = Environment.OSVersion.Version;
        if (v.Major < 6 || (v.Major == 6 && v.Minor < 1))
        {
            MessageBox.Show(
                $"Coalesce requires Windows 7 SP1 or later.\nDetected: {Environment.OSVersion.VersionString}", Brand.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current?.Shutdown(1);
        }
    }
}
