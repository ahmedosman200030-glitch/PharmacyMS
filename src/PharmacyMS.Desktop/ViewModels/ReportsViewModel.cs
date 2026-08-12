using System.Collections.ObjectModel;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Interfaces.Repositories;

namespace PharmacyMS.Desktop.ViewModels;

public class ReportsViewModel
{
    private readonly IReportRepository _repository;

    public ObservableCollection<TopSellingMedicine> TopSellers { get; } = new();
    public ObservableCollection<MonthlyStockReconciliationRow> StockReconciliation { get; } = new();

    public decimal TotalRevenue { get; private set; }
    public int TotalTransactions { get; private set; }
    public decimal TotalPurchaseCost { get; private set; }
    public decimal NetProfit => TotalRevenue - TotalPurchaseCost;

    public ReportsViewModel(IReportRepository repository)
    {
        _repository = repository;
    }

    public async Task LoadAsync(DateTime from, DateTime to)
    {
        TopSellers.Clear();

        TotalRevenue = await _repository.GetTotalRevenueAsync(from, to);
        TotalTransactions = await _repository.GetTotalTransactionsAsync(from, to);
        TotalPurchaseCost = await _repository.GetTotalPurchaseCostAsync(from, to);

        var topSellers = await _repository.GetTopSellingMedicinesAsync(from, to, 10);
        foreach (var item in topSellers)
            TopSellers.Add(item);
    }

    public async Task LoadStockReconciliationAsync(DateTime monthStart, DateTime monthEnd)
    {
        StockReconciliation.Clear();
        var rows = await _repository.GetMonthlyStockReconciliationAsync(monthStart, monthEnd);
        foreach (var r in rows)
            StockReconciliation.Add(r);
    }
}
