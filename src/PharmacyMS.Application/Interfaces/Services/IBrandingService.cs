namespace PharmacyMS.Application.Interfaces.Services;

public class BrandingSettings
{
    public string AppName { get; set; } = "PharmaPro";
    public string? LogoPath { get; set; }
    public string? IconPath { get; set; }

    public string? PharmacyName { get; set; }
    public string? Tagline { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? ContactNumber { get; set; }
}

public interface IBrandingService
{
    Task<BrandingSettings> GetAsync();
    Task SetAppNameAsync(string appName);
    Task<string> SaveLogoAsync(Stream fileStream, string originalFileName);
    Task<string> SaveIconAsync(Stream fileStream, string originalFileName);
    Task SavePharmacyInfoAsync(string? pharmacyName, string? tagline, string? address, string? phoneNumber, string? mobileNumber, string? email, string? website, string? contactNumber);
}
