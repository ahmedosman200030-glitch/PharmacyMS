namespace PharmacyMS.Domain.Entities;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public DateTime? LogoutTime { get; set; }
    public DateTime CreatedAt { get; set; }
}
