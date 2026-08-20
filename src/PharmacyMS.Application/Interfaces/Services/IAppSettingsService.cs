namespace PharmacyMS.Application.Interfaces.Services;

public interface IAppSettingsService
{
    Task<decimal> GetTaxRateAsync();
    Task SetTaxRateAsync(decimal rate);

    Task<string> GetCurrencySymbolAsync();
    Task SetCurrencySymbolAsync(string symbol);

    Task<string> GetInvoicePrefixAsync();
    Task SetInvoicePrefixAsync(string prefix);

    Task<string> GetReceiptFooterAsync();
    Task SetReceiptFooterAsync(string footer);

    Task<int> GetDefaultReorderLevelAsync();
    Task SetDefaultReorderLevelAsync(int level);

    Task<decimal> GetSlshExchangeRateAsync();
    Task SetSlshExchangeRateAsync(decimal rate);

    Task<string> GetLanguageAsync();
    Task SetLanguageAsync(string languageCode);

    Task<string?> GetLicenseKeyAsync();
    Task SetLicenseKeyAsync(string licenseKey);
}
