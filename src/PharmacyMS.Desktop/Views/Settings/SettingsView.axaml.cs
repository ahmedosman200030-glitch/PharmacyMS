using System.Globalization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Enums;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Desktop.Views.Auth;

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

        // ---- Sidebar navigation wiring ----
        _navPanels = new Dictionary<Button, Border>
        {
            [NavGeneral]  = PanelGeneral,
            [NavPharmacy] = PanelPharmacy,
            [NavTax]      = PanelTax,
            [NavSecurity] = PanelSecurity,
            [NavDatabase] = PanelDatabase,
            [NavSounds]   = PanelSounds,
            [NavAbout]    = PanelAbout,
        };

        foreach (var navButton in _navPanels.Keys)
        {
            navButton.Click += (_, _) => SelectSection(navButton);
        }

        SelectSection(NavPharmacy); // default active section

        AttachedToVisualTree += async (_, _) => await LoadAsync();

        UploadLogoButton.Click += async (_, _) => await UploadLogoAsync();
        ChangePasswordButton.Click += async (_, _) => await OpenChangePasswordAsync();
        BackupButton.Click += async (_, _) => await BackupAsync();
        RestoreButton.Click += async (_, _) => await RestoreAsync();
        SaveButton.Click += async (_, _) => await SaveAsync();

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
        AddressBox.Text = branding.Address;
        PhoneNumberBox.Text = branding.PhoneNumber;
        MobileNumberBox.Text = branding.MobileNumber;
        EmailBox.Text = branding.Email;
        WebsiteBox.Text = branding.Website;

        var taxRate = await _vm.LoadTaxRateAsync();
        TaxRateBox.Text = (taxRate * 100).ToString(CultureInfo.InvariantCulture);

        CurrencySymbolBox.Text = await _vm.LoadCurrencySymbolAsync();
        InvoicePrefixBox.Text = await _vm.LoadInvoicePrefixAsync();
        ReceiptFooterBox.Text = await _vm.LoadReceiptFooterAsync();
        ReorderLevelBox.Text = (await _vm.LoadDefaultReorderLevelAsync()).ToString();

        LoadSoundSettings();

        RefreshDatabaseInfo();
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

        if (string.IsNullOrWhiteSpace(AppNameBox.Text))
        {
            ShowError("App name is required.");
            return;
        }

        await _brandingService.SetAppNameAsync(AppNameBox.Text.Trim());

        await _brandingService.SavePharmacyInfoAsync(
            PharmacyNameBox.Text?.Trim(),
            AddressBox.Text?.Trim(),
            PhoneNumberBox.Text?.Trim(),
            MobileNumberBox.Text?.Trim(),
            EmailBox.Text?.Trim(),
            WebsiteBox.Text?.Trim());

        await _vm.SaveTaxRateAsync(taxPercent / 100m);
        await _vm.SaveCurrencySymbolAsync(CurrencySymbolBox.Text?.Trim() ?? "$");
        await _vm.SaveInvoicePrefixAsync(InvoicePrefixBox.Text?.Trim() ?? "INV-");
        await _vm.SaveReceiptFooterAsync(ReceiptFooterBox.Text?.Trim() ?? "");
        await _vm.SaveDefaultReorderLevelAsync(reorderLevel);

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
}
