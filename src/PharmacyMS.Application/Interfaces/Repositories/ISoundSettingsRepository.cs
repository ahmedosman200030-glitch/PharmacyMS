using PharmacyMS.Application.DTOs;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface ISoundSettingsRepository
{
    SoundSettings Load();
    void Save(SoundSettings settings);
}
