using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Desktop.Services;

namespace PharmacyMS.Desktop.Views.License;

public partial class LicenseEntryView : UserControl
{
    private readonly IAppSettingsService? _settingsService;
    private readonly Action _onActivated;

    private TextBox _licenseKeyBox = null!;
    private TextBlock _errorText = null!;

    public LicenseEntryView(Action onActivated)
    {
        AvaloniaXamlLoader.Load(this);
        _onActivated = onActivated;
        _settingsService = Program.Services?.GetService<IAppSettingsService>();

        _licenseKeyBox = this.FindControl<TextBox>("LicenseKeyBox")!;
        _errorText = this.FindControl<TextBlock>("ErrorText")!;
    }

    private async void OnActivateClick(object? sender, RoutedEventArgs e)
    {
        var key = _licenseKeyBox.Text ?? "";
        var result = LicenseService.Validate(key);

        if (!result.IsValid)
        {
            _errorText.Text = result.ErrorMessage ?? "Invalid license.";
            _errorText.IsVisible = true;
            return;
        }

        if (_settingsService != null)
            await _settingsService.SetLicenseKeyAsync(key.Trim());

        _onActivated();
    }
}
