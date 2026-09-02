using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Services;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Desktop.Services;
using PharmacyMS.Desktop.Views.Auth;
using PharmacyMS.Desktop.Views.License;
using PharmacyMS.Desktop.Views.Plans;
using PharmacyMS.Desktop.Views.Onboarding;
using PharmacyMS.Desktop.Views.Splash;
using PharmacyMS.Desktop.Views.Startup;

namespace PharmacyMS.Desktop.Views.Shell;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();

        Opened += (_, _) =>
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen != null)
            {
                WindowState = WindowState.Normal;
                Width = screen.WorkingArea.Width;
                Height = screen.WorkingArea.Height;
                Position = screen.WorkingArea.Position;
            }
        };

        Closing += async (_, _) =>
        {
            if (SessionManager.CurrentSessionId is int sid)
            {
                var sessionRepo = Program.Services.GetRequiredService<IUserSessionRepository>();
                await sessionRepo.CloseSessionAsync(sid, DateTime.Now);
                SessionManager.CurrentSessionId = null;
            }
        };
    }

    public void ShowSplash(SplashView splash) => RootContent.Content = splash;

    public void ShowServerUnreachable(string reason, Func<Task> onRetry) =>
        RootContent.Content = new ServerUnreachableView(reason, onRetry);

    public void ShowPharmacySetup(Action onComplete) => RootContent.Content = new PharmacySetupView(onComplete);

    public void ShowLogin() => RootContent.Content = new LoginView(ShowMain);

    public void ShowLicenseEntry(bool preferMonthly = false) => RootContent.Content = new LicenseEntryView(ShowLogin, onBack: ShowPlans, preferMonthly: preferMonthly);

    // Plan choice is shown before license entry. All three buttons currently
    // just proceed to license entry - hook up real trial/monthly/annual
    // logic here once that's decided.
    public async void ShowPlans()
    {
        var settingsService = Program.Services?.GetService<IAppSettingsService>();
        var trialUsed = false;
        if (settingsService != null)
        {
            try { trialUsed = await settingsService.GetTrialUsedAsync(); }
            catch { /* default to not-used if the lookup fails */ }
        }

        RootContent.Content = new PlansView(
            onSelectTrial: StartTrial,
            onSelectMonthly: () => ShowLicenseEntry(preferMonthly: true),
            onSelectAnnual: () => ShowLicenseEntry(preferMonthly: false),
            trialAlreadyUsed: trialUsed);
    }

    // No license form for the trial - mint a real 30-day key and go straight in.
    // When it expires, the normal startup check will route back to ShowPlans().
    private async void StartTrial()
    {
        var trialKey = LicenseService.GenerateTrialLicenseKey();
        var settingsService = Program.Services?.GetService<IAppSettingsService>();
        if (settingsService != null)
        {
            await settingsService.SetLicenseKeyAsync(trialKey);
            await settingsService.SetTrialUsedAsync();
        }

        ShowLogin();
    }

    public void ShowMain() => RootContent.Content = new MainShell(ShowLogin);
}
