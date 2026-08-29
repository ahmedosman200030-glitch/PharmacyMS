using System.Text.Json;

namespace PharmacyMS.Infrastructure.Data;

public class SmtpConfigService
{
    private readonly string _configPath;

    public SmtpConfigService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PharmaPro");
        Directory.CreateDirectory(folder);
        _configPath = Path.Combine(folder, "smtp-config.json");
    }

    public SmtpConfig Load()
    {
        if (!File.Exists(_configPath))
            return new SmtpConfig();

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<SmtpConfig>(json) ?? new SmtpConfig();
        }
        catch
        {
            return new SmtpConfig();
        }
    }

    public void Save(SmtpConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
