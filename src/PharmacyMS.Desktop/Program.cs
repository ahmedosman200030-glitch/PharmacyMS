using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Infrastructure.DependencyInjection;
using Velopack;
using Velopack.Sources;

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

        _ = CheckForUpdatesAsync(); // fire-and-forget, won't block startup

        // Ensure the baked-in Resend API key is available to all services via env var.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RESEND_API_KEY")))
            Environment.SetEnvironmentVariable("RESEND_API_KEY", PharmacyMS.Desktop.Services.ResendApiKeyProvider.Key);

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

    private static async Task CheckForUpdatesAsync()
    {
        await PharmacyMS.Desktop.Services.UpdateService.CheckForUpdatesAsync();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
