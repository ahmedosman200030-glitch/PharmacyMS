using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Infrastructure.Data;
using PharmacyMS.Infrastructure.Repositories;
using PharmacyMS.Infrastructure.Services;

namespace PharmacyMS.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? connectionString = null)
    {
        var connStr = connectionString ?? AppDbContext.DefaultConnectionString();
        services.AddSingleton(new AppDbContext(connStr));
        services.AddSingleton<DatabaseInitializer>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IStockAdjustmentRepository, StockAdjustmentRepository>();
        services.AddScoped<IDailyClosingRepository, DailyClosingRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
        services.AddScoped<IBrandingService, BrandingService>();

        services.AddSingleton<ISoundSettingsRepository, JsonSoundSettingsRepository>();
        services.AddSingleton<ISoundService, SoundService>();
        return services;
    }
}
