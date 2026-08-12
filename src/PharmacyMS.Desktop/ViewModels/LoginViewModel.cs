using System.Windows.Input;
using PharmacyMS.Application.Enums;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Application.Services;

namespace PharmacyMS.Desktop.ViewModels;

public class LoginViewModel
{
    private readonly IAuthService _authService;
    private readonly ISoundService _soundService;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public event Action? LoginSucceeded;

    public ICommand LoginCommand { get; }

    public LoginViewModel(IAuthService authService, ISoundService soundService)
    {
        _authService = authService;
        _soundService = soundService;
        LoginCommand = new RelayCommand(async () => await DoLoginAsync());
    }

    private async Task DoLoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter username and password.";
            _soundService.Play(SoundEvent.Error);
            return;
        }

        var user = await _authService.LoginAsync(Username, Password);

        if (user == null)
        {
            ErrorMessage = "Invalid username or password.";
            _soundService.Play(SoundEvent.Error);
            return;
        }

        SessionManager.Login(user);
        _soundService.Play(SoundEvent.AppStart);
        LoginSucceeded?.Invoke();
    }
}
