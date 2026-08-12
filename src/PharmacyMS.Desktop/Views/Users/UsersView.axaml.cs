using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Desktop.Views.Users;

public partial class UsersView : UserControl
{
    private readonly UsersViewModel _vm;
    private User? _editingUser;
    private bool _isEdit;

    public UsersView() { InitializeComponent(); }
    public UsersView(UsersViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Grid.ItemsSource = _vm.Users;
        AttachedToVisualTree += async (_, _) =>
        {
            await _vm.LoadAsync();
            RefreshStats();
        };

        AddButton.Click += (_, _) => OpenPanel(null);
        EditButton.Click += (_, _) =>
        {
            if (Grid.SelectedItem is User u) OpenPanel(u);
        };
        DeactivateButton.Click += async (_, _) =>
        {
            if (Grid.SelectedItem is User u)
            {
                if (u.IsActive)
                    await _vm.DeactivateAsync(u);
                else
                    await _vm.ActivateAsync(u);
                Grid.ItemsSource = null;
                Grid.ItemsSource = _vm.Users;
                RefreshStats();
                UpdateDeactivateButton();
            }
        };

        Grid.SelectionChanged += (_, _) => UpdateDeactivateButton();

        CloseButton.Click += (_, _) => ClosePanel();
        CancelButton.Click += (_, _) => ClosePanel();
        SaveButton.Click += async (_, _) => await SaveAsync();
        Scrim.PointerPressed += (_, _) => ClosePanel();

        SearchBox.TextChanged += (_, _) => ApplyFilter();
    }

    private void RefreshStats()
    {
        var list = _vm.Users.ToList();
        TotalCountText.Text = list.Count.ToString();
        ActiveCountText.Text = $"{list.Count(u => u.IsActive)} Active";
        InactiveCountText.Text = $"{list.Count(u => !u.IsActive)} Inactive";
    }

    private void UpdateDeactivateButton()
    {
        if (Grid.SelectedItem is User u)
        {
            DeactivateButton.IsEnabled = true;
            DeactivateButton.Content = u.IsActive ? "Deactivate Selected" : "Activate Selected";
            DeactivateButton.Foreground = u.IsActive
                ? Avalonia.Media.Brush.Parse("#EF4444")
                : Avalonia.Media.Brush.Parse("#22C55E");
        }
        else
        {
            DeactivateButton.IsEnabled = false;
            DeactivateButton.Content = "Deactivate Selected";
            DeactivateButton.Foreground = Avalonia.Media.Brush.Parse("#EF4444");
        }
    }

