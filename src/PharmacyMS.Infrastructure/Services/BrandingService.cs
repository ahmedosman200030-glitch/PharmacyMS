using Dapper;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Services;

public class BrandingService : IBrandingService
{
    private const string AppNameKey = "Branding_AppName";
    private const string LogoPathKey = "Branding_LogoPath";
    private const string IconPathKey = "Branding_IconPath";
    private const string PharmacyNameKey = "Branding_PharmacyName";
    private const string TaglineKey = "Branding_Tagline";
    private const string AddressKey = "Branding_Address";
    private const string PhoneNumberKey = "Branding_PhoneNumber";
    private const string MobileNumberKey = "Branding_MobileNumber";
    private const string EmailKey = "Branding_Email";
    private const string WebsiteKey = "Branding_Website";
    private const string ContactNumberKey = "Branding_ContactNumber";

    private readonly AppDbContext _context;
    public BrandingService(AppDbContext context) => _context = context;

    public async Task<BrandingSettings> GetAsync()
    {
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<(string Key, string Value)>(
            "SELECT Key, Value FROM Settings WHERE Key IN (@A,@B,@C,@D,@E,@F,@G,@H,@I,@J,@K)",
            new { A=AppNameKey,B=LogoPathKey,C=IconPathKey,D=PharmacyNameKey,E=AddressKey,F=PhoneNumberKey,G=MobileNumberKey,H=EmailKey,I=WebsiteKey,J=TaglineKey,K=ContactNumberKey });
        var dict = rows.ToDictionary(r => r.Key, r => r.Value);
        return new BrandingSettings
        {
            AppName = dict.TryGetValue(AppNameKey, out var name) ? name : "PharmacyMS",
            LogoPath = dict.TryGetValue(LogoPathKey, out var logo) ? logo : null,
            IconPath = dict.TryGetValue(IconPathKey, out var icon) ? icon : null,
            PharmacyName = dict.TryGetValue(PharmacyNameKey, out var pn) ? pn : null,
            Tagline = dict.TryGetValue(TaglineKey, out var tag) ? tag : null,
            Address = dict.TryGetValue(AddressKey, out var addr) ? addr : null,
            PhoneNumber = dict.TryGetValue(PhoneNumberKey, out var ph) ? ph : null,
            MobileNumber = dict.TryGetValue(MobileNumberKey, out var mob) ? mob : null,
            Email = dict.TryGetValue(EmailKey, out var em) ? em : null,
            Website = dict.TryGetValue(WebsiteKey, out var web) ? web : null,
            ContactNumber = dict.TryGetValue(ContactNumberKey, out var cn) ? cn : null
        };
    }

    public async Task SetAppNameAsync(string appName)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("INSERT INTO Settings(Key,Value) VALUES(@Key,@Value) ON CONFLICT(Key) DO UPDATE SET Value=@Value",
            new { Key = AppNameKey, Value = appName });
    }

    public async Task<string> SaveLogoAsync(Stream fileStream, string originalFileName)
    {
        var path = await SaveBrandingFileAsync(fileStream, originalFileName, "logo");
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("INSERT INTO Settings(Key,Value) VALUES(@Key,@Value) ON CONFLICT(Key) DO UPDATE SET Value=@Value",
            new { Key = LogoPathKey, Value = path });
        return path;
    }

    public async Task<string> SaveIconAsync(Stream fileStream, string originalFileName)
    {
        var path = await SaveBrandingFileAsync(fileStream, originalFileName, "icon");
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("INSERT INTO Settings(Key,Value) VALUES(@Key,@Value) ON CONFLICT(Key) DO UPDATE SET Value=@Value",
            new { Key = IconPathKey, Value = path });
        return path;
    }

    public async Task SavePharmacyInfoAsync(string? pharmacyName, string? tagline, string? address, string? phoneNumber, string? mobileNumber, string? email, string? website, string? contactNumber)
    {
        using var conn = _context.CreateConnection();
        foreach (var (key, value) in new[] { (PharmacyNameKey,pharmacyName),(TaglineKey,tagline),(AddressKey,address),(PhoneNumberKey,phoneNumber),(MobileNumberKey,mobileNumber),(EmailKey,email),(WebsiteKey,website),(ContactNumberKey,contactNumber) })
            await conn.ExecuteAsync("INSERT INTO Settings(Key,Value) VALUES(@Key,@Value) ON CONFLICT(Key) DO UPDATE SET Value=@Value",
                new { Key = key, Value = value ?? "" });
    }

    private static async Task<string> SaveBrandingFileAsync(Stream fileStream, string originalFileName, string prefix)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PharmacyMS", "Branding");
        Directory.CreateDirectory(folder);
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
        var filePath = Path.Combine(folder, $"{prefix}{ext}");
        using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output);
        return filePath;
    }
}
