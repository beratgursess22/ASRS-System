using ASRS.Core.DTOs;
using ASRS.Core.Enums;

namespace ASRS.Core.Interfaces;

public interface IPurchaseRequestService
{
    Task<IEnumerable<PurchaseRequestListDto>> GetAllAsync(PurchaseRequestStatus? status);
    Task<PurchaseRequestListDto?> GetByIdAsync(int id);
    Task<bool> CreateFromWorkOrderAsync(int workOrderId, string requestedByUserId, string? notes);
    Task<bool> UpdateStatusAsync(int id, PurchaseRequestStatus status, string? notes);
}