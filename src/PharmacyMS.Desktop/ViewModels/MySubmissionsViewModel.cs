using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Services;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class MySubmissionsViewModel
{
    private readonly IPendingSalePaymentRepository _paymentRepo;
    private readonly IPendingExpenseRepository _expenseRepo;
    private readonly IGoodsReceiptRepository _receiptRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ISupplierRepository _supplierRepo;

    public ObservableCollection<PendingSalePayment> MyPayments { get; } = new();
    public ObservableCollection<PendingExpense> MyExpenses { get; } = new();
    public ObservableCollection<GoodsReceipt> MyReceipts { get; } = new();
    public ObservableCollection<Customer> MyCustomers { get; } = new();
    public ObservableCollection<Supplier> MySuppliers { get; } = new();

    public MySubmissionsViewModel(
        IPendingSalePaymentRepository paymentRepo,
        IPendingExpenseRepository expenseRepo,
        IGoodsReceiptRepository receiptRepo,
        ICustomerRepository customerRepo,
        ISupplierRepository supplierRepo)
    {
        _paymentRepo = paymentRepo;
        _expenseRepo = expenseRepo;
        _receiptRepo = receiptRepo;
        _customerRepo = customerRepo;
        _supplierRepo = supplierRepo;
    }

    public async Task LoadAsync()
    {
        var userId = SessionManager.CurrentUser?.Id ?? 0;

        MyPayments.Clear();
        foreach (var p in await _paymentRepo.GetBySubmitterAsync(userId))
            MyPayments.Add(p);

        MyExpenses.Clear();
        foreach (var ex in await _expenseRepo.GetBySubmitterAsync(userId))
            MyExpenses.Add(ex);

        MyReceipts.Clear();
        foreach (var r in (await _receiptRepo.GetAllAsync()).Where(x => x.ReceivedByUserId == userId))
            MyReceipts.Add(r);

        MyCustomers.Clear();
        foreach (var c in await _customerRepo.GetBySubmitterAsync(userId))
            MyCustomers.Add(c);

        MySuppliers.Clear();
        foreach (var s in await _supplierRepo.GetBySubmitterAsync(userId))
            MySuppliers.Add(s);
    }
}
