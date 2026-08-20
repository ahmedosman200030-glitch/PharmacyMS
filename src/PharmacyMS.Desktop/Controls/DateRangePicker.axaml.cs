using System;
using Avalonia.Controls;

namespace PharmacyMS.Desktop.Controls;

public partial class DateRangePicker : UserControl
{
    public DateTime FromDate { get; private set; } = DateTime.Today;
    public DateTime ToDate { get; private set; } = DateTime.Today;

    public event EventHandler? RangeApplied;

    public DateRangePicker()
    {
        InitializeComponent();

        ApplyButton.Click += (s, e) =>
        {
            var from = FromCalendar.SelectedDate ?? FromDate;
            var to = ToCalendar.SelectedDate ?? ToDate;
            if (to < from) (from, to) = (to, from);

            FromDate = from;
            ToDate = to;
            UpdateLabel();
            ToggleButton.Flyout?.Hide();
            RangeApplied?.Invoke(this, EventArgs.Empty);
        };
    }

    public void SetRange(DateTime from, DateTime to)
    {
        FromDate = from;
        ToDate = to;
        FromCalendar.SelectedDate = from;
        ToCalendar.SelectedDate = to;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        RangeLabel.Text = FromDate.Date == ToDate.Date
            ? FromDate.ToString("MMM d, yyyy")
            : $"{FromDate:MMM d, yyyy} - {ToDate:MMM d, yyyy}";
    }
}
