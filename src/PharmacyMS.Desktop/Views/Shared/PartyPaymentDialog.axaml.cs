using System.Globalization;
using Avalonia.Controls;

namespace PharmacyMS.Desktop.Views.Shared;

public class PartyOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public override string ToString() => $"{Name} — ${Balance:F2}";
}

public partial class PartyPaymentDialog : Window
{
    public (int Id, decimal Amount)? Result { get; private set; }

    public PartyPaymentDialog() { InitializeComponent(); }

    public PartyPaymentDialog(string title, string partyLabel, List<PartyOption> parties, int? preselectId = null)
    {
        InitializeComponent();

        TitleText.Text = title;
        PartyLabel.Text = partyLabel;
        PartyCombo.ItemsSource = parties;

        var initial = preselectId.HasValue
            ? parties.FirstOrDefault(p => p.Id == preselectId.Value) ?? parties.FirstOrDefault()
            : parties.FirstOrDefault();
        PartyCombo.SelectedItem = initial;
        UpdateForSelection(initial);

        PartyCombo.SelectionChanged += (_, _) => UpdateForSelection(PartyCombo.SelectedItem as PartyOption);

        CancelButton.Click += (_, _) => Close();

        ConfirmButton.Click += (_, _) =>
        {
            ErrorText.IsVisible = false;
            var selected = PartyCombo.SelectedItem as PartyOption;
            if (selected == null)
            {
                ErrorText.Text = "Select a customer.";
                ErrorText.IsVisible = true;
                return;
            }

            if (!decimal.TryParse(AmountBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid amount greater than 0.";
                ErrorText.IsVisible = true;
                return;
            }

            if (amount > selected.Balance)
            {
                ErrorText.Text = $"Amount can't exceed the balance of ${selected.Balance:F2}.";
                ErrorText.IsVisible = true;
                return;
            }

            Result = (selected.Id, amount);
            Close();
        };
    }

    private void UpdateForSelection(PartyOption? party)
    {
        if (party == null)
        {
            BalanceText.Text = "";
            AmountBox.Text = "";
            return;
        }
        BalanceText.Text = $"Outstanding balance: ${party.Balance:F2}";
        AmountBox.Text = party.Balance.ToString("F2", CultureInfo.InvariantCulture);
    }
}
