using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? SecurityQuestion { get; set; }
    public string? SecurityAnswerHash { get; set; }
    public string? RecoveryCodeHash { get; set; }
    public string? Email { get; set; }
    public string? AvatarPath { get; set; }
    public Permission Permissions { get; set; } = Permission.None;
}
