using ASRS.Core.Enums;

namespace ASRS.Core.DTOs;

public class PurchaseRequestListDto
{
    public int Id { get; set; }

    public int WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string WorkOrderTitle { get; set; } = string.Empty;

    public string RequestedByUserId { get; set; } = string.Empty;
    public string RequestedByUserName { get; set; } = string.Empty;

    public PurchaseRequestStatus Status { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<PurchaseRequestItemDto> Items { get; set; } = new();
}