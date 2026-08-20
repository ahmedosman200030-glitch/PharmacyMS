using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Desktop.Services;
using PharmacyMS.Desktop.Views.Auth;

namespace PharmacyMS.Desktop.Views.License;

public partial class LicenseEntryView : Window
{
    private readonly IAppSettingsService? _settingsService;

    private TextBox _licenseKeyBox = null!;
    private TextBlock _errorText = null!;

    public LicenseEntryView()
    {
        AvaloniaXamlLoader.Load(this);
        _settingsService = Program.Services?.GetService<IAppSettingsService>();

        _licenseKeyBox = this.FindControl<TextBox>("LicenseKeyBox")!;
        _errorText = this.FindControl<TextBlock>("ErrorText")!;
    }

    private async void OnActivateClick(object? sender, RoutedEventArgs e)
    {
        var key = _licenseKeyBox.Text ?? "";
        var result = PharmacyMS.Desktop.Services.LicenseService.Validate(key);

        if (!result.IsValid)
        {
            _errorText.Text = result.ErrorMessage ?? "Invalid license.";
            _errorText.IsVisible = true;
            return;
        }

        if (_settingsService != null)
            await _settingsService.SetLicenseKeyAsync(key.Trim());

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new LoginView();
            desktop.MainWindow.Show();
        }

        Close();
    }
}
