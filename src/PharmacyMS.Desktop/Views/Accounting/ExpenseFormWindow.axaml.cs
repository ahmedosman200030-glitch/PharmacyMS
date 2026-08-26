using Avalonia.Controls;
using PharmacyMS.Application.Services;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Accounting;

public partial class ExpenseFormWindow : Window
{
    public Expense? Result { get; private set; }

    private static readonly string[] PaymentMethods =
    {
        "Cash", "ZAAD Merchant", "E-DAHAB", "Bank Transfer"
    };

    public ExpenseFormWindow()
    {
        InitializeComponent();

        CategoryCombo.ItemsSource = ExpensesViewModel.PredefinedCategories;
        CategoryCombo.SelectedIndex = 0;

        PaymentCombo.ItemsSource = PaymentMethods;
        PaymentCombo.SelectedIndex = 0;

        DatePickerBox.SelectedDate = DateTimeOffset.Now;

        RecordedByText.Text = SessionManager.CurrentUser?.FullName ?? "Unknown";

        CategoryCombo.SelectionChanged += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(DescriptionBox.Text) ||
                ExpensesViewModel.PredefinedCategories.Contains(DescriptionBox.Text))
            {
                DescriptionBox.Text = CategoryCombo.SelectedItem as string ?? "";
            }
        };

        CloseButton.Click += (_, _) => Close();
        CancelButton.Click += (_, _) => Close();

        SaveButton.Click += (_, _) =>
        {
            ErrorText.IsVisible = false;

            var category = CategoryCombo.SelectedItem as string ?? "Other Expense";
            var description = DescriptionBox.Text?.Trim() ?? "";
            var payment = PaymentCombo.SelectedItem as string ?? "Cash";
            var notes = NotesBox.Text?.Trim() ?? "";

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

            if (!string.IsNullOrWhiteSpace(notes))
                description += $" — {notes}";

            Result = new Expense
            {
                Date = DatePickerBox.SelectedDate?.DateTime ?? DateTime.Now,
                Category = category,
                Description = description,
                Amount = amount,
                PaymentMethod = payment,
                CreatedBy = SessionManager.CurrentUser?.FullName ?? "Unknown",
                CreatedAt = DateTime.Now
            };

            Close();
        };
    }
}
