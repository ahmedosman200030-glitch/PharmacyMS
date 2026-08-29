using Avalonia.Controls;
using PharmacyMS.Application.Interfaces.Services;

namespace PharmacyMS.Desktop.Views.Settings;

public partial class RecoveryCodeRequestWindow : Window
{
    private readonly IAppSettingsService _appSettingsService;

    public RecoveryCodeRequestWindow() : this(null!) { InitializeComponent(); }

    public RecoveryCodeRequestWindow(IAppSettingsService appSettingsService)
    {
        InitializeComponent();
        _appSettingsService = appSettingsService;

        GenerateButton.Click += (_, _) =>
        {
            var pharmacyName = PharmacyNameBox.Text?.Trim() ?? "";
            var email = EmailBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(pharmacyName) || string.IsNullOrWhiteSpace(email))
            {
                ErrorText.Text = "Pharmacy name and email are both required.";
                ErrorText.IsVisible = true;
                return;
            }

            Close((pharmacyName, email));
        };

        CancelButton.Click += (_, _) => Close(null);
    }

    public async Task LoadDefaultsAsync()
    {
        if (_appSettingsService == null) return;
        var name = await _appSettingsService.GetPharmacyNameAsync();
        var email = await _appSettingsService.GetRecoveryEmailAsync();
        if (!string.IsNullOrWhiteSpace(name)) PharmacyNameBox.Text = name;
        if (!string.IsNullOrWhiteSpace(email)) EmailBox.Text = email;
    }
}
