using PharmacyMS.Application.Interfaces.Services;

namespace PharmacyMS.Desktop.ViewModels;

public class SettingsViewModel
{
    private readonly IAppSettingsService _settingsService;
    private readonly IDatabaseBackupService _backupService;

    public SettingsViewModel(IAppSettingsService settingsService, IDatabaseBackupService backupService)
    {
        _settingsService = settingsService;
        _backupService = backupService;
    }

    public Task<decimal> LoadTaxRateAsync() => _settingsService.GetTaxRateAsync();
    public Task SaveTaxRateAsync(decimal rate) => _settingsService.SetTaxRateAsync(rate);

    public Task<string> LoadCurrencySymbolAsync() => _settingsService.GetCurrencySymbolAsync();
    public Task SaveCurrencySymbolAsync(string symbol) => _settingsService.SetCurrencySymbolAsync(symbol);

    public Task<string> LoadInvoicePrefixAsync() => _settingsService.GetInvoicePrefixAsync();
    public Task SaveInvoicePrefixAsync(string prefix) => _settingsService.SetInvoicePrefixAsync(prefix);

    public Task<string> LoadReceiptFooterAsync() => _settingsService.GetReceiptFooterAsync();
    public Task SaveReceiptFooterAsync(string footer) => _settingsService.SetReceiptFooterAsync(footer);

    public Task<int> LoadDefaultReorderLevelAsync() => _settingsService.GetDefaultReorderLevelAsync();
    public Task SaveDefaultReorderLevelAsync(int level) => _settingsService.SetDefaultReorderLevelAsync(level);

    public Task<decimal> LoadSlshExchangeRateAsync() => _settingsService.GetSlshExchangeRateAsync();
    public Task SaveSlshExchangeRateAsync(decimal rate) => _settingsService.SetSlshExchangeRateAsync(rate);

    public Task<string> LoadLanguageAsync() => _settingsService.GetLanguageAsync();
    public Task SaveLanguageAsync(string lang) => _settingsService.SetLanguageAsync(lang);

    public Task<string?> LoadLicenseKeyAsync() => _settingsService.GetLicenseKeyAsync();

    public DatabaseInfo GetDatabaseInfo() => _backupService.GetDatabaseInfo();
    public Task<string> BackupAsync(string? destinationPath = null) => _backupService.BackupAsync(destinationPath);
    public Task RestoreAsync(string backupFilePath) => _backupService.RestoreAsync(backupFilePath);
}
