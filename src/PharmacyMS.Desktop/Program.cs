using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Infrastructure.DependencyInjection;
using Velopack;

namespace PharmacyMS.Desktop;

internal sealed class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    // Set when the app had to fall back from Postgres to local SQLite at startup.
    // App.axaml.cs checks this after the window is shown and warns the user.
    public static string? DatabaseFallbackReason { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var services = new ServiceCollection();
        services.AddInfrastructure();
        Services = services.BuildServiceProvider();

        var initializer = Services.GetRequiredService<PharmacyMS.Infrastructure.Data.DatabaseInitializer>();
        try
        {
            initializer.InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            var dbContext = Services.GetRequiredService<PharmacyMS.Infrastructure.Data.AppDbContext>();
            if (dbContext.IsPostgres)
            {
                Console.WriteLine($"[Startup] Postgres unavailable ({ex.Message}), falling back to SQLite.");
                DatabaseFallbackReason = ex.Message;

                var fallbackServices = new ServiceCollection();
                fallbackServices.AddInfrastructure(PharmacyMS.Infrastructure.Data.AppDbContext.DefaultSqliteConnectionString());
                Services = fallbackServices.BuildServiceProvider();
                var fallbackInit = Services.GetRequiredService<PharmacyMS.Infrastructure.Data.DatabaseInitializer>();
                fallbackInit.InitializeAsync().GetAwaiter().GetResult();
            }
            else
            {
                throw;
            }
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
