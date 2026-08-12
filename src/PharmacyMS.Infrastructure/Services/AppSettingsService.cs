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

    private const decimal DefaultTaxRate = 0.15m;
    private const string DefaultCurrencySymbol = "$";
    private const string DefaultInvoicePrefix = "INV-";
    private const string DefaultReceiptFooter = "Thank you for your purchase";
    private const int DefaultReorderLevelValue = 10;

    private readonly AppDbContext _context;

    public AppSettingsService(AppDbContext context)
    {
        _context = context;
    }

    private async Task<string?> GetRawAsync(string key)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT Value FROM Settings WHERE Key = @Key", new { Key = key });
    }

    private async Task SetRawAsync(string key, string value)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO Settings (Key, Value) VALUES (@Key, @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = @Value;",
            new { Key = key, Value = value });
    }

    public async Task<decimal> GetTaxRateAsync()
    {
        var value = await GetRawAsync(TaxRateKey);
        if (value == null) return DefaultTaxRate;
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)
            ? rate : DefaultTaxRate;
    }

    public Task SetTaxRateAsync(decimal rate) =>
        SetRawAsync(TaxRateKey, rate.ToString(CultureInfo.InvariantCulture));

    public async Task<string> GetCurrencySymbolAsync() =>
        await GetRawAsync(CurrencySymbolKey) ?? DefaultCurrencySymbol;

    public Task SetCurrencySymbolAsync(string symbol) =>
        SetRawAsync(CurrencySymbolKey, symbol);

    public async Task<string> GetInvoicePrefixAsync() =>
        await GetRawAsync(InvoicePrefixKey) ?? DefaultInvoicePrefix;

    public Task SetInvoicePrefixAsync(string prefix) =>
        SetRawAsync(InvoicePrefixKey, prefix);

    public async Task<string> GetReceiptFooterAsync() =>
        await GetRawAsync(ReceiptFooterKey) ?? DefaultReceiptFooter;

    public Task SetReceiptFooterAsync(string footer) =>
        SetRawAsync(ReceiptFooterKey, footer);

    public async Task<int> GetDefaultReorderLevelAsync()
    {
        var value = await GetRawAsync(DefaultReorderLevelKey);
        if (value == null) return DefaultReorderLevelValue;
        return int.TryParse(value, out var level) ? level : DefaultReorderLevelValue;
    }

    public Task SetDefaultReorderLevelAsync(int level) =>
        SetRawAsync(DefaultReorderLevelKey, level.ToString());
}
