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
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ledgerly", "Server");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "server.json");

    public static string DefaultSqlitePath => Path.Combine(ConfigDirectory, "ledgerly.db");

    public static ServerConfig LoadOrCreate()
    {
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

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public string Describe()
    {
        if (Provider == DatabaseProvider.Sqlite)
            return $"SQLite · {ConnectionString.Replace("Data Source=", "").Trim()}";
        // Avoid dumping passwords into the console/UI
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
