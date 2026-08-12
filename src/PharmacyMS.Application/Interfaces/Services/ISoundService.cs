using PharmacyMS.Application.Enums;

namespace PharmacyMS.Application.Interfaces.Services;

public interface ISoundService
{
    void Play(SoundEvent soundEvent);
    void TestSound(SoundEvent soundEvent);
}
