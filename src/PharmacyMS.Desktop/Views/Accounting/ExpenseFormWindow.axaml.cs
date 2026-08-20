using Avalonia.Controls;
using PharmacyMS.Application.Services;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Accounting;

public partial class ExpenseFormWindow : Window
{
    public Expense? Result { get; private set; }

    public ExpenseFormWindow()
    {
        InitializeComponent();

        CategoryCombo.ItemsSource = ExpensesViewModel.PredefinedCategories;
        CategoryCombo.SelectedIndex = 0;
        DatePickerBox.SelectedDate = DateTimeOffset.Now;

        CancelButton.Click += (_, _) => Close();

        SaveButton.Click += (_, _) =>
        {
            ErrorText.IsVisible = false;

            var category = CategoryCombo.SelectedItem as string ?? "Other";
            var description = DescriptionBox.Text?.Trim() ?? "";

            if (!decimal.TryParse(AmountBox.Text, out var amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid amount greater than zero.";
                ErrorText.IsVisible = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                ErrorText.Text = "Enter a short description.";
                ErrorText.IsVisible = true;
                return;
            }

            Result = new Expense
            {
                Date = DatePickerBox.SelectedDate?.DateTime ?? DateTime.Now,
                Category = category,
                Description = description,
                Amount = amount,
                CreatedBy = SessionManager.CurrentUser?.FullName ?? "Unknown",
                CreatedAt = DateTime.Now
            };

            Close();
        };
    }
}
