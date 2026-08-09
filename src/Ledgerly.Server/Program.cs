using System;
using System.Linq;
using System.Threading;
using Ledgerly.Server.Data;
using Microsoft.Owin.Hosting;

namespace Ledgerly.Server;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        WinCompat.RequireWindows7OrLater();

        if (args.Length > 0 && IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        if (args.Length > 0 && args[0].Equals("migrate", StringComparison.OrdinalIgnoreCase))
            return RunMigrate(args.Skip(1).ToArray());

        return RunServer(includeDemoData: HasFlag(args, "--demo"));
    }

    private static int RunServer(bool includeDemoData = false)
    {
        try
        {
            var config = ServerConfig.LoadOrCreate();
            Db.Configure(config);

            if (Db.Provider == DatabaseProvider.Sqlite)
                SQLitePCL.Batteries_V2.Init();

            DbSeeder.Seed(includeDemoData);

            Console.WriteLine("Coalesce.ERP.CRM API server (C# / .NET Framework 4.8)");
            Console.WriteLine("Compatible with Windows 7 SP1 and later");
            Console.WriteLine($"Provider : {Db.Provider}");
            Console.WriteLine($"Database : {config.Describe()}");
            Console.WriteLine($"Config   : {ServerConfig.ConfigPath}");
            Console.WriteLine($"Listening on {Db.ListenUrl}");
            Console.WriteLine("Press Ctrl+C to stop" +
                              (CanReadConsoleInput() ? ", or Enter." : "."));
            Console.WriteLine();
            Console.WriteLine("Grow later with:");
            Console.WriteLine("  Coalesce.Server.exe migrate --connection \"Server=.;Database=Coalesce;Trusted_Connection=True;\"");

            using (WebApp.Start<Startup>(Db.ListenUrl))
            {
                WaitForShutdown();
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Server failed to start:");
            Console.Error.WriteLine(ex.Message);
            if (ex.InnerException != null)
                Console.Error.WriteLine(ex.InnerException.Message);
            return 1;
        }
    }

    private static bool CanReadConsoleInput()
    {
        try
        {
            return Environment.UserInteractive && !Console.IsInputRedirected;
        }
        catch
        {
            return false;
        }
    }

    private static void WaitForShutdown()
    {
        using var exit = new ManualResetEvent(false);
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            exit.Set();
        };

        // Only wait on Enter when a real console is attached. Redirected/closed
        // stdin makes ReadLine return immediately and would stop the host.
        if (CanReadConsoleInput())
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { Console.ReadLine(); }
                catch { /* ignore */ }
                exit.Set();
            });
        }

        exit.WaitOne();
    }

    private static int RunMigrate(string[] args)
    {
        var connection = GetArg(args, "--connection") ?? GetArg(args, "-c");
        var switchConfig = !HasFlag(args, "--no-switch");

        if (string.IsNullOrWhiteSpace(connection))
        {
            Console.Error.WriteLine("Missing --connection \"...\"");
            Console.Error.WriteLine();
            PrintHelp();
            return 1;
        }

        var config = ServerConfig.LoadOrCreate();
        Db.Configure(config);
        if (Db.Provider == DatabaseProvider.Sqlite)
            SQLitePCL.Batteries_V2.Init();

        Console.WriteLine("Migrating Coalesce data â†’ SQL Server...");
        Console.WriteLine($"Source : {config.Describe()}");
        Console.WriteLine($"Target : SQL Server");
        Console.WriteLine();

        try
        {
            var result = PlatformMigrator.MigrateToSqlServer(connection, switchConfig);
            Console.WriteLine("Migration complete.");
            Console.WriteLine($"Config updated : {result.ConfigUpdated}");
            Console.WriteLine($"Config path    : {result.ConfigPath}");
            foreach (var kv in result.Counts)
                Console.WriteLine($"  {kv.Key}: {kv.Value}");
            Console.WriteLine();
            Console.WriteLine("Next: start the server again. It will use SQL Server automatically.");
            Console.WriteLine("Keep a backup of the original SQLite file until you verify the new database.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Migration failed:");
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"Coalesce.Server`n
Usage:
  Coalesce.Server.exe
      Start the API using %LOCALAPPDATA%\Coalesce\Server\server.json
      Fresh databases get clean system defaults only (no demo catalog).

  Coalesce.Server.exe --demo
      Same as above, but seed sample customers, suppliers, products, and POs
      when the product catalog is empty (for demos / training).

  Coalesce.Server.exe migrate --connection ""<sql-server-connection-string>""
      Copy current database to an empty SQL Server database and switch config.

  Coalesce.Server.exe migrate --connection ""..."" --no-switch
      Copy data but leave server.json on the current provider.

Examples:
  Coalesce.Server.exe migrate --connection ""Server=localhost;Database=Coalesce;Trusted_Connection=True;TrustServerCertificate=True;""
  Coalesce.Server.exe migrate --connection ""Server=db01;Database=Coalesce;User Id=coalesce;Password=***;""

Notes:
  - Create an empty SQL Server database first (or allow the login to create it).
  - Target must not already contain Coalesce data.
  - App code does not change â€” only the database provider in server.json.
");
    }

    private static bool IsHelp(string arg) =>
        arg is "-h" or "--help" or "/?" or "help";

    private static bool HasFlag(string[] args, string name) =>
        args.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string? GetArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}


