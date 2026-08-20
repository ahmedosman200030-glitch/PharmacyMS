using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class ReceiveLine
{
    public int PurchaseOrderItemId { get; set; }
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderedQuantity { get; set; }
    public int AlreadyReceived { get; set; }
    public int RemainingQuantity => OrderedQuantity - AlreadyReceived;
    public decimal UnitCost { get; set; }

    // Editable at receiving time:
    public int ReceivedQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public bool HasDiscrepancy => ReceivedQuantity != RemainingQuantity;
}

public class ReceiveGoodsViewModel
{
    private readonly IPurchaseOrderRepository _orderRepo;
    private readonly IGoodsReceiptRepository _receiptRepo;
    private readonly IPurchaseRepository _purchaseRepo;

    public ObservableCollection<PurchaseOrder> PendingOrders { get; } = new();
    public ObservableCollection<ReceiveLine> Lines { get; } = new();

    public PurchaseOrder? SelectedOrder { get; private set; }

    public ReceiveGoodsViewModel(IPurchaseOrderRepository orderRepo, IGoodsReceiptRepository receiptRepo, IPurchaseRepository purchaseRepo)
    {
        _orderRepo = orderRepo;
        _receiptRepo = receiptRepo;
        _purchaseRepo = purchaseRepo;
    }

    public async Task LoadAsync()
    {
        PendingOrders.Clear();
        foreach (var o in await _orderRepo.GetPendingReceivingAsync())
            PendingOrders.Add(o);
    }

    public void SelectOrder(PurchaseOrder order)
    {
        SelectedOrder = order;
        Lines.Clear();
        foreach (var item in order.Items.Where(i => i.RemainingQuantity > 0))
        {
            Lines.Add(new ReceiveLine
            {
                PurchaseOrderItemId = item.Id,
                MedicineId = item.MedicineId,
                Name = item.MedicineName,
                OrderedQuantity = item.Quantity,
                AlreadyReceived = item.ReceivedQuantity,
                UnitCost = item.UnitCost,
                ReceivedQuantity = item.RemainingQuantity // default: assume full remaining qty arrived
            });
        }
    }

    public async Task<int> SubmitAsync(string? notes)
    {
        if (SelectedOrder == null) throw new InvalidOperationException("No purchase order selected.");

        var receipt = new GoodsReceipt
        {
            PurchaseOrderId = SelectedOrder.Id,
            ReceivedByUserId = PharmacyMS.Application.Services.SessionManager.CurrentUser?.Id ?? 0,
            Notes = notes,
            Items = Lines.Where(l => l.ReceivedQuantity > 0).Select(l => new GoodsReceiptItem
            {
                PurchaseOrderItemId = l.PurchaseOrderItemId,
                MedicineId = l.MedicineId,
                MedicineName = l.Name,
                OrderedQuantity = l.RemainingQuantity,
                ReceivedQuantity = l.ReceivedQuantity,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate,
                UnitCost = l.UnitCost
            }).ToList()
        };

        var id = await _receiptRepo.ReceiveAsync(receipt);

        // Stock and the Supplier Bill are no longer created here — this receipt
        // is now Pending until an admin approves it (see PendingApprovalsViewModel).

        SelectedOrder = null;
        Lines.Clear();
        await LoadAsync();
        return id;
    }
}
