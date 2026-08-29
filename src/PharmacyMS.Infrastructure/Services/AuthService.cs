using System.Net.Http;
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
    private readonly IEmailService _emailService;

    private static readonly char[] CodeChars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

    public AuthService(IUserRepository userRepository, AppDbContext context, IEmailService emailService)
    {
        _userRepository = userRepository;
        _context = context;
        _emailService = emailService;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;
        var user = await _userRepository.GetByUsernameAsync(username.Trim());
        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        await _userRepository.UpdateLastLoginAsync(user.Id);
        return user;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) return false;
        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE Users SET PasswordHash=@Hash, UpdatedAt=datetime('now') WHERE Id=@Id",
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
        if (user == null || string.IsNullOrWhiteSpace(user.SecurityAnswerHash)) return false;
        if (!BCrypt.Net.BCrypt.Verify(answer.Trim().ToLowerInvariant(), user.SecurityAnswerHash)) return false;
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

    public async Task<bool> UserExistsAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username.Trim());
        return user != null;
    }

    public async Task<string> GenerateRecoveryCodeAsync(int userId, string pharmacyName, string email)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new InvalidOperationException("User not found.");

        if (string.IsNullOrWhiteSpace(pharmacyName)) throw new InvalidOperationException("Pharmacy name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Email is required.");

        if (!await IsInternetAvailableAsync())
            throw new InvalidOperationException("An internet connection is required to generate a recovery code.");

        var code = $"PMSY-{RandomGroup()}-{RandomGroup()}-{RandomGroup()}";

        var supportEmail = "pharmaprofficial@gmail.com";
        var subject = $"[Recovery] {pharmacyName} ({email}) - PharmacyMS Code";
        var body = $"Pharmacy: {pharmacyName}\nClient email on file: {email}\n\nRecovery code:\n\n{code}\n\nKeep this safe. It will not be shown again.";

        var sent = await _emailService.SendAsync(supportEmail, subject, body);
        if (!sent)
            throw new InvalidOperationException("Failed to send the recovery code by email. Check your SMTP settings and try again.");

        user.RecoveryCodeHash = BCrypt.Net.BCrypt.HashPassword(code);
        await _userRepository.UpdateAsync(user);

        return code;
    }

    private static async Task<bool> IsInternetAvailableAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            var response = await client.GetAsync("https://www.gstatic.com/generate_204");
            return response.IsSuccessStatusCode || (int)response.StatusCode == 204;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ResetPasswordWithRecoveryCodeAsync(string username, string recoveryCode, string newPassword)
    {
        var user = await _userRepository.GetByUsernameAsync(username.Trim());
        if (user == null || string.IsNullOrWhiteSpace(user.RecoveryCodeHash)) return false;
        if (!BCrypt.Net.BCrypt.Verify(recoveryCode.Trim().ToUpperInvariant(), user.RecoveryCodeHash)) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.RecoveryCodeHash = null;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    private static string RandomGroup()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(4);
        var chars = new char[4];
        for (int i = 0; i < 4; i++)
            chars[i] = CodeChars[bytes[i] % CodeChars.Length];
        return new string(chars);
    }
}
