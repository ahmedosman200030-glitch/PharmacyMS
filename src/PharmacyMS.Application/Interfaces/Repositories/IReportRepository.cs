using PharmacyMS.Application.DTOs;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to);
    Task<int> GetTotalTransactionsAsync(DateTime from, DateTime to);
    Task<decimal> GetTotalPurchaseCostAsync(DateTime from, DateTime to);
    Task<IEnumerable<TopSellingMedicine>> GetTopSellingMedicinesAsync(DateTime from, DateTime to, int topN = 10);

    Task<decimal> GetTotalReceivablesAsync();
    Task<decimal> GetReceivablesCreatedInRangeAsync(DateTime from, DateTime to);
    Task<IEnumerable<MonthlySummary>> GetMonthlySalesAndPurchasesAsync(int months = 4);
    Task<IEnumerable<MonthlyStockReconciliationRow>> GetMonthlyStockReconciliationAsync(DateTime monthStart, DateTime monthEnd);
    Task<string?> GetSettingAsync(string key);
    Task SetSettingAsync(string key, string value);

    // New financial reports
    Task<IEnumerable<DailySalesSummaryRow>> GetDailySalesAsync(DateTime from, DateTime to);
    Task<IEnumerable<PaymentMethodBreakdownRow>> GetPaymentMethodBreakdownAsync(DateTime from, DateTime to);
    Task<IEnumerable<TaxReportRow>> GetTaxReportAsync(DateTime from, DateTime to);
    Task<IEnumerable<InventoryValuationRow>> GetInventoryValuationAsync();
    Task<IEnumerable<PurchaseVsSalesRow>> GetPurchaseVsSalesAsync(DateTime from, DateTime to);
    Task<IEnumerable<SupplierPaymentRow>> GetSupplierPaymentsAsync();
}
