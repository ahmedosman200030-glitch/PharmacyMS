using System.Collections.ObjectModel;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Interfaces.Repositories;

namespace PharmacyMS.Desktop.ViewModels;

public class ReportsViewModel
{
    private readonly IReportRepository _repository;

    public ObservableCollection<TopSellingMedicine> TopSellers { get; } = new();
    public ObservableCollection<MonthlyStockReconciliationRow> StockReconciliation { get; } = new();
    public ObservableCollection<DailySalesSummaryRow> DailySales { get; } = new();
    public ObservableCollection<PaymentMethodBreakdownRow> PaymentBreakdown { get; } = new();
    public ObservableCollection<TaxReportRow> TaxReport { get; } = new();
    public ObservableCollection<InventoryValuationRow> InventoryValuation { get; } = new();
    public ObservableCollection<PurchaseVsSalesRow> PurchaseVsSales { get; } = new();
    public ObservableCollection<SupplierPaymentRow> SupplierPayments { get; } = new();

    public decimal TotalRevenue { get; private set; }
    public int TotalTransactions { get; private set; }
    public decimal TotalPurchaseCost { get; private set; }
    public decimal NetProfit => TotalRevenue - TotalPurchaseCost;
    public double TotalTax { get; private set; }
    public double TotalDiscount { get; private set; }
    public double TotalInventoryCostValue { get; private set; }
    public double TotalInventoryRetailValue { get; private set; }
    public double TotalSupplierBalance { get; private set; }

    public ReportsViewModel(IReportRepository repository) { _repository = repository; }

    public async Task LoadAsync(DateTime from, DateTime to)
    {
        TopSellers.Clear(); DailySales.Clear(); PaymentBreakdown.Clear();
        TaxReport.Clear(); PurchaseVsSales.Clear();

        TotalRevenue = await _repository.GetTotalRevenueAsync(from, to);
        TotalTransactions = await _repository.GetTotalTransactionsAsync(from, to);
        TotalPurchaseCost = await _repository.GetTotalPurchaseCostAsync(from, to);

        foreach (var i in await _repository.GetTopSellingMedicinesAsync(from, to, 10)) TopSellers.Add(i);
        foreach (var i in await _repository.GetDailySalesAsync(from, to)) DailySales.Add(i);
        foreach (var i in await _repository.GetPaymentMethodBreakdownAsync(from, to)) PaymentBreakdown.Add(i);
        foreach (var i in await _repository.GetTaxReportAsync(from, to)) TaxReport.Add(i);
        foreach (var i in await _repository.GetPurchaseVsSalesAsync(from, to)) PurchaseVsSales.Add(i);

        TotalTax = TaxReport.Sum(r => r.TaxAmount);
        TotalDiscount = DailySales.Sum(r => r.Discount);
    }

    public async Task LoadStockReconciliationAsync(DateTime monthStart, DateTime monthEnd)
    {
        StockReconciliation.Clear();
        foreach (var r in await _repository.GetMonthlyStockReconciliationAsync(monthStart, monthEnd))
            StockReconciliation.Add(r);
    }

    public async Task LoadInventoryValuationAsync()
    {
        InventoryValuation.Clear();
        foreach (var r in await _repository.GetInventoryValuationAsync()) InventoryValuation.Add(r);
        TotalInventoryCostValue = InventoryValuation.Sum(r => r.CostValue);
        TotalInventoryRetailValue = InventoryValuation.Sum(r => r.RetailValue);
    }

    public async Task LoadSupplierPaymentsAsync()
    {
        SupplierPayments.Clear();
        foreach (var r in await _repository.GetSupplierPaymentsAsync()) SupplierPayments.Add(r);
        TotalSupplierBalance = SupplierPayments.Sum(r => r.Balance);
    }
}
