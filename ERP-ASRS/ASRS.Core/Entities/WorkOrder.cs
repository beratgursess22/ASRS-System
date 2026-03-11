using ASRS.Core.Enums;

namespace ASRS.Core.Entities;

public class WorkOrder
{
	public int Id { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public int ProductId { get; set; }
	public Product Product { get; set; } = null!;
	public int Quantity { get; set; }
	public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
	public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
	public int? DepartmentId { get; set; }
	public Department? Department { get; set; }
	public string? AssignedUserId { get; set; }
	public AppUser? AssignedUser { get; set; }
	public string? CreatedByUserId { get; set; }
	public AppUser? CreatedByUser { get; set; }
	public DateTime PlannedStartDate { get; set; }
	public DateTime PlannedEndDate { get; set; }
	public string? Notes { get; set; }

	// Satın alma modülü için — şimdilik null
	public int? PurchaseRequestId { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? CompletedAt { get; set; }
}