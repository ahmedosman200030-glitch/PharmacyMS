using Avalonia.Controls;
using PharmacyMS.Application.Services;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Accounting;

public partial class OtherIncomeFormWindow : Window
{
    public OtherIncome? Result { get; private set; }

    private static readonly string[] IncomeTypes =
    {
        "Service Income",
        "Other Income"
    };

    private static readonly string[] ServiceCategories =
    {
        "Consultation",
        "Injection Service",
        "Home Injection Service",
        "Blood Pressure Check",
        "Blood Glucose Check",
        "First Aid Service",
        "Wound Dressing",
        "Nebulization Service",
        "Home Visit",
        "Health Screening",
        "Medical Advice Service",
        "Delivery Service",
        "Other Service Income"
    };

    private static readonly string[] PaymentMethods =
    {
        "Cash", "ZAAD Merchant", "E-DAHAB", "Bank Transfer"
    };

    public OtherIncomeFormWindow()
    {
        InitializeComponent();

        IncomeTypeCombo.ItemsSource = IncomeTypes;
        IncomeTypeCombo.SelectedIndex = 0;

        CategoryCombo.ItemsSource = ServiceCategories;
        CategoryCombo.SelectedIndex = 0;

        PaymentCombo.ItemsSource = PaymentMethods;
        PaymentCombo.SelectedIndex = 0;

        DatePickerBox.SelectedDate = DateTimeOffset.Now;

        RecordedByText.Text = SessionManager.CurrentUser?.FullName ?? "Unknown";

        CloseButton.Click += (_, _) => Close();
        CancelButton.Click += (_, _) => Close();

        SaveButton.Click += (_, _) =>
        {
            ErrorText.IsVisible = false;

            var incomeType = IncomeTypeCombo.SelectedItem as string ?? "Service Income";
            var category = CategoryCombo.SelectedItem as string ?? "Other Service Income";
            var payment = PaymentCombo.SelectedItem as string ?? "Cash";
            var notes = NotesBox.Text?.Trim() ?? "";

            if (!decimal.TryParse(AmountBox.Text, out var amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid amount greater than zero.";
                ErrorText.IsVisible = true;
                return;
            }

            var description = category;
            if (!string.IsNullOrWhiteSpace(notes))
                description += $" — {notes}";

            Result = new OtherIncome
            {
                Date = DatePickerBox.SelectedDate?.DateTime ?? DateTime.Now,
                Category = $"{incomeType}: {category}",
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