    private void ApplyFilter()
    {
        var q = SearchBox.Text?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(q))
        {
            Grid.ItemsSource = _vm.Users;
            return;
        }
        Grid.ItemsSource = _vm.Users
            .Where(u => u.FullName.ToLowerInvariant().Contains(q) || u.Username.ToLowerInvariant().Contains(q))
            .ToList();
    }

    private void OpenPanel(User? existing)
    {
        _editingUser = existing;
        _isEdit = existing != null;
        ErrorText.IsVisible = false;
        PasswordBox.Text = "";

        if (existing != null)
        {
            PanelTitleText.Text = "Edit User";
            UsernameBox.Text = existing.Username;
            UsernameBox.IsEnabled = false;
            FullNameBox.Text = existing.FullName;
            RoleBox.SelectedIndex = (int)existing.Role;
            PermManageUsers.IsChecked = existing.Permissions.HasFlag(Permission.ManageUsers);
            PermManageMedicines.IsChecked = existing.Permissions.HasFlag(Permission.ManageMedicines);
            PermManageSales.IsChecked = existing.Permissions.HasFlag(Permission.ManageSales);
            PermManagePurchases.IsChecked = existing.Permissions.HasFlag(Permission.ManagePurchases);
            PermViewReports.IsChecked = existing.Permissions.HasFlag(Permission.ViewReports);
            PermManageCustomers.IsChecked = existing.Permissions.HasFlag(Permission.ManageCustomers);
            PermManageCategories.IsChecked = existing.Permissions.HasFlag(Permission.ManageCategories);
            PermManageSuppliers.IsChecked = existing.Permissions.HasFlag(Permission.ManageSuppliers);
            PermAdjustStock.IsChecked = existing.Permissions.HasFlag(Permission.AdjustStock);
            PermManageSettings.IsChecked = existing.Permissions.HasFlag(Permission.ManageSettings);
            PermBackupRestore.IsChecked = existing.Permissions.HasFlag(Permission.BackupRestore);
        }
        else
        {
            PanelTitleText.Text = "Add User";
            UsernameBox.Text = "";
            UsernameBox.IsEnabled = true;
            FullNameBox.Text = "";
            RoleBox.SelectedIndex = 2; // default Cashier
            PermManageUsers.IsChecked = false;
            PermManageMedicines.IsChecked = false;
            PermManageSales.IsChecked = false;
            PermManagePurchases.IsChecked = false;
            PermViewReports.IsChecked = false;
            PermManageCustomers.IsChecked = false;
            PermManageCategories.IsChecked = false;
            PermManageSuppliers.IsChecked = false;
            PermAdjustStock.IsChecked = false;
            PermManageSettings.IsChecked = false;
            PermBackupRestore.IsChecked = false;
        }

        Scrim.IsHitTestVisible = true;
        Scrim.Opacity = 0.55;
        SidePanel.Margin = new Avalonia.Thickness(0, 0, 0, 0);
    }

    private void ClosePanel()
    {
        Scrim.Opacity = 0;
        Scrim.IsHitTestVisible = false;
        SidePanel.Margin = new Avalonia.Thickness(400, 0, 0, 0);
    }

    private async System.Threading.Tasks.Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(UsernameBox.Text)) { ShowError("Username is required."); return; }
        if (string.IsNullOrWhiteSpace(FullNameBox.Text)) { ShowError("Full name is required."); return; }
        if (!_isEdit && string.IsNullOrWhiteSpace(PasswordBox.Text)) { ShowError("Password is required for a new user."); return; }
        if (RoleBox.SelectedIndex < 0) { ShowError("Select a role."); return; }

        var user = _editingUser ?? new User();
        user.Username = UsernameBox.Text!.Trim();
        user.FullName = FullNameBox.Text!.Trim();
        user.Role = (UserRole)RoleBox.SelectedIndex;
        user.IsActive = true;

        var perms = Permission.None;
        if (PermManageUsers.IsChecked == true) perms |= Permission.ManageUsers;
        if (PermManageMedicines.IsChecked == true) perms |= Permission.ManageMedicines;
        if (PermManageSales.IsChecked == true) perms |= Permission.ManageSales;
        if (PermManagePurchases.IsChecked == true) perms |= Permission.ManagePurchases;
        if (PermViewReports.IsChecked == true) perms |= Permission.ViewReports;
        if (PermManageCustomers.IsChecked == true) perms |= Permission.ManageCustomers;
        if (PermManageCategories.IsChecked == true) perms |= Permission.ManageCategories;
        if (PermManageSuppliers.IsChecked == true) perms |= Permission.ManageSuppliers;
        if (PermAdjustStock.IsChecked == true) perms |= Permission.AdjustStock;
        if (PermManageSettings.IsChecked == true) perms |= Permission.ManageSettings;
        if (PermBackupRestore.IsChecked == true) perms |= Permission.BackupRestore;
        user.Permissions = perms;

        var enteredPassword = string.IsNullOrWhiteSpace(PasswordBox.Text) ? null : PasswordBox.Text;

        if (_isEdit)
            await _vm.UpdateAsync(user);
        else
            await _vm.AddAsync(user, enteredPassword ?? "");

        RefreshStats();
        ClosePanel();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
