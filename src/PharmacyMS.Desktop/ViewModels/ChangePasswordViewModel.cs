using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Application.Services;

namespace PharmacyMS.Desktop.ViewModels;

public class ChangePasswordViewModel
{
    private readonly IAuthService _authService;

    public ChangePasswordViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<(bool Success, string? Error)> ChangeAsync(
        string currentPassword, string newPassword, string confirmPassword)
    {
        var userId = SessionManager.CurrentUser?.Id;
        if (userId == null)
            return (false, "No user is logged in.");

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            return (false, "All fields are required.");

        if (newPassword.Length < 6)
            return (false, "New password must be at least 6 characters.");

        if (newPassword != confirmPassword)
            return (false, "New password and confirmation do not match.");

        var success = await _authService.ChangePasswordAsync(userId.Value, currentPassword, newPassword);

        return success
            ? (true, null)
            : (false, "Current password is incorrect.");
    }
}
