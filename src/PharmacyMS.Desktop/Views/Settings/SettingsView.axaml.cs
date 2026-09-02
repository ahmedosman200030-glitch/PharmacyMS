using System.Globalization;
using Avalonia.Controls;
using Npgsql;
using PharmacyMS.Infrastructure.Data;
using Avalonia.Platform.Storage;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Enums;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Desktop.Views.Auth;
using PharmacyMS.Application.Services;

namespace PharmacyMS.Desktop.Views.Settings;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _vm;
    private readonly IBrandingService _brandingService;
    private readonly ISoundSettingsRepository _soundSettingsRepo;
    private readonly ISoundService _soundService;
    private readonly Action? _onBrandingChanged;

    private Dictionary<CheckBox, SoundEvent> _soundChecks = new();
    private Dictionary<string, SoundEvent> _testSoundOptions = new();

    // Sidebar navigation
    private Dictionary<Button, Border> _navPanels = new();
    private string _currentLangForAbout = "en";

    public SettingsView() { InitializeComponent(); }
    public SettingsView(
        SettingsViewModel vm,
        IBrandingService brandingService,
        ISoundSettingsRepository soundSettingsRepo,
        ISoundService soundService,
        Action? onBrandingChanged = null)
    {
        InitializeComponent();
        _vm = vm;
        _brandingService = brandingService;
        _soundSettingsRepo = soundSettingsRepo;
        _soundService = soundService;
        _onBrandingChanged = onBrandingChanged;

        var appVersion = PharmacyMS.Desktop.Services.AppVersionService.GetVersion();
        SettingsVersionText.Text = appVersion;
        SettingsAppVersionText.Text = appVersion;
        SettingsFooterVersionText.Text = $"PharmaPro v{appVersion}";

        _soundChecks = new Dictionary<CheckBox, SoundEvent>
        {
            [TransactionSuccessCheck] = SoundEvent.TransactionSuccess,
            [ErrorSoundCheck]         = SoundEvent.Error,
            [WarningSoundCheck]       = SoundEvent.Warning,
            [ReceiptPrintCheck]       = SoundEvent.ReceiptPrint,
            [BackupCompleteCheck]     = SoundEvent.BackupComplete,
        };

        _testSoundOptions = new Dictionary<string, SoundEvent>
        {
            ["Transaction Success"] = SoundEvent.TransactionSuccess,
            ["Error"]                = SoundEvent.Error,
            ["Warning"]              = SoundEvent.Warning,
            ["Receipt Print"]        = SoundEvent.ReceiptPrint,
            ["Backup Complete"]      = SoundEvent.BackupComplete,
        };
        TestSoundCombo.ItemsSource = _testSoundOptions.Keys;
        TestSoundCombo.SelectedIndex = 0;

        LanguageCombo.ItemsSource = new[] { "English", "Somali" };

        // ---- Sidebar navigation wiring ----
        _navPanels = new Dictionary<Button, Border>
        {
            [NavGeneral]  = PanelGeneral,
            [NavPharmacy] = PanelPharmacy,
            [NavTax]      = PanelTax,
            [NavSecurity] = PanelSecurity,
            [NavDatabase]  = PanelDatabase,
            [NavCloudSync] = PanelCloudSync,
            [NavEmail]     = PanelEmail,
            [NavSounds]    = PanelSounds,
            [NavAbout]     = PanelAbout,
        };

        foreach (var navButton in _navPanels.Keys)
        {
            navButton.Click += (_, _) =>
            {
                SelectSection(navButton);
                if (navButton == NavCloudSync) LoadCloudSyncFields();
                if (navButton == NavEmail) LoadEmailFields();
            };
        }

        SelectSection(NavPharmacy); // default active section

        AttachedToVisualTree += async (_, _) => await LoadAsync();

        UploadLogoButton.Click += async (_, _) => await UploadLogoAsync();
        ChangePasswordButton.Click += async (_, _) => await OpenChangePasswordAsync();
        GenerateRecoveryCodeButton.Click += async (_, _) => await GenerateRecoveryCodeAsync();
        BackupButton.Click += async (_, _) => await BackupAsync();
        RestoreButton.Click += async (_, _) => await RestoreAsync();
        SaveButton.Click += async (_, _) => await SaveAsync();
        CheckUpdateButton.Click += async (_, _) => await CheckForUpdatesAsync();

        TestCloudConnectionButton.Click += async (_, _) => await TestCloudConnectionAsync();
        SaveCloudSyncButton.Click += async (_, _) => await SaveCloudSyncAsync();
        MigrateToCloudButton.Click += async (_, _) => await MigrateToCloudAsync();

        TestEmailButton.Click += async (_, _) => await TestEmailAsync();
        SaveEmailButton.Click += async (_, _) => await SaveEmailSettingsAsync();
        CloudSslModeCombo.ItemsSource = new[] { "Require", "Disable", "Prefer" };
        CloudSslModeCombo.SelectedIndex = 0;

        DbModeCombo.ItemsSource = new[] { "Offline (this PC only)", "Local Network (share with other PCs)", "Cloud (Supabase/Postgres)" };
        DbModeCombo.SelectionChanged += (_, _) => UpdateDbModeUi();

        SoundChecklistToggle.Click += (_, _) => SoundChecklistPopup.IsOpen = !SoundChecklistPopup.IsOpen;

        foreach (var check in _soundChecks.Keys)
        {
            check.IsCheckedChanged += (_, _) => UpdateSoundChecklistSummary();
        }

        SelectAllSoundsButton.Click += (_, _) =>
        {
            var allChecked = _soundChecks.Keys.All(c => c.IsChecked == true);
            foreach (var check in _soundChecks.Keys)
                check.IsChecked = !allChecked;
            SelectAllSoundsButton.Content = allChecked ? "Select All" : "Deselect All";
        };

        VolumeSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
                VolumeValueText.Text = $"{(int)VolumeSlider.Value}%";
        };

        TestSoundButton.Click += (_, _) =>
        {
            if (TestSoundCombo.SelectedItem is string key && _testSoundOptions.TryGetValue(key, out var evt))
                _soundService.TestSound(evt);
        };
    }

    private void SelectSection(Button selected)
    {
        foreach (var (navButton, panel) in _navPanels)
        {
            var isActive = navButton == selected;
            panel.IsVisible = isActive;
            if (isActive)
                navButton.Classes.Add("active");
            else
                navButton.Classes.Remove("active");
        }
    }

    private void LoadSoundSettings()
    {
        var settings = _soundSettingsRepo.Load();

        EnableSystemSoundsCheck.IsChecked = settings.EnableSystemSounds;
        TransactionSuccessCheck.IsChecked = settings.TransactionSuccessEnabled;
        ErrorSoundCheck.IsChecked = settings.ErrorEnabled;
        WarningSoundCheck.IsChecked = settings.WarningEnabled;
        ReceiptPrintCheck.IsChecked = settings.ReceiptPrintEnabled;
        BackupCompleteCheck.IsChecked = settings.BackupCompleteEnabled;

        VolumeSlider.Value = settings.Volume;
        VolumeValueText.Text = $"{settings.Volume}%";

        UpdateSoundChecklistSummary();
    }

    private void UpdateSoundChecklistSummary()
    {
        var total = _soundChecks.Count;
        var enabled = _soundChecks.Keys.Count(c => c.IsChecked == true);
        SoundChecklistSummaryText.Text = $"{enabled} of {total} sounds enabled";
    }

    private void SaveSoundSettings()
    {
        var settings = new SoundSettings
        {
            EnableSystemSounds = EnableSystemSoundsCheck.IsChecked == true,
            TransactionSuccessEnabled = TransactionSuccessCheck.IsChecked == true,
            ErrorEnabled = ErrorSoundCheck.IsChecked == true,
            WarningEnabled = WarningSoundCheck.IsChecked == true,
            ReceiptPrintEnabled = ReceiptPrintCheck.IsChecked == true,
            BackupCompleteEnabled = BackupCompleteCheck.IsChecked == true,
            ItemAddedEnabled = true,
            AppStartEnabled = true,
            LogoutEnabled = true,
            Volume = (int)VolumeSlider.Value,
        };
        _soundSettingsRepo.Save(settings);
    }

    private async Task LoadAsync()
    {
        var branding = await _brandingService.GetAsync();
        AppNameText_SetSafely(branding.AppName);
        if (!string.IsNullOrWhiteSpace(branding.LogoPath) && File.Exists(branding.LogoPath))
        {
            LogoPreview.Source = new Avalonia.Media.Imaging.Bitmap(branding.LogoPath);
        }

        PharmacyNameBox.Text = branding.PharmacyName;
        TaglineBox.Text = branding.Tagline;
        AddressBox.Text = branding.Address;
        PhoneNumberBox.Text = branding.PhoneNumber;
        MobileNumberBox.Text = branding.MobileNumber;
        EmailBox.Text = branding.Email;
        WebsiteBox.Text = branding.Website;
        ContactNumberBox.Text = branding.ContactNumber;

        var taxRate = await _vm.LoadTaxRateAsync();
        TaxRateBox.Text = (taxRate * 100).ToString(CultureInfo.InvariantCulture);

        CurrencySymbolBox.Text = await _vm.LoadCurrencySymbolAsync();
        InvoicePrefixBox.Text = await _vm.LoadInvoicePrefixAsync();
        ReceiptFooterBox.Text = await _vm.LoadReceiptFooterAsync();
        ReorderLevelBox.Text = (await _vm.LoadDefaultReorderLevelAsync()).ToString();
        SlshRateBox.Text = (await _vm.LoadSlshExchangeRateAsync()).ToString(CultureInfo.InvariantCulture);

        var lang = await _vm.LoadLanguageAsync();
        LanguageCombo.SelectedIndex = lang == "so" ? 1 : 0;
        _currentLangForAbout = lang;

        LoadSoundSettings();

        RefreshDatabaseInfo();
        LoadAboutInfo(_currentLangForAbout);
    }

    private async void LoadAboutInfo(string languageCode)
    {
        AboutLanguageText.Text = "English/Somali";
        AboutCurrencyText.Text = CurrencySymbolBox.Text ?? "$";

        var info = _vm.GetDatabaseInfo();
        AboutDbSizeText.Text = $"{info.SizeBytes / 1024.0:F1} KB";

        if (File.Exists(info.Path))
            AboutInstallDateText.Text = File.GetCreationTime(info.Path).ToString("dd MMM yyyy");

        AboutCurrentUserText.Text = SessionManager.CurrentUser?.FullName ?? "—";

        var savedLicenseKey = await _vm.LoadLicenseKeyAsync();
        var license = PharmacyMS.Desktop.Services.LicenseService.Validate(savedLicenseKey ?? "");
        AboutLicenseExpiryText.Text = license.ExpiryDate?.ToString("dd MMM yyyy") ?? "—";
    }

    private void AppNameText_SetSafely(string name)
    {
        AppNameBox.Text = name;
    }

    private void RefreshDatabaseInfo()
    {
        var info = _vm.GetDatabaseInfo();
        DbPathText.Text = info.Path;
        DbSizeText.Text = $"{info.SizeBytes / 1024.0:F1} KB";
        DbModifiedText.Text = info.LastModified == DateTime.MinValue
            ? "—"
            : info.LastModified.ToString("dd MMM yyyy HH:mm");
    }

    private async Task UploadLogoAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Logo",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count == 0) return;

        await using var stream = await files[0].OpenReadAsync();
        var savedPath = await _brandingService.SaveLogoAsync(stream, files[0].Name);
        LogoPreview.Source = new Avalonia.Media.Imaging.Bitmap(savedPath);

        ShowSuccess("Logo updated.");
        _onBrandingChanged?.Invoke();
    }

    private async Task OpenChangePasswordAsync()
    {
        var authService = Program.Services.GetRequiredService<IAuthService>();
        var vm = new ChangePasswordViewModel(authService);
        var window = new ChangePasswordWindow(vm);
        await window.ShowDialog(TopLevel.GetTopLevel(this) as Window);
    }

    private async Task GenerateRecoveryCodeAsync()
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        var appSettingsService = Program.Services.GetRequiredService<IAppSettingsService>();

        var requestWindow = new RecoveryCodeRequestWindow(appSettingsService);
        await requestWindow.LoadDefaultsAsync();
        var result = await requestWindow.ShowDialog<(string pharmacyName, string email)?>(owner);
        if (result == null) return;

        var authService = Program.Services.GetRequiredService<IAuthService>();
        var userId = SessionManager.CurrentUser.Id;

        try
        {
            var code = await authService.GenerateRecoveryCodeAsync(userId, result.Value.pharmacyName, result.Value.email);
            await appSettingsService.SetPharmacyNameAsync(result.Value.pharmacyName);
            await appSettingsService.SetRecoveryEmailAsync(result.Value.email);
            var window = new RecoveryCodeDisplayWindow(code);
            await window.ShowDialog(owner);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void LoadEmailFields()
    {
        var resendConfigService = Program.Services.GetRequiredService<ResendConfigService>();
        var config = resendConfigService.Load();

        ResendApiKeyBox.Text = config.ApiKey;

        EmailStatusText.IsVisible = false;
    }

    private Task SaveEmailSettingsAsync()
    {
        var resendConfigService = Program.Services.GetRequiredService<ResendConfigService>();

        var config = new ResendConfig
        {
            ApiKey = ResendApiKeyBox.Text?.Trim()
        };

        resendConfigService.Save(config);

        EmailStatusText.Text = "Email settings saved.";
        EmailStatusText.Foreground = Avalonia.Media.Brush.Parse("#16A34A");
        EmailStatusText.IsVisible = true;

        return Task.CompletedTask;
    }

    private async Task TestEmailAsync()
    {
        var testAddress = SmtpTestEmailBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(testAddress))
        {
            EmailStatusText.Text = "Enter an address to send the test email to.";
            EmailStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            EmailStatusText.IsVisible = true;
            return;
        }

        await SaveEmailSettingsAsync();

        var emailService = Program.Services.GetRequiredService<IEmailService>();
        var sent = await emailService.SendAsync(testAddress, "PharmacyMS Test Email",
            "This is a test email from PharmacyMS to confirm your email settings are working.");

        EmailStatusText.Text = sent
            ? "Test email sent successfully."
            : "Failed to send test email. Check your Resend API key above.";
        EmailStatusText.Foreground = Avalonia.Media.Brush.Parse(sent ? "#16A34A" : "#DC2626");
        EmailStatusText.IsVisible = true;
    }

    private async Task BackupAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var suggestedName = $"pharmacyms-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db";

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Database Backup",
            SuggestedFileName = suggestedName,
            DefaultExtension = "db",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Database Files") { Patterns = new[] { "*.db" } }
            }
        });

        if (file == null) return; // user cancelled

        try
        {
            var path = await _vm.BackupAsync(file.Path.LocalPath);
            RefreshDatabaseInfo();
            ShowDbStatus($"Backup saved to: {path}");
        }
        catch (Exception ex)
        {
            ShowDbStatus($"Backup failed: {ex.Message}");
        }
    }

    private async Task RestoreAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Backup File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Database Files") { Patterns = new[] { "*.db" } }
            }
        });

        if (files.Count == 0) return;

        try
        {
            await _vm.RestoreAsync(files[0].Path.LocalPath);
            RefreshDatabaseInfo();
            ShowDbStatus("Restore complete. Please restart the app for changes to take effect.");
        }
        catch (Exception ex)
        {
            ShowDbStatus($"Restore failed: {ex.Message}");
        }
    }

    private async Task SaveAsync()
    {
        ErrorText.IsVisible = false;
        SuccessText.IsVisible = false;

        if (!decimal.TryParse(TaxRateBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var taxPercent))
        {
            ShowError("Tax rate must be a number.");
            return;
        }

        if (!int.TryParse(ReorderLevelBox.Text, out var reorderLevel))
        {
            ShowError("Default reorder level must be a whole number.");
            return;
        }

        if (!decimal.TryParse(SlshRateBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var slshRate))
        {
            ShowError("SLSH exchange rate must be a number.");
            return;
        }

        if (string.IsNullOrWhiteSpace(AppNameBox.Text))
        {
            ShowError("App name is required.");
            return;
        }

        await _brandingService.SetAppNameAsync(AppNameBox.Text.Trim());

        await _brandingService.SavePharmacyInfoAsync(
            PharmacyNameBox.Text?.Trim(),
            TaglineBox.Text?.Trim(),
            AddressBox.Text?.Trim(),
            PhoneNumberBox.Text?.Trim(),
            MobileNumberBox.Text?.Trim(),
            EmailBox.Text?.Trim(),
            WebsiteBox.Text?.Trim(),
            ContactNumberBox.Text?.Trim());

        await _vm.SaveTaxRateAsync(taxPercent / 100m);
        await _vm.SaveCurrencySymbolAsync(CurrencySymbolBox.Text?.Trim() ?? "$");
        await _vm.SaveInvoicePrefixAsync(InvoicePrefixBox.Text?.Trim() ?? "INV-");
        await _vm.SaveReceiptFooterAsync(ReceiptFooterBox.Text?.Trim() ?? "");
        await _vm.SaveDefaultReorderLevelAsync(reorderLevel);
        await _vm.SaveSlshExchangeRateAsync(slshRate);
        await _vm.SaveLanguageAsync(LanguageCombo.SelectedIndex == 1 ? "so" : "en");

        SaveSoundSettings();

        _onBrandingChanged?.Invoke();
        ShowSuccess("Saved.");
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    private void ShowSuccess(string message)
    {
        SuccessText.Text = message;
        SuccessText.IsVisible = true;
    }

    private void ShowDbStatus(string message)
    {
        DbStatusText.Text = message;
        DbStatusText.IsVisible = true;
    }

    private void LoadCloudSyncFields()
    {
        var cfg = new DbConfigService().Load();
        DbModeCombo.SelectedIndex = cfg.NetworkMode switch
        {
            DbNetworkMode.LocalNetwork => 1,
            DbNetworkMode.Cloud => 2,
            _ => 0
        };

        if (!string.IsNullOrWhiteSpace(cfg.PostgresConnectionString))
        {
            try
            {
                var b = new NpgsqlConnectionStringBuilder(cfg.PostgresConnectionString);
                CloudHostBox.Text = b.Host;
                CloudPortBox.Text = b.Port.ToString();
                CloudDatabaseBox.Text = b.Database;
                CloudUsernameBox.Text = b.Username;
                CloudPasswordBox.Text = b.Password;
                CloudSslModeCombo.SelectedItem = b.SslMode.ToString();
            }
            catch
            {
                // malformed/legacy connection string — leave fields blank for the user to re-enter
            }
        }

        UpdateDbModeUi();
    }

    private void UpdateDbModeUi()
    {
        var mode = GetSelectedNetworkMode();
        var showPostgresFields = mode != DbNetworkMode.Offline;
        PostgresFieldsGrid.IsVisible = showPostgresFields;
        TestCloudConnectionButton.IsVisible = showPostgresFields;
        MigrateToCloudButton.IsVisible = showPostgresFields;

        DbModeHintText.Text = mode switch
        {
            DbNetworkMode.Offline => "This PC keeps its own local database. No other PC can see this data.",
            DbNetworkMode.LocalNetwork => "Connect to a PostgreSQL server on your local network (e.g. the pharmacy's main PC). Use its LAN IP address as Host, and set SSL Mode to Disable unless you have configured SSL yourself.",
            DbNetworkMode.Cloud => "Connect to a hosted Supabase/Postgres database over the internet.",
            _ => ""
        };
    }

    private DbNetworkMode GetSelectedNetworkMode() => DbModeCombo.SelectedIndex switch
    {
        1 => DbNetworkMode.LocalNetwork,
        2 => DbNetworkMode.Cloud,
        _ => DbNetworkMode.Offline
    };

    private string BuildCloudConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = CloudHostBox.Text,
            Port = int.TryParse(CloudPortBox.Text, out var p) ? p : 5432,
            Database = CloudDatabaseBox.Text,
            Username = CloudUsernameBox.Text,
            Password = CloudPasswordBox.Text,
            SslMode = Enum.TryParse<SslMode>(CloudSslModeCombo.SelectedItem as string, out var sm) ? sm : SslMode.Require,
            Timeout = 10
        };
        return builder.ConnectionString;
    }

    private async Task TestCloudConnectionAsync()
    {
        TestCloudConnectionButton.IsEnabled = false;
        CloudSyncStatusText.IsVisible = true;
        CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#64748B");
        CloudSyncStatusText.Text = "Testing connection...";

        var host = CloudHostBox.Text?.Trim() ?? "";
        var isCloudMode = GetSelectedNetworkMode() == DbNetworkMode.Cloud;

        if (isCloudMode && host.StartsWith("db.", StringComparison.OrdinalIgnoreCase) && host.Contains(".supabase.co"))
        {
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            CloudSyncStatusText.Text = "This looks like the direct-connection host, which usually won't resolve. " +
                "Use the pooler host instead, e.g. aws-0-<region>.pooler.supabase.com " +
                "(Supabase dashboard \u2192 Project Settings \u2192 Database \u2192 Connection pooling). " +
                "Also set Username to postgres.<project-ref>, not just postgres.";
            TestCloudConnectionButton.IsEnabled = true;
            return;
        }

        var username = CloudUsernameBox.Text?.Trim() ?? "";
        if (isCloudMode && username == "postgres" && host.Contains(".pooler.supabase.com"))
        {
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            CloudSyncStatusText.Text = "When using the pooler host, Username must be postgres.<project-ref>, not just postgres.";
            TestCloudConnectionButton.IsEnabled = true;
            return;
        }

        try
        {
            await using var conn = new NpgsqlConnection(BuildCloudConnectionString());
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync();

            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#16A34A");
            CloudSyncStatusText.Text = "Connection successful.";
        }
        catch (Exception ex)
        {
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            CloudSyncStatusText.Text = $"Connection failed: {ex.Message}";
        }
        finally
        {
            TestCloudConnectionButton.IsEnabled = true;
        }
    }

    private async Task SaveCloudSyncAsync()
    {
        SaveCloudSyncButton.IsEnabled = false;
        try
        {
            var mode = GetSelectedNetworkMode();
            var cfg = new DbConfig
            {
                Provider = mode == DbNetworkMode.Offline ? DbProvider.Sqlite : DbProvider.Postgres,
                NetworkMode = mode,
                PostgresConnectionString = mode == DbNetworkMode.Offline ? null : BuildCloudConnectionString()
            };

            new DbConfigService().Save(cfg);

            CloudSyncStatusText.IsVisible = true;
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#16A34A");
            CloudSyncStatusText.Text = "Saved. Restarting app...";

            await Task.Delay(800);
            RestartApp();
        }
        catch (Exception ex)
        {
            CloudSyncStatusText.IsVisible = true;
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            CloudSyncStatusText.Text = $"Save failed: {ex.Message}";
        }
        finally
        {
            SaveCloudSyncButton.IsEnabled = true;
        }
    }

    private async Task MigrateToCloudAsync()
    {
        MigrateToCloudButton.IsEnabled = false;
        CloudSyncStatusText.IsVisible = true;
        CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#64748B");
        CloudSyncStatusText.Text = "Migrating local data to cloud... this may take a moment.";

        try
        {
            var connString = BuildCloudConnectionString();
            var migrationService = new PharmacyMS.Infrastructure.Data.CloudMigrationService();
            var results = await migrationService.MigrateAsync(connString);

            var summary = string.Join(", ", results.Select(r => $"{r.Table}: {r.RowsCopied}"));
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#16A34A");
            CloudSyncStatusText.Text = $"Migration complete. {summary}";
        }
        catch (Exception ex)
        {
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            CloudSyncStatusText.Text = $"Migration failed: {ex.Message}";
        }
        finally
        {
            MigrateToCloudButton.IsEnabled = true;
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.IsVisible = true;
        UpdateStatusText.Foreground = Avalonia.Media.Brush.Parse("#64748B");
        UpdateStatusText.Text = "Checking for updates...";

        var result = await PharmacyMS.Desktop.Services.UpdateService.CheckForUpdatesAsync();

        UpdateStatusText.Text = result.Message ?? "";
        UpdateStatusText.Foreground = result.Status == PharmacyMS.Desktop.Services.UpdateCheckStatus.Error
            ? Avalonia.Media.Brush.Parse("#DC2626")
            : Avalonia.Media.Brush.Parse("#16A34A");

        CheckUpdateButton.IsEnabled = true;
    }

    private void RestartApp()
    {
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            System.Diagnostics.Process.Start(exePath);
        }
        Environment.Exit(0);
    }

}
