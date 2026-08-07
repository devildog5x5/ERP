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
            "Ledgerly", "Client", "config.json");

    public static ClientConfig Load()
    {
        try
        {
            if (File.Exists(Path))
            {
                var cfg = JsonConvert.DeserializeObject<ClientConfig>(File.ReadAllText(Path));
                if (cfg != null && !string.IsNullOrWhiteSpace(cfg.ApiBaseUrl))
                    return cfg;
            }
        }
        catch { /* use defaults */ }
        return new ClientConfig();
    }

    public void Save()
    {
        var dir = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
