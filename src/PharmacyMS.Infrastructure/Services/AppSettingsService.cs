using System.Globalization;
using Dapper;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Services;

public class AppSettingsService : IAppSettingsService
{
    private const string TaxRateKey = "TaxRate";
    private const string CurrencySymbolKey = "CurrencySymbol";
    private const string InvoicePrefixKey = "InvoicePrefix";
    private const string ReceiptFooterKey = "ReceiptFooter";
    private const string DefaultReorderLevelKey = "DefaultReorderLevel";
    private const string SlshExchangeRateKey = "SlshExchangeRate";
    private const string LanguageKey = "AppLanguage";
    private const string LicenseKeyKey = "LicenseKey";
    private const string PharmacyNameKey = "PharmacyName";
    private const string RecoveryEmailKey = "RecoveryEmail";
    private const string OwnerNameKey = "OwnerName";
    private const string PhoneNumberKey = "PhoneNumber";
    private const string PharmacyAddressKey = "PharmacyAddress";
    private const string PharmacySetupCompletedKey = "PharmacySetupCompleted";

    private readonly AppDbContext _context;
    public AppSettingsService(AppDbContext context) => _context = context;

    private async Task<string?> GetRawAsync(string key)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string>("SELECT Value FROM Settings WHERE Key=@Key", new { Key = key });
    }

    private async Task SetRawAsync(string key, string value)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("INSERT INTO Settings(Key,Value) VALUES(@Key,@Value) ON CONFLICT(Key) DO UPDATE SET Value=@Value",
            new { Key = key, Value = value });
    }

    public async Task<decimal> GetTaxRateAsync()
    {
        var value = await GetRawAsync(TaxRateKey);
        return value != null && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ? rate : 0.15m;
    }

    public Task SetTaxRateAsync(decimal rate) => SetRawAsync(TaxRateKey, rate.ToString(CultureInfo.InvariantCulture));
    public async Task<string> GetCurrencySymbolAsync() => await GetRawAsync(CurrencySymbolKey) ?? "$";
    public Task SetCurrencySymbolAsync(string symbol) => SetRawAsync(CurrencySymbolKey, symbol);
    public async Task<string> GetInvoicePrefixAsync() => await GetRawAsync(InvoicePrefixKey) ?? "INV-";
    public Task SetInvoicePrefixAsync(string prefix) => SetRawAsync(InvoicePrefixKey, prefix);
    public async Task<string> GetReceiptFooterAsync() => await GetRawAsync(ReceiptFooterKey) ?? "Thank you for your purchase";
    public Task SetReceiptFooterAsync(string footer) => SetRawAsync(ReceiptFooterKey, footer);
    public async Task<int> GetDefaultReorderLevelAsync()
    {
        var value = await GetRawAsync(DefaultReorderLevelKey);
        return value != null && int.TryParse(value, out var level) ? level : 10;
    }
    public Task SetDefaultReorderLevelAsync(int level) => SetRawAsync(DefaultReorderLevelKey, level.ToString());

    public async Task<decimal> GetSlshExchangeRateAsync()
    {
        var value = await GetRawAsync(SlshExchangeRateKey);
        return value != null && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ? rate : 8500m;
    }
    public Task SetSlshExchangeRateAsync(decimal rate) => SetRawAsync(SlshExchangeRateKey, rate.ToString(CultureInfo.InvariantCulture));

    public async Task<string> GetLanguageAsync() => await GetRawAsync(LanguageKey) ?? "en";
    public Task SetLanguageAsync(string languageCode) => SetRawAsync(LanguageKey, languageCode);

    public Task<string?> GetLicenseKeyAsync() => GetRawAsync(LicenseKeyKey);
    public Task SetLicenseKeyAsync(string licenseKey) => SetRawAsync(LicenseKeyKey, licenseKey);

    public Task<string?> GetPharmacyNameAsync() => GetRawAsync(PharmacyNameKey);
    public Task SetPharmacyNameAsync(string name) => SetRawAsync(PharmacyNameKey, name);

    public Task<string?> GetRecoveryEmailAsync() => GetRawAsync(RecoveryEmailKey);
    public Task SetRecoveryEmailAsync(string email) => SetRawAsync(RecoveryEmailKey, email);

    public Task<string?> GetOwnerNameAsync() => GetRawAsync(OwnerNameKey);
    public Task SetOwnerNameAsync(string name) => SetRawAsync(OwnerNameKey, name);

    public Task<string?> GetPhoneNumberAsync() => GetRawAsync(PhoneNumberKey);
    public Task SetPhoneNumberAsync(string phone) => SetRawAsync(PhoneNumberKey, phone);

    public Task<string?> GetPharmacyAddressAsync() => GetRawAsync(PharmacyAddressKey);
    public Task SetPharmacyAddressAsync(string address) => SetRawAsync(PharmacyAddressKey, address);

    public async Task<bool> GetPharmacySetupCompletedAsync()
    {
        var value = await GetRawAsync(PharmacySetupCompletedKey);
        return value == "true";
    }
    public Task SetPharmacySetupCompletedAsync() => SetRawAsync(PharmacySetupCompletedKey, "true");
}
