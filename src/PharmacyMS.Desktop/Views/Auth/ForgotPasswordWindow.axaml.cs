using Avalonia.Controls;
using PharmacyMS.Application.Interfaces.Services;

namespace PharmacyMS.Desktop.Views.Auth;

public partial class ForgotPasswordWindow : Window
{
    private readonly IAuthService _authService;
    private string _username = "";
    private bool _hasSecurityQuestion;

    public ForgotPasswordWindow() { InitializeComponent(); }
    public ForgotPasswordWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;

        ContinueButton.Click += async (_, _) => await LoadOptionsAsync();
        ResetButton.Click += async (_, _) => await SubmitSecurityAnswerResetAsync();
        ResetButtonCode.Click += async (_, _) => await SubmitRecoveryCodeResetAsync();
        UseCodeInsteadLink.PointerPressed += (_, _) => ShowCodePanel();
        UseQuestionInsteadLink.PointerPressed += (_, _) => ShowQuestionPanel();
        CloseButton.Click += (_, _) => Close();
    }

    private async Task LoadOptionsAsync()
    {
        ErrorText.IsVisible = false;
        _username = UsernameBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(_username)) { ShowError("Enter your username."); return; }

        var exists = await _authService.UserExistsAsync(_username);
        if (!exists) { ShowError("Invalid user. Please check the username and try again."); return; }

        var question = await _authService.GetSecurityQuestionAsync(_username);
        _hasSecurityQuestion = !string.IsNullOrWhiteSpace(question);

        Step1Panel.IsVisible = false;

        if (_hasSecurityQuestion)
        {
            QuestionText.Text = question;
            ShowQuestionPanel();
        }
        else
        {
            // No security question set up — go straight to recovery code entry.
            ShowCodePanel();
        }
    }

    private void ShowQuestionPanel()
    {
        Step2Panel.IsVisible = true;
        Step2CodePanel.IsVisible = false;
        StepText.Text = "Answer your security question to set a new password.";
    }

    private void ShowCodePanel()
    {
        Step2Panel.IsVisible = false;
        Step2CodePanel.IsVisible = true;
        UseQuestionInsteadLink.IsVisible = _hasSecurityQuestion;
        StepText.Text = "Enter your recovery code to set a new password.";
    }

    private async Task SubmitSecurityAnswerResetAsync()
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

        ShowSuccess();
        Step2Panel.IsVisible = false;
    }

    private async Task SubmitRecoveryCodeResetAsync()
    {
        ErrorText.IsVisible = false;
        SuccessText.IsVisible = false;

        var code = RecoveryCodeBox.Text?.Trim() ?? "";
        var newPassword = NewPasswordBox2.Text ?? "";
        var confirm = ConfirmPasswordBox2.Text ?? "";

        if (string.IsNullOrWhiteSpace(code)) { ShowError("Enter your recovery code."); return; }
        if (newPassword.Length < 6) { ShowError("Password must be at least 6 characters."); return; }
        if (newPassword != confirm) { ShowError("Passwords do not match."); return; }

        var ok = await _authService.ResetPasswordWithRecoveryCodeAsync(_username, code, newPassword);
        if (!ok) { ShowError("That recovery code wasn't correct."); return; }

        ShowSuccess();
        Step2CodePanel.IsVisible = false;
    }

    private void ShowSuccess()
    {
        SuccessText.Text = "Password reset. You can now sign in with your new password.";
        SuccessText.IsVisible = true;
    }

    private void ShowError(string message) { ErrorText.Text = message; ErrorText.IsVisible = true; }
}
