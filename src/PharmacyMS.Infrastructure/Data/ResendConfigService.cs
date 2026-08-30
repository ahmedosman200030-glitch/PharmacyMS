using System.Text.Json;

namespace PharmacyMS.Infrastructure.Data;

public class ResendConfigService
{
    private readonly string _configPath;

    public ResendConfigService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PharmaPro");
        Directory.CreateDirectory(folder);
        _configPath = Path.Combine(folder, "resend-config.json");
    }

    public ResendConfig Load()
    {
        ResendConfig config;
        if (!File.Exists(_configPath))
            config = new ResendConfig();
        else
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                config = JsonSerializer.Deserialize<ResendConfig>(json) ?? new ResendConfig();
            }
            catch { config = new ResendConfig(); }
        }

        // Fall back to environment variable if no API key is configured.
        // The build pipeline injects the key via RESEND_API_KEY at compile time.
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            config.ApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? "";

        return config;
    }

    public void Save(ResendConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
