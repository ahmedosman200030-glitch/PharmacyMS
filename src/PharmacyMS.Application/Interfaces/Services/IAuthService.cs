using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<string?> GetSecurityQuestionAsync(string username);
    Task<bool> ResetPasswordWithSecurityAnswerAsync(string username, string answer, string newPassword);
    Task SetSecurityQuestionAsync(int userId, string question, string answer);
}
