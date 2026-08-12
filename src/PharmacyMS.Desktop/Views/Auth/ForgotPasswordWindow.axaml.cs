using Avalonia.Controls;
using PharmacyMS.Application.Interfaces.Services;

namespace PharmacyMS.Desktop.Views.Auth;

public partial class ForgotPasswordWindow : Window
{
    private readonly IAuthService _authService;
    private string _username = "";

    public ForgotPasswordWindow() { InitializeComponent(); }
    public ForgotPasswordWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;

        ContinueButton.Click += async (_, _) => await LoadQuestionAsync();
        ResetButton.Click += async (_, _) => await SubmitResetAsync();
        CloseButton.Click += (_, _) => Close();
    }

    private async Task LoadQuestionAsync()
    {
        ErrorText.IsVisible = false;
        _username = UsernameBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(_username)) { ShowError("Enter your username."); return; }

        var question = await _authService.GetSecurityQuestionAsync(_username);
        if (string.IsNullOrWhiteSpace(question))
        {
            ShowError("No recovery option is set up for this account. Contact your administrator.");
            return;
        }

        QuestionText.Text = question;
        Step1Panel.IsVisible = false;
        Step2Panel.IsVisible = true;
        StepText.Text = "Answer your security question to set a new password.";
    }

    private async Task SubmitResetAsync()
    {
        ErrorText.IsVisible = false;
        SuccessText.IsVisible = false;

        var answer = AnswerBox.Text?.Trim() ?? "";
        var newPassword = NewPasswordBox.Text ?? "";
        var confirm = ConfirmPasswordBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(answer)) { ShowError("Enter your answer."); return; }
        if (newPassword.Length < 6) { ShowError("Password must be at least 6 characters."); return; }
        if (newPassword != confirm) { ShowError("Passwords do not match."); return; }

        var ok = await _authService.ResetPasswordWithSecurityAnswerAsync(_username, answer, newPassword);
        if (!ok) { ShowError("That answer wasn't correct."); return; }

        SuccessText.Text = "Password reset. You can now sign in with your new password.";
        SuccessText.IsVisible = true;
        Step2Panel.IsVisible = false;
    }

    private void ShowError(string message) { ErrorText.Text = message; ErrorText.IsVisible = true; }
}
