using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Enums;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Application.Services;
using PharmacyMS.Desktop.Views.Shell;

namespace PharmacyMS.Desktop.Views.Auth;

public partial class LoginView : Window
{
    private bool _passwordVisible;

    public LoginView()
    {
        InitializeComponent();

        TogglePasswordButton.Click += (_, _) =>
        {
            _passwordVisible = !_passwordVisible;
            PasswordBox.PasswordChar = _passwordVisible ? '\0' : '●';
            TogglePasswordButton.Content = _passwordVisible ? "🙈" : "👁";
        };

        PasswordBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter) await LoginAsync();
        };

        LoginButton.Click += async (_, _) => await LoginAsync();

        ForgotPasswordText.PointerPressed += async (_, _) =>
        {
            var authService = Program.Services.GetRequiredService<IAuthService>();
            var win = new ForgotPasswordWindow(authService);
            await win.ShowDialog(this);
        };
    }

    private async Task LoginAsync()
    {
        ErrorText.IsVisible = false;

        var username = UsernameBox.Text?.Trim() ?? "";
        var password = PasswordBox.Text ?? "";

        var soundService = Program.Services.GetRequiredService<ISoundService>();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Enter your username and password.");
            soundService.Play(SoundEvent.Error);
            return;
        }

        var authService = Program.Services.GetRequiredService<IAuthService>();
        var user = await authService.LoginAsync(username, password);

        if (user == null)
        {
            ShowError("Invalid username or password.");
            soundService.Play(SoundEvent.Error);
            return;
        }

        SessionManager.Login(user);
        soundService.Play(SoundEvent.AppStart);
        new MainWindow().Show();
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
