using ASRS.Core.Enums;

namespace ASRS.Core.DTOs;

public class CreateQualityInspectionDto
{
	public InspectionType InspectionType { get; set; } = InspectionType.Incoming;
	public int? PurchaseOrderId { get; set; }
	public int? PurchaseOrderItemId { get; set; }
	public int? WorkOrderId { get; set; }
	public DateTime? InspectionDate { get; set; }
	public string? Notes { get; set; }
	public List<CreateQualityInspectionItemDto> Items { get; set; } = new();
}
