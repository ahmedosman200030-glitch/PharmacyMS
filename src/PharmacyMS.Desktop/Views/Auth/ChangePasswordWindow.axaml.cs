using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Auth;

public partial class ChangePasswordWindow : Window
{
    private readonly ChangePasswordViewModel _viewModel;

    public ChangePasswordWindow() { InitializeComponent(); }
    public ChangePasswordWindow(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        SaveButton.Click += async (_, _) => await Save();
    }

    private async Task Save()
    {
        ErrorText.IsVisible = false;
        SuccessText.IsVisible = false;

        var (success, error) = await _viewModel.ChangeAsync(
            CurrentPasswordBox.Text ?? string.Empty,
            NewPasswordBox.Text ?? string.Empty,
            ConfirmPasswordBox.Text ?? string.Empty);

        if (!success)
        {
            ErrorText.Text = error;
            ErrorText.IsVisible = true;
            return;
        }

        SuccessText.IsVisible = true;
        CurrentPasswordBox.Text = string.Empty;
        NewPasswordBox.Text = string.Empty;
        ConfirmPasswordBox.Text = string.Empty;
    }
}
