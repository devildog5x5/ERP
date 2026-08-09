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

    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Coalesce", "Server");

    public static string LegacyConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ledgerly", "Server");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "server.json");

    public static string DefaultSqlitePath => Path.Combine(ConfigDirectory, "coalesce.db");

    public static ServerConfig LoadOrCreate()
    {
        MigrateFromLegacyAppData();
        Directory.CreateDirectory(ConfigDirectory);
        if (File.Exists(ConfigPath))
        {
            try
            {
                var loaded = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText(ConfigPath));
                if (loaded != null && !string.IsNullOrWhiteSpace(loaded.ConnectionString))
                    return loaded;
            }
            catch
            {
                // fall through to defaults
            }
        }

        var cfg = new ServerConfig
        {
            Provider = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={DefaultSqlitePath}",
            ListenUrl = "http://127.0.0.1:8000/"
        };
        cfg.Save();
        return cfg;
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
        var safe = ConnectionString;
        foreach (var key in new[] { "Password=", "Pwd=" })
        {
            var idx = safe.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var end = safe.IndexOf(';', idx);
                if (end < 0) end = safe.Length;
                safe = safe.Substring(0, idx + key.Length) + "***" + (end < safe.Length ? safe.Substring(end) : "");
            }
        }
        return $"SQL Server · {safe}";
    }
}
