using System.Text.Json;

namespace PharmacyMS.Infrastructure.Data;

public class DbConfigService
{
    private readonly string _configPath;

    public DbConfigService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PharmacyMS");
        Directory.CreateDirectory(folder);
        _configPath = Path.Combine(folder, "db-config.json");
    }

    public DbConfig Load()
    {
        if (!File.Exists(_configPath))
            return new DbConfig();

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<DbConfig>(json) ?? new DbConfig();
        }
        catch
        {
            return new DbConfig();
        }
    }

    public void Save(DbConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
