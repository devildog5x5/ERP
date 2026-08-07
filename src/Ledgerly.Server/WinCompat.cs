using System;

namespace Ledgerly.Server;

public static class WinCompat
{
    public static void RequireWindows7OrLater()
    {
        var v = Environment.OSVersion.Version;
        // Windows 7 is 6.1
        if (v.Major < 6 || (v.Major == 6 && v.Minor < 1))
        {
            throw new PlatformNotSupportedException(
                $"Ledgerly requires Windows 7 SP1 or later. Detected {Environment.OSVersion.VersionString}.");
        }
    }
}
