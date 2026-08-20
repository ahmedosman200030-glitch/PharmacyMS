using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Desktop.ViewModels;

public class PendingApprovalsViewModel
{
    private readonly ICustomerRepository _customerRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IGoodsReceiptRepository _receiptRepo;
    private readonly IPendingSalePaymentRepository _paymentRepo;
    private readonly IPendingExpenseRepository _expenseRepo;
    private readonly ISaleRepository _saleRepo;
    private readonly IExpenseRepository _realExpenseRepo;

    public ObservableCollection<Customer> PendingCustomers { get; } = new();
    public ObservableCollection<Supplier> PendingSuppliers { get; } = new();
    public ObservableCollection<Purchase> PendingPurchases { get; } = new();
    public ObservableCollection<GoodsReceipt> PendingReceipts { get; } = new();
    public ObservableCollection<PendingSalePayment> PendingPayments { get; } = new();
    public ObservableCollection<PendingExpense> PendingExpenses { get; } = new();

    public int TotalPending =>
        PendingCustomers.Count + PendingSuppliers.Count + PendingPurchases.Count +
        PendingReceipts.Count + PendingPayments.Count + PendingExpenses.Count;

    public PendingApprovalsViewModel(
        ICustomerRepository customerRepo,
        ISupplierRepository supplierRepo,
        IPurchaseRepository purchaseRepo,
        IGoodsReceiptRepository receiptRepo,
        IPendingSalePaymentRepository paymentRepo,
        IPendingExpenseRepository expenseRepo,
        ISaleRepository saleRepo,
        IExpenseRepository realExpenseRepo)
    {
        _customerRepo = customerRepo;
        _supplierRepo = supplierRepo;
        _purchaseRepo = purchaseRepo;
        _receiptRepo = receiptRepo;
        _paymentRepo = paymentRepo;
        _expenseRepo = expenseRepo;
        _saleRepo = saleRepo;
        _realExpenseRepo = realExpenseRepo;
    }

    public async Task LoadAsync()
    {
        PendingCustomers.Clear();
        foreach (var c in (await _customerRepo.GetAllAsync()).Where(x => x.ApprovalStatus == ApprovalStatus.Pending))
            PendingCustomers.Add(c);

        PendingSuppliers.Clear();
        foreach (var s in (await _supplierRepo.GetAllAsync()).Where(x => x.ApprovalStatus == ApprovalStatus.Pending))
            PendingSuppliers.Add(s);

        PendingPurchases.Clear();
        foreach (var p in (await _purchaseRepo.GetAllAsync()).Where(x => x.ApprovalStatus == ApprovalStatus.Pending))
            PendingPurchases.Add(p);

        PendingReceipts.Clear();
        foreach (var r in (await _receiptRepo.GetAllAsync()).Where(x => x.ApprovalStatus == ApprovalStatus.Pending))
            PendingReceipts.Add(r);

        PendingPayments.Clear();
        foreach (var pay in await _paymentRepo.GetPendingAsync())
            PendingPayments.Add(pay);

        PendingExpenses.Clear();
        foreach (var ex in await _expenseRepo.GetPendingAsync())
            PendingExpenses.Add(ex);
    }

    public async Task ApproveCustomerAsync(Customer c)
    {
        c.ApprovalStatus = ApprovalStatus.Approved;
        await _customerRepo.UpdateAsync(c);
        PendingCustomers.Remove(c);
    }

    public async Task RejectCustomerAsync(Customer c)
    {
        c.ApprovalStatus = ApprovalStatus.Rejected;
        await _customerRepo.UpdateAsync(c);
        PendingCustomers.Remove(c);
    }

    public async Task ApproveSupplierAsync(Supplier s)
    {
        s.ApprovalStatus = ApprovalStatus.Approved;
        await _supplierRepo.UpdateAsync(s);
        PendingSuppliers.Remove(s);
    }

    public async Task RejectSupplierAsync(Supplier s)
    {
        s.ApprovalStatus = ApprovalStatus.Rejected;
        await _supplierRepo.UpdateAsync(s);
        PendingSuppliers.Remove(s);
    }

    public async Task ApprovePurchaseAsync(Purchase p)
    {
        p.ApprovalStatus = ApprovalStatus.Approved;
        await _purchaseRepo.UpdateApprovalStatusAsync(p.Id, p.ApprovalStatus);
        PendingPurchases.Remove(p);
    }

    public async Task RejectPurchaseAsync(Purchase p)
    {
        p.ApprovalStatus = ApprovalStatus.Rejected;
        await _purchaseRepo.UpdateApprovalStatusAsync(p.Id, p.ApprovalStatus);
        PendingPurchases.Remove(p);
    }

    public async Task ApproveReceiptAsync(GoodsReceipt r)
    {
        await _receiptRepo.ApproveAsync(r.Id);
        await _purchaseRepo.CreateBillFromReceiptAsync(r.Id, invoiceNumber: null);
        PendingReceipts.Remove(r);
    }

    public async Task RejectReceiptAsync(GoodsReceipt r, string reason)
    {
        await _receiptRepo.RejectAsync(r.Id, reason);
        PendingReceipts.Remove(r);
    }

    public async Task ApprovePaymentAsync(PendingSalePayment pay)
    {
        await _saleRepo.RecordPaymentAsync(pay.SaleId, pay.Amount, pay.Note);
        await _paymentRepo.UpdateStatusAsync(pay.Id, ApprovalStatus.Approved);
        PendingPayments.Remove(pay);
    }

    public async Task RejectPaymentAsync(PendingSalePayment pay, string reason)
    {
        await _paymentRepo.UpdateStatusAsync(pay.Id, ApprovalStatus.Rejected, reason);
        PendingPayments.Remove(pay);
    }

    public async Task ApproveExpenseAsync(PendingExpense ex)
    {
        await _realExpenseRepo.CreateAsync(new Expense
        {
            Date = ex.Date,
            Category = ex.Category,
            Description = ex.Description,
            Amount = ex.Amount,
            CreatedBy = ex.SubmittedByName,
            CreatedAt = DateTime.Now
        });
        await _expenseRepo.UpdateStatusAsync(ex.Id, ApprovalStatus.Approved);
        PendingExpenses.Remove(ex);
    }

    public async Task RejectExpenseAsync(PendingExpense ex, string reason)
    {
        await _expenseRepo.UpdateStatusAsync(ex.Id, ApprovalStatus.Rejected, reason);
        PendingExpenses.Remove(ex);
    }
}
