using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Desktop.Services;

namespace PharmacyMS.Desktop.Views.Onboarding;

public partial class PharmacySetupView : UserControl
{
    private readonly Action _onComplete;
    private readonly Action? _onBack;
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public PharmacySetupView(Action onComplete, Action? onBack = null)
    {
        InitializeComponent();
        _onComplete = onComplete;
        _onBack = onBack;

        // Hide the Back button on the first onboarding step if there's nowhere to go back to.
        BackButton.IsVisible = _onBack is not null;
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        _onBack?.Invoke();
    }

    // If the client types their number starting with a country code (e.g.
    // "+252634..." or "+1415..."), automatically switch the dropdown to that
    // country and strip the code from the text box, so it isn't duplicated
    // against the dropdown's own "+252" prefix.
    private bool _updatingPhoneText;

    private void OnPhoneTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (_updatingPhoneText)
            return;

        var text = PhoneNumberBox.Text ?? "";
        if (!text.StartsWith("+"))
            return;

        var digits = new string(text.Skip(1).TakeWhile(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return;

        ComboBoxItem? bestMatch = null;
        string? bestTag = null;

        foreach (var item in CountryCodePicker.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && digits.StartsWith(tag))
            {
                if (bestTag == null || tag.Length > bestTag.Length)
                {
                    bestTag = tag;
                    bestMatch = item;
                }
            }
        }

        if (bestMatch != null && bestTag != null)
        {
            _updatingPhoneText = true;
            CountryCodePicker.SelectedItem = bestMatch;
            var remainder = text.Substring(1 + bestTag.Length);
            PhoneNumberBox.Text = remainder;
            PhoneNumberBox.CaretIndex = remainder.Length;
            _updatingPhoneText = false;
        }
    }

    private async void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        HideError();

        var pharmacyName = PharmacyNameBox.Text?.Trim() ?? "";
        var ownerName = OwnerNameBox.Text?.Trim() ?? "";
        var phone = PhoneNumberBox.Text?.Trim() ?? "";
        var address = AddressBox.Text?.Trim() ?? "";
        var email = EmailBox.Text?.Trim() ?? "";

        if (pharmacyName.Length == 0 || ownerName.Length == 0 || phone.Length == 0 || address.Length == 0)
        {
            ShowError("Please fill in all required fields (marked with *).");
            return;
        }

        if (email.Length > 0 && !EmailRegex.IsMatch(email))
        {
            ShowError("Please enter a valid email address, or leave it blank.");
            return;
        }

        ContinueButton.IsEnabled = false;
        ContinueText.Text = "Saving...";

        try
        {
            var settings = Program.Services.GetRequiredService<IAppSettingsService>();
            var selectedCode = (CountryCodePicker.SelectedItem as ComboBoxItem)?.Tag as string ?? "252";
            var fullPhone = "+" + selectedCode + phone;

            await settings.SetPharmacyNameAsync(pharmacyName);
            await settings.SetOwnerNameAsync(ownerName);
            await settings.SetPhoneNumberAsync(fullPhone);
            await settings.SetPharmacyAddressAsync(address);
            if (email.Length > 0)
                await settings.SetRecoveryEmailAsync(email);

            ContinueText.Text = "Connecting...";

            // An internet connection is REQUIRED to finish setup: we must be
            // able to notify the PharmaPro team before this screen is allowed
            // to complete. This screen only ever runs once per install (new
            // clients, or existing installs backfilling missing info), so we
            // do not let it complete silently offline.
            var notified = await ResendEmailNotifier.NotifyPharmacyOnboardedAsync(
                pharmacyName, ownerName, fullPhone, address,
                email.Length > 0 ? email : null);

            if (!notified)
            {
                ShowError("An internet connection is required to complete setup. Please connect to the internet and click Continue again.");
                ContinueButton.IsEnabled = true;
                ContinueText.Text = "Continue";
                return;
            }

            await settings.SetPharmacySetupCompletedAsync();

            _onComplete();
        }
        catch (Exception ex)
        {
            ShowError($"Could not save your pharmacy information: {ex.Message}. Please try again.");
            ContinueButton.IsEnabled = true;
            ContinueText.Text = "Continue";
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.IsVisible = true;
    }

    private void HideError()
    {
        ErrorBanner.IsVisible = false;
    }
}
