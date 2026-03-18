using ASRS.Core.Enums;

namespace ASRS.Core.Entities;

public class PurchaseRequest
{
    public int Id { get; set; }

    public int WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;

    public string RequestedByUserId { get; set; } = string.Empty;
    public AppUser RequestedByUser { get; set; } = null!;

    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PurchaseRequestItem> Items { get; set; } = new List<PurchaseRequestItem>();
}