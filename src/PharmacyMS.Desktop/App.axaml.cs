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

        await splash.RunAsync(20, () => { });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            void ProceedPastSetup()
            {
                if (license.IsValid)
                    shell.ShowLogin();
                else
                    shell.ShowPlans();

                if (Program.DatabaseFallbackReason != null)
                {
                    ShowDatabaseFallbackWarning(shell, Program.DatabaseFallbackReason);
                }
            }

            if (!setupCompleted)
                shell.ShowPharmacySetup(ProceedPastSetup);
            else
                ProceedPastSetup();
        });
    }

    private void ShowDatabaseFallbackWarning(Avalonia.Controls.Window owner, string reason)
    {
        var dialog = new Avalonia.Controls.Window
        {
            Title = "Working Offline — Cloud Database Unreachable",
            Width = 520,
            Height = 260,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new Avalonia.Controls.StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 12,
                Children =
                {
                    new Avalonia.Controls.TextBlock
                    {
                        Text = "⚠ Could not connect to the cloud database (Supabase/Postgres).",
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        FontSize = 15,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new Avalonia.Controls.TextBlock
                    {
                        Text = "This session is using the LOCAL database on this PC only. " +
                               "Any sales, inventory changes, or other data you enter now will NOT be visible on other computers, " +
                               "and will need to be manually reconciled later.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new Avalonia.Controls.TextBlock
                    {
                        Text = $"Reason: {reason}",
                        FontSize = 11,
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B")),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new Avalonia.Controls.TextBlock
                    {
                        Text = "Check your internet connection, then go to Settings → Cloud Sync and click \"Save & Restart\" to try reconnecting.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    }
                }
            }
        };

        dialog.Show(owner);
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
