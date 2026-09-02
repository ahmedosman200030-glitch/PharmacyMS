using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Desktop.Services;
using PharmacyMS.Desktop.Views.Shell;
using PharmacyMS.Desktop.Views.Splash;

namespace PharmacyMS.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += OnUnhandledUiException;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            _ = InitializeAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        ShellWindow shell = null!;
        SplashView splash = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            shell = new ShellWindow();
            desktop.MainWindow = shell;
            splash = new SplashView();
            shell.ShowSplash(splash);
            shell.Show();
        });

        // Fire-and-forget: retry any onboarding-notification emails that
        // couldn't be sent on a previous launch because the machine was
        // offline. Never awaited on the UI path, never blocks startup.
        _ = PharmacyMS.Desktop.Services.ResendEmailNotifier.RetryPendingNotificationsAsync();

        await splash.RunAsync(20, () => { });

        await ProceedFromSplashAsync(shell);
    }

    // Called after the splash animation, and again after a successful
    // "Server Unreachable" retry. If the database still can't be reached,
    // this blocks on the Server Unreachable screen instead of proceeding.
    private async Task ProceedFromSplashAsync(ShellWindow shell)
    {
        if (Program.DatabaseUnreachableReason != null)
        {
            var reason = Program.DatabaseUnreachableReason;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                shell.ShowServerUnreachable(reason, onRetry: async () =>
                {
                    await Program.TryReconnectAsync();
                    await ProceedFromSplashAsync(shell);
                });
            });
            return;
        }

        string? savedKey = null;
        bool setupCompleted = true;
        try
        {
            var settingsService = Program.Services.GetService<IAppSettingsService>();
            if (settingsService != null)
            {
                savedKey = await settingsService.GetLicenseKeyAsync().ConfigureAwait(false);
                setupCompleted = await settingsService.GetPharmacySetupCompletedAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // If settings/DB lookup fails, fall through and treat as unlicensed / setup-needed
            // rather than hanging or crashing startup.
        }

        var license = LicenseService.Validate(savedKey ?? "");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            void ProceedPastSetup()
            {
                if (license.IsValid)
                    shell.ShowLogin();
                else
                    shell.ShowPlans();
            }

            if (!setupCompleted)
                shell.ShowPharmacySetup(ProceedPastSetup);
            else
                ProceedPastSetup();
        });
    }

    private void OnUnhandledUiException(object? sender, Avalonia.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        try
        {
            var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { /* logging is best-effort */ }

        try
        {
            var dialog = new Avalonia.Controls.Window
            {
                Title = "Something went wrong",
                Width = 460,
                Height = 220,
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen,
                Content = new Avalonia.Controls.TextBlock
                {
                    Text = $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe app will keep running. If this keeps happening, please note what you were doing and report it.",
                    Margin = new Avalonia.Thickness(20),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            };
            dialog.Show();
        }
        catch { /* if the dialog itself fails, at least we already logged */ }
    }
}
