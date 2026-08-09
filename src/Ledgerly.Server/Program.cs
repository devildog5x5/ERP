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

        if (args.Length > 0 &&
            (args[0].Equals("set-db", StringComparison.OrdinalIgnoreCase) ||
             args[0].Equals("setdb", StringComparison.OrdinalIgnoreCase)))
            return RunSetDb(args.Skip(1).ToArray());

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
            Console.WriteLine("Database options: Sqlite (default), SqlServer, MySql, PostgreSql");
            Console.WriteLine("  Coalesce.Server.exe migrate --provider SqlServer --connection \"...\"");
            Console.WriteLine("  Coalesce.Server.exe set-db --provider MySql --connection \"...\"");

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
        var providerArg = GetArg(args, "--provider") ?? GetArg(args, "-p") ?? "SqlServer";
        var switchConfig = !HasFlag(args, "--no-switch");

        if (string.IsNullOrWhiteSpace(connection))
        {
            Console.Error.WriteLine("Missing --connection \"...\"");
            Console.Error.WriteLine();
            PrintHelp();
            return 1;
        }

        if (!ServerConfig.TryParseProvider(providerArg, out var target) || target == DatabaseProvider.Sqlite)
        {
            Console.Error.WriteLine("Invalid --provider. Use SqlServer, MySql, or PostgreSql.");
            return 1;
        }

        var config = ServerConfig.LoadOrCreate();
        Db.Configure(config);
        if (Db.Provider == DatabaseProvider.Sqlite)
            SQLitePCL.Batteries_V2.Init();

        Console.WriteLine($"Migrating Coalesce data -> {target}...");
        Console.WriteLine($"Source : {config.Describe()}");
        Console.WriteLine($"Target : {target}");
        Console.WriteLine();

        try
        {
            var result = PlatformMigrator.MigrateTo(target, connection, switchConfig);
            Console.WriteLine("Migration complete.");
            Console.WriteLine($"Config updated : {result.ConfigUpdated}");
            Console.WriteLine($"Config path    : {result.ConfigPath}");
            foreach (var kv in result.Counts)
                Console.WriteLine($"  {kv.Key}: {kv.Value}");
            Console.WriteLine();
            Console.WriteLine($"Next: start the server again. It will use {target} automatically.");
            Console.WriteLine("Keep a backup of the original database until you verify the new one.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Migration failed:");
            Console.Error.WriteLine(ex.Message);
            if (ex.InnerException != null)
                Console.Error.WriteLine(ex.InnerException.Message);
            return 2;
        }
    }

    private static int RunSetDb(string[] args)
    {
        var providerArg = GetArg(args, "--provider") ?? GetArg(args, "-p");
        var connection = GetArg(args, "--connection") ?? GetArg(args, "-c");
        var ensure = HasFlag(args, "--ensure-created") || HasFlag(args, "--ensure");

        if (string.IsNullOrWhiteSpace(providerArg) ||
            !ServerConfig.TryParseProvider(providerArg, out var provider))
        {
            Console.Error.WriteLine("Missing or invalid --provider (Sqlite, SqlServer, MySql, PostgreSql).");
            PrintHelp();
            return 1;
        }

        if (provider == DatabaseProvider.Sqlite)
        {
            if (string.IsNullOrWhiteSpace(connection))
                connection = $"Data Source={ServerConfig.DefaultSqlitePath}";
        }
        else if (string.IsNullOrWhiteSpace(connection))
        {
            Console.Error.WriteLine("Missing --connection \"...\" for non-SQLite providers.");
            PrintHelp();
            return 1;
        }

        try
        {
            if (provider == DatabaseProvider.Sqlite)
                SQLitePCL.Batteries_V2.Init();

            if (ensure || Db.IsServerDatabase(provider))
            {
                using var test = Db.Create(provider, connection!.Trim());
                test.Database.EnsureCreated();
                Console.WriteLine("Connection OK; schema ensured.");
            }

            var cfg = ServerConfig.LoadOrCreate();
            cfg.Provider = provider;
            cfg.ConnectionString = connection!.Trim();
            cfg.Save();
            Db.Configure(cfg);

            Console.WriteLine("server.json updated.");
            Console.WriteLine($"Provider : {cfg.Provider}");
            Console.WriteLine($"Database : {cfg.Describe()}");
            Console.WriteLine($"Config   : {ServerConfig.ConfigPath}");
            Console.WriteLine();
            Console.WriteLine("Restart Coalesce.Server.exe to use this database.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("set-db failed:");
            Console.Error.WriteLine(ex.Message);
            if (ex.InnerException != null)
                Console.Error.WriteLine(ex.InnerException.Message);
            return 2;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
Coalesce.Server

Usage:
  Coalesce.Server.exe
      Start the API using %LOCALAPPDATA%\Coalesce\Server\server.json
      Fresh databases get clean system defaults only (no demo catalog).

  Coalesce.Server.exe --demo
      Seed sample customers, suppliers, products, and POs when the catalog is empty.

  Coalesce.Server.exe migrate --provider <SqlServer|MySql|PostgreSql> --connection ""...""
      Copy the current database into an empty target database and switch server.json.

  Coalesce.Server.exe migrate --connection ""..."" --no-switch
      Copy data but leave server.json on the current provider.
      (--provider defaults to SqlServer when omitted.)

  Coalesce.Server.exe set-db --provider <Sqlite|SqlServer|MySql|PostgreSql> --connection ""...""
      Point server.json at a database (optionally create schema). Does not copy data.

Examples:
  Coalesce.Server.exe migrate --provider SqlServer --connection ""Server=localhost;Database=Coalesce;Trusted_Connection=True;TrustServerCertificate=True;""
  Coalesce.Server.exe migrate --provider MySql --connection ""Server=localhost;Port=3306;Database=coalesce;User=coalesce;Password=***;""
  Coalesce.Server.exe migrate --provider PostgreSql --connection ""Host=localhost;Port=5432;Database=coalesce;Username=coalesce;Password=***;""
  Coalesce.Server.exe set-db --provider MySql --connection ""Server=localhost;Database=coalesce;User=coalesce;Password=***;""

Notes:
  - Create an empty target database first (or allow the login to create it).
  - Migrate target must not already contain Coalesce business data.
  - You can also edit Provider and ConnectionString in server.json directly.
  - Client and API stay the same — only the database backend changes.
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
