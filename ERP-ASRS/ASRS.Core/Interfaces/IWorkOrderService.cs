using ASRS.Core.DTOs;
using ASRS.Core.Enums;

namespace ASRS.Core.Interfaces;

public interface IWorkOrderService
{
    Task<IEnumerable<WorkOrderListDto>> GetAllAsync(string? search, WorkOrderStatus? status);
    Task<WorkOrderListDto?> GetByIdAsync(int id);
    Task<bool> CreateAsync(CreateWorkOrderDto dto, string createdByUserId);
    Task<bool> UpdateStatusAsync(int id, WorkOrderStatus newStatus);
    Task<bool> DeleteAsync(int id);
}