using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Infrastructure.DependencyInjection;
using Velopack;
using Velopack.Sources;

namespace PharmacyMS.Desktop;

internal sealed class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    // Set when Postgres (Local Network or Cloud mode) can't be reached at startup.
    // The app does NOT fall back to local SQLite in this case - App.axaml.cs shows
    // a blocking "Server Unreachable" screen instead, with a Retry button that
    // calls TryReconnectAsync().
    public static string? DatabaseUnreachableReason { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        _ = CheckForUpdatesAsync(); // fire-and-forget, won't block startup

        // Ensure the baked-in Resend API key is available to all services via env var.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RESEND_API_KEY")))
            Environment.SetEnvironmentVariable("RESEND_API_KEY", PharmacyMS.Desktop.Services.ResendApiKeyProvider.Key);

        InitializeDatabase();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Builds the DI container and attempts to initialize the database.
    // For Postgres (Local Network / Cloud), failure does NOT fall back to SQLite -
    // it sets DatabaseUnreachableReason and leaves Services pointed at the
    // (uninitialized) Postgres context, so the UI can show a blocking retry screen.
    private static void InitializeDatabase()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        Services = services.BuildServiceProvider();

        var initializer = Services.GetRequiredService<PharmacyMS.Infrastructure.Data.DatabaseInitializer>();
        try
        {
            initializer.InitializeAsync().GetAwaiter().GetResult();
            DatabaseUnreachableReason = null;
        }
        catch (Exception ex)
        {
            var dbContext = Services.GetRequiredService<PharmacyMS.Infrastructure.Data.AppDbContext>();
            if (dbContext.IsPostgres)
            {
                Console.WriteLine($"[Startup] Postgres unavailable ({ex.Message}). Showing Server Unreachable screen.");
                DatabaseUnreachableReason = ex.Message;
            }
            else
            {
                throw;
            }
        }
    }

    // Called from the "Server Unreachable" screen's Retry button.
    // Rebuilds the DI container and tries the Postgres connection again.
    public static async Task<bool> TryReconnectAsync()
    {
        return await Task.Run(() =>
        {
            InitializeDatabase();
            return DatabaseUnreachableReason == null;
        });
    }

    private static async Task CheckForUpdatesAsync()
    {
        await PharmacyMS.Desktop.Services.UpdateService.CheckForUpdatesAsync();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new FontManagerOptions
            {
                FontFallbacks = new List<FontFallback>
                {
                    new FontFallback { FontFamily = new FontFamily("Apple Color Emoji") }
                }
            })
            .LogToTrace();
}
