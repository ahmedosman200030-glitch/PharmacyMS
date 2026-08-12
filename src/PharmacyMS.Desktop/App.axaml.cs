using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Desktop.Views.Auth;

namespace PharmacyMS.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new LoginView();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
