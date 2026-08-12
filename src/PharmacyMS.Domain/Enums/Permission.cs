namespace PharmacyMS.Domain.Enums;

[Flags]
public enum Permission : long
{
    None            = 0,
    ManageUsers     = 1L << 0,
    ManageMedicines = 1L << 1,
    ManageSales     = 1L << 2,
    ManagePurchases = 1L << 3,
    ViewReports     = 1L << 4,
    ManageCustomers = 1L << 5,
    ManageCategories= 1L << 6,
    ManageSuppliers = 1L << 7,
    AdjustStock     = 1L << 8,
    ManageSettings  = 1L << 9,
    BackupRestore   = 1L << 10,

    All = ManageUsers | ManageMedicines | ManageSales | ManagePurchases |
          ViewReports | ManageCustomers | ManageCategories | ManageSuppliers |
          AdjustStock | ManageSettings | BackupRestore
}
