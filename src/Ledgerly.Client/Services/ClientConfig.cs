using System;
using System.IO;
using Newtonsoft.Json;

namespace Ledgerly.Client.Services;

public sealed class ClientConfig
{
    public string ApiBaseUrl { get; set; } = "http://127.0.0.1:8000/";

    private static string Path =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Coalesce", "Client", "config.json");

    private static string LegacyPath =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ledgerly", "Client", "config.json");

    public static ClientConfig Load()
    {
        try
        {
            MigrateFromLegacy();
            if (File.Exists(Path))
            {
                var cfg = JsonConvert.DeserializeObject<ClientConfig>(File.ReadAllText(Path));
                if (cfg != null && !string.IsNullOrWhiteSpace(cfg.ApiBaseUrl))
                {
                    try
                    {
                        cfg.ApiBaseUrl = ApiClient.NormalizeBaseAddress(cfg.ApiBaseUrl);
                        return cfg;
                    }
                    catch
                    {
                        /* fall through to defaults */
                    }
                }
            }
        }
        catch { /* use defaults */ }
        return new ClientConfig();
    }

    private static void MigrateFromLegacy()
    {
        try
        {
            if (File.Exists(Path) || !File.Exists(LegacyPath)) return;
            var dir = System.IO.Path.GetDirectoryName(Path)!;
            Directory.CreateDirectory(dir);
            File.Copy(LegacyPath, Path, overwrite: false);
        }
        catch { /* ignore */ }
    }

    public void Save()
    {
        var dir = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
