using System;
using System.IO;
using System.Text.Json;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Interfaces.Repositories;

namespace PharmacyMS.Infrastructure.Repositories;

public class JsonSoundSettingsRepository : ISoundSettingsRepository
{
    private readonly string _filePath;
    private SoundSettings? _cache;

    public JsonSoundSettingsRepository()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PharmaPro");

        if (OperatingSystem.IsMacOS())
        {
            appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "PharmaPro");
        }

        Directory.CreateDirectory(appDataDir);
        _filePath = Path.Combine(appDataDir, "sound-settings.json");
    }

    public SoundSettings Load()
    {
        if (_cache != null) return _cache;

        if (!File.Exists(_filePath))
        {
            _cache = new SoundSettings();
            Save(_cache);
            return _cache;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            _cache = JsonSerializer.Deserialize<SoundSettings>(json) ?? new SoundSettings();
        }
        catch
        {
            _cache = new SoundSettings();
        }

        return _cache;
    }

    public void Save(SoundSettings settings)
    {
        _cache = settings;
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
