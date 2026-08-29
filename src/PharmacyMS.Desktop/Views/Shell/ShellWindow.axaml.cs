using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Services;
using PharmacyMS.Desktop.Views.Auth;
using PharmacyMS.Desktop.Views.License;
using PharmacyMS.Desktop.Views.Onboarding;
using PharmacyMS.Desktop.Views.Splash;

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

    public void ShowPharmacySetup(Action onComplete) => RootContent.Content = new PharmacySetupView(onComplete);

    public void ShowLogin() => RootContent.Content = new LoginView(ShowMain);

    public void ShowLicenseEntry() => RootContent.Content = new LicenseEntryView(ShowLogin);

    public void ShowMain() => RootContent.Content = new MainShell(ShowLogin);
}
