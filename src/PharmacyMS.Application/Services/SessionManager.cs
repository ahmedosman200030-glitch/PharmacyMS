using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Application.Services;

public static class SessionManager
{
    public static User? CurrentUser { get; private set; }

    public static bool IsLoggedIn => CurrentUser != null;

    public static bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

    // Role-based
    public static bool CanViewReports =>
        CurrentUser?.Role is UserRole.Admin or UserRole.Pharmacist;

    // Permission-based helpers
    public static bool Has(Permission p) =>
        IsAdmin || (CurrentUser?.Permissions.HasFlag(p) ?? false);

    public static bool CanManageUsers      => Has(Permission.ManageUsers);
    public static bool CanManageMedicines  => Has(Permission.ManageMedicines);
    public static bool CanManageSales      => Has(Permission.ManageSales);
    public static bool CanManagePurchases  => Has(Permission.ManagePurchases);
    public static bool CanManageCustomers  => Has(Permission.ManageCustomers);
    public static bool CanManageCategories => Has(Permission.ManageCategories);
    public static bool CanManageSuppliers  => Has(Permission.ManageSuppliers);
    public static bool CanAdjustStock      => Has(Permission.AdjustStock);
    public static bool CanManageSettings   => Has(Permission.ManageSettings);
    public static bool CanBackupRestore    => Has(Permission.BackupRestore);

    public static void Login(User user) => CurrentUser = user;
    public static void Logout() => CurrentUser = null;
}
