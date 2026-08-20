using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Desktop.ViewModels;

public class PurchaseOrderListViewModel
{
    private readonly IPurchaseOrderRepository _orderRepo;

    public ObservableCollection<PurchaseOrder> Orders { get; } = new();

    public PurchaseOrderListViewModel(IPurchaseOrderRepository orderRepo)
    {
        _orderRepo = orderRepo;
    }

    public async Task LoadAsync()
    {
        Orders.Clear();
        foreach (var o in await _orderRepo.GetAllAsync())
            Orders.Add(o);
    }

    public async Task MarkSentAsync(int orderId)
    {
        await _orderRepo.UpdateStatusAsync(orderId, PurchaseOrderStatus.Sent);
        await LoadAsync();
    }

    public async Task CancelAsync(int orderId)
    {
        await _orderRepo.UpdateStatusAsync(orderId, PurchaseOrderStatus.Cancelled);
        await LoadAsync();
    }
}
