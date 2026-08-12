using Avalonia.Controls;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Customers;

public partial class CustomerFormWindow : Window
{
    private readonly Customer _customer;

    public CustomerFormWindow(Customer? existing = null)
    {
        InitializeComponent();

        _customer = existing ?? new Customer();

        if (existing != null)
        {
            NameBox.Text = existing.Name;
            PhoneBox.Text = existing.Phone;
            EmailBox.Text = existing.Email;
            AddressBox.Text = existing.Address;
        }

        SaveButton.Click += (_, _) => Save();
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = "Name is required.";
            ErrorText.IsVisible = true;
            return;
        }

        _customer.Name = NameBox.Text!.Trim();
        _customer.Phone = string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim();
        _customer.Email = string.IsNullOrWhiteSpace(EmailBox.Text) ? null : EmailBox.Text.Trim();
        _customer.Address = string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim();
        _customer.IsActive = true;

        Close(_customer);
    }
}
