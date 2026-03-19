using ASRS.Core.DTOs;
using ASRS.Core.Enums;

namespace ASRS.Core.Interfaces;

public interface IPurchaseOrderService
{
	Task<bool> CreateFromPurchaseRequestAsync(CreatePurchaseOrderDto dto, string createdByUserId);
	Task<IEnumerable<PurchaseOrderListDto>> GetAllAsync(PurchaseOrderStatus? status);
	Task<PurchaseOrderListDto?> GetByIdAsync(int id);
	Task<bool> UpdateStatusAsync(int id, PurchaseOrderStatus newStatus);
	Task<bool> ReceiveItemAsync(ReceivePurchaseOrderItemDto dto);
}