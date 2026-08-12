using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly AppDbContext _context;

    public AuthService(IUserRepository userRepository, AppDbContext context)
    {
        _userRepository = userRepository;
        _context = context;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _userRepository.GetByUsernameAsync(username.Trim());
        if (user == null) return null;

        bool valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!valid) return null;

        await _userRepository.UpdateLastLoginAsync(user.Id);
        return user;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET PasswordHash = @Hash, UpdatedAt = datetime('now') WHERE Id = @Id",
            new { Hash = newHash, Id = userId });
        return true;
    }

    public async Task<string?> GetSecurityQuestionAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username.Trim());
        return user?.SecurityQuestion;
    }

    public async Task<bool> ResetPasswordWithSecurityAnswerAsync(string username, string answer, string newPassword)
    {
        var user = await _userRepository.GetByUsernameAsync(username.Trim());
        if (user == null || string.IsNullOrWhiteSpace(user.SecurityAnswerHash))
            return false;

        if (!BCrypt.Net.BCrypt.Verify(answer.Trim().ToLowerInvariant(), user.SecurityAnswerHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task SetSecurityQuestionAsync(int userId, string question, string answer)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return;

        user.SecurityQuestion = question;
        user.SecurityAnswerHash = BCrypt.Net.BCrypt.HashPassword(answer.Trim().ToLowerInvariant());
        await _userRepository.UpdateAsync(user);
    }
}
