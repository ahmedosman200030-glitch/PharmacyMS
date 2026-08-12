using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Application.Services;

/// <summary>
/// Holds the currently logged-in user for the lifetime of the session.
/// </summary>
public static class SessionManager
{
    public static User? CurrentUser { get; private set; }

    public static bool IsLoggedIn => CurrentUser != null;

    public static bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

    public static bool CanViewReports =>
        CurrentUser?.Role is UserRole.Admin or UserRole.Pharmacist;

    public static void Login(User user)
    {
        CurrentUser = user;
    }

    public static void Logout()
    {
        CurrentUser = null;
    }
}
