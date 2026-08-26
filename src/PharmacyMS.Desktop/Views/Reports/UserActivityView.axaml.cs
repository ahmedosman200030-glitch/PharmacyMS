using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Interfaces.Repositories;

namespace PharmacyMS.Desktop.Views.Reports;

public partial class UserActivityView : UserControl
{
    private readonly IReportRepository _reportRepo;

    public UserActivityView()
    {
        InitializeComponent();
        _reportRepo = Program.Services.GetRequiredService<IReportRepository>();

        RangePicker.SetRange(DateTime.Today, DateTime.Today);

        RefreshButton.Click += async (_, _) => await LoadAndRender();

        _ = LoadAndRender();
    }

    private async Task LoadAndRender()
    {
        var from = RangePicker.FromDate;
        var to = RangePicker.ToDate;

        var rows = (await _reportRepo.GetUserActivityAsync(from, to)).ToList();

        double totalHours = 0;
        double totalSales = 0;
        int totalTxns = 0;

        var display = rows.Select(r =>
        {
            var logoutForCalc = r.LogoutTime ?? DateTime.Now;
            var hours = (logoutForCalc - r.LoginTime).TotalHours;
            if (hours < 0) hours = 0;
            totalHours += hours;
            totalSales += r.SalesAmount;
            totalTxns += r.Transactions;

            var h = (int)hours;
            var m = (int)((hours - h) * 60);

            return new
            {
                r.UserName,
                LoginDisplay = r.LoginTime.ToString("HH:mm"),
                LogoutDisplay = r.LogoutTime?.ToString("HH:mm") ?? "Online",
                HoursDisplay = $"{h}h {m:D2}m",
                SalesDisplay = $"${r.SalesAmount:F2}",
                r.Transactions
            };
        }).ToList();

        ActivityGrid.ItemsSource = display;

        var totalH = (int)totalHours;
        var totalM = (int)((totalHours - totalH) * 60);
        TotalHoursText.Text = $"{totalH}h {totalM:D2}m";
        TotalSalesText.Text = $"${totalSales:F2}";
        AvgSalesText.Text = rows.Count > 0 ? $"${(totalSales / rows.Count):F2}" : "$0.00";
    }
}
