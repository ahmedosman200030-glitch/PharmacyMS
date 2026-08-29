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
        if (!File.Exists(_configPath))
            return new ResendConfig();
        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<ResendConfig>(json) ?? new ResendConfig();
        }
        catch { return new ResendConfig(); }
    }

    public void Save(ResendConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
