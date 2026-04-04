using ASRS.Core.Enums;

namespace ASRS.Core.Entities;

public class QualityInspection
{
	public int Id { get; set; }
	public string InspectionNumber { get; set; } = string.Empty;

	public InspectionType InspectionType { get; set; } = InspectionType.Incoming;
	public InspectionStatus Status { get; set; } = InspectionStatus.Pending;
	public UsageDecision? UsageDecision { get; set; }

	public int? PurchaseOrderId { get; set; }
	public PurchaseOrder? PurchaseOrder { get; set; }

	public int? PurchaseOrderItemId { get; set; }
	public PurchaseOrderItem? PurchaseOrderItem { get; set; }

	public int? WorkOrderId { get; set; }
	public WorkOrder? WorkOrder { get; set; }

	public string? InspectedByUserId { get; set; }
	public AppUser? InspectedByUser { get; set; }

	public DateTime InspectionDate { get; set; } = DateTime.UtcNow;
	public string? Notes { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? UpdatedAt { get; set; }
	public DateTime? ClosedAt { get; set; }

	public ICollection<QualityInspectionItem> Items { get; set; } = new List<QualityInspectionItem>();
	public ICollection<QualityDefect> Defects { get; set; } = new List<QualityDefect>();
}
