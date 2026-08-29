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
        var dbConfigService = new DbConfigService();
        services.AddSingleton(dbConfigService);

        var smtpConfigService = new SmtpConfigService();
        services.AddSingleton(smtpConfigService);
        var resendConfigService = new ResendConfigService();
        services.AddSingleton(resendConfigService);

        AppDbContext dbContext;
        if (connectionString != null)
        {
            // Explicit override (e.g. tests) always wins, stays on SQLite.
            dbContext = new AppDbContext(DbProvider.Sqlite, connectionString);
        }
        else
        {
            var dbConfig = dbConfigService.Load();
            if (dbConfig.Provider == DbProvider.Postgres && !string.IsNullOrWhiteSpace(dbConfig.PostgresConnectionString))
            {
                dbContext = new AppDbContext(DbProvider.Postgres, dbConfig.PostgresConnectionString);
            }
            else
            {
                dbContext = new AppDbContext(DbProvider.Sqlite, AppDbContext.DefaultConnectionString());
            }
        }

        services.AddSingleton(dbContext);
        services.AddSingleton<DatabaseInitializer>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IStockAdjustmentRepository, StockAdjustmentRepository>();
        services.AddScoped<ISaleReturnRepository, SaleReturnRepository>();
        services.AddScoped<PharmacyMS.Infrastructure.Services.CodeGeneratorService>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IOtherIncomeRepository, OtherIncomeRepository>();
        services.AddScoped<IDailyClosingRepository, DailyClosingRepository>();
        services.AddScoped<IPendingSalePaymentRepository, PendingSalePaymentRepository>();
        services.AddScoped<IPendingExpenseRepository, PendingExpenseRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, SmtpEmailService>(); // SmtpEmailService now uses Resend internally
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IPurchaseOrderPdfService, PurchaseOrderPdfService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
        services.AddScoped<IBrandingService, BrandingService>();

        services.AddSingleton<ISoundSettingsRepository, JsonSoundSettingsRepository>();
        services.AddSingleton<ISoundService, SoundService>();
        return services;
    }
}
