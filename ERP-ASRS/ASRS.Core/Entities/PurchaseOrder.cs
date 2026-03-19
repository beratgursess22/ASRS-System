using ASRS.Core.Enums;

namespace ASRS.Core.Entities;

public class PurchaseOrder
{
	public int Id { get; set; }

	public string OrderNumber { get; set; } = string.Empty;

	public int PurchaseRequestId { get; set; }
	public PurchaseRequest PurchaseRequest { get; set; } = null!;

	public string CreatedByUserId { get; set; } = string.Empty;
	public AppUser CreatedByUser { get; set; } = null!;

	public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? ApprovedAt { get; set; }
	public DateTime? CompletedAt { get; set; }

	public string? Notes { get; set; }

	public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}