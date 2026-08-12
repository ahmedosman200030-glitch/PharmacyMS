using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Infrastructure.DependencyInjection;
using Velopack;

namespace PharmacyMS.Desktop;

internal sealed class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var services = new ServiceCollection();
        services.AddInfrastructure();
        Services = services.BuildServiceProvider();

        // Initialize DB (create tables + seed admin user)
        var initializer = Services.GetRequiredService<PharmacyMS.Infrastructure.Data.DatabaseInitializer>();
        initializer.InitializeAsync().GetAwaiter().GetResult();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
