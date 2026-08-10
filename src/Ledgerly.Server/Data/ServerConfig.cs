using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ledgerly.Server.Data;

public sealed class ServerConfig
{
    [JsonConverter(typeof(StringEnumConverter))]
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;

    public string ConnectionString { get; set; } = "";

    public string ListenUrl { get; set; } = "http://127.0.0.1:8000/";

    /// <summary>
    /// Planned database capacity in megabytes (set at install time). Used for SQLite fullness guidance.
    /// </summary>
    public long DatabaseSizeMb { get; set; } = 2048;

    /// <summary>Install profile label: Small | Medium | Large | Custom.</summary>
    public string CapacityProfile { get; set; } = "Medium";

    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Coalesce", "Server");

    public static string LegacyConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ledgerly", "Server");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "server.json");

    /// <summary>Written by the installer; merged into server.json on first server start.</summary>
    public static string CapacityOverlayPath => Path.Combine(ConfigDirectory, "capacity.json");

    public static string DefaultSqlitePath => Path.Combine(ConfigDirectory, "coalesce.db");

    public static long DefaultDatabaseSizeMb => 2048;

    public long PlannedCapacityBytes =>
        Math.Max(100L, DatabaseSizeMb <= 0 ? DefaultDatabaseSizeMb : DatabaseSizeMb) * 1024L * 1024L;

    public static ServerConfig LoadOrCreate()
    {
        MigrateFromLegacyAppData();
        Directory.CreateDirectory(ConfigDirectory);
        ServerConfig cfg;
        if (File.Exists(ConfigPath))
        {
            try
            {
                var loaded = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText(ConfigPath));
                if (loaded != null && !string.IsNullOrWhiteSpace(loaded.ConnectionString))
                {
                    cfg = loaded;
                    if (cfg.DatabaseSizeMb <= 0) cfg.DatabaseSizeMb = DefaultDatabaseSizeMb;
                    if (string.IsNullOrWhiteSpace(cfg.CapacityProfile)) cfg.CapacityProfile = "Medium";
                    if (MergeCapacityOverlay(cfg))
                        cfg.Save();
                    return cfg;
                }
            }
            catch
            {
                // fall through to defaults
            }
        }

        cfg = new ServerConfig
        {
            Provider = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={DefaultSqlitePath}",
            ListenUrl = "http://127.0.0.1:8000/",
            DatabaseSizeMb = DefaultDatabaseSizeMb,
            CapacityProfile = "Medium"
        };
        MergeCapacityOverlay(cfg);
        cfg.Save();
        return cfg;
    }

    /// <summary>
    /// Applies installer-written capacity.json (DatabaseSizeMb / CapacityProfile), then removes the overlay.
    /// </summary>
    private static bool MergeCapacityOverlay(ServerConfig cfg)
    {
        if (!File.Exists(CapacityOverlayPath)) return false;
        try
        {
            var overlay = JsonConvert.DeserializeObject<CapacityOverlay>(File.ReadAllText(CapacityOverlayPath));
            if (overlay == null || overlay.DatabaseSizeMb < 100) return false;
            cfg.DatabaseSizeMb = Math.Min(1024L * 1024L, overlay.DatabaseSizeMb); // cap 1 TB
            if (!string.IsNullOrWhiteSpace(overlay.CapacityProfile))
                cfg.CapacityProfile = overlay.CapacityProfile.Trim();
            try { File.Delete(CapacityOverlayPath); } catch { /* keep if locked */ }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class CapacityOverlay
    {
        public long DatabaseSizeMb { get; set; }
        public string? CapacityProfile { get; set; }
    }

    /// <summary>
    /// One-time copy of prior Ledgerly AppData into Coalesce so upgrades keep the company database.
    /// </summary>
    private static void MigrateFromLegacyAppData()
    {
        try
        {
            if (Directory.Exists(ConfigDirectory)) return;
            if (!Directory.Exists(LegacyConfigDirectory)) return;

            Directory.CreateDirectory(ConfigDirectory);
            foreach (var file in Directory.GetFiles(LegacyConfigDirectory))
            {
                var name = Path.GetFileName(file);
                var destName = name.Equals("ledgerly.db", StringComparison.OrdinalIgnoreCase) ? "coalesce.db" : name;
                var dest = Path.Combine(ConfigDirectory, destName);
                if (!File.Exists(dest))
                    File.Copy(file, dest, overwrite: false);
            }

            var cfgPath = Path.Combine(ConfigDirectory, "server.json");
            if (File.Exists(cfgPath))
            {
                var text = File.ReadAllText(cfgPath);
                var legacyDb = Path.Combine(LegacyConfigDirectory, "ledgerly.db").Replace("\\", "\\\\");
                var newDb = DefaultSqlitePath.Replace("\\", "\\\\");
                text = text.Replace(legacyDb, newDb);
                text = text.Replace(
                    Path.Combine(LegacyConfigDirectory, "ledgerly.db"),
                    DefaultSqlitePath);
                // Also handle single-backslash JSON paths
                text = text.Replace(
                    LegacyConfigDirectory.Replace("\\", "\\\\") + "\\\\ledgerly.db",
                    DefaultSqlitePath.Replace("\\", "\\\\"));
                if (text.IndexOf("ledgerly.db", StringComparison.OrdinalIgnoreCase) >= 0)
                    text = text.Replace("ledgerly.db", "coalesce.db");
                File.WriteAllText(cfgPath, text);
            }
        }
        catch
        {
            // Best-effort; fresh defaults if migration fails.
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public string Describe()
    {
        if (Provider == DatabaseProvider.Sqlite)
            return $"SQLite · {ConnectionString.Replace("Data Source=", "").Trim()}";
        var safe = MaskSecrets(ConnectionString);
        return Provider switch
        {
            DatabaseProvider.MySql => $"MySQL · {safe}",
            DatabaseProvider.PostgreSql => $"PostgreSQL · {safe}",
            _ => $"SQL Server · {safe}"
        };
    }

    public static string MaskSecrets(string connectionString)
    {
        var safe = connectionString ?? "";
        foreach (var key in new[] { "Password=", "Pwd=" })
        {
            var idx = safe.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var end = safe.IndexOf(';', idx);
            if (end < 0) end = safe.Length;
            safe = safe.Substring(0, idx + key.Length) + "***" + (end < safe.Length ? safe.Substring(end) : "");
        }
        return safe;
    }

    public static bool TryParseProvider(string? value, out DatabaseProvider provider)
    {
        provider = DatabaseProvider.Sqlite;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim();
        if (v.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            provider = DatabaseProvider.Sqlite;
            return true;
        }
        if (v.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("SQLServer", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("MSSQL", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("SQL Server", StringComparison.OrdinalIgnoreCase))
        {
            provider = DatabaseProvider.SqlServer;
            return true;
        }
        if (v.Equals("MySql", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("MySQL", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("MariaDB", StringComparison.OrdinalIgnoreCase))
        {
            provider = DatabaseProvider.MySql;
            return true;
        }
        if (v.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            provider = DatabaseProvider.PostgreSql;
            return true;
        }
        return Enum.TryParse(v, ignoreCase: true, out provider);
    }
}
