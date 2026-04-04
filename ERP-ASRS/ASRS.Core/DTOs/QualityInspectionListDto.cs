using ASRS.Core.Enums;

namespace ASRS.Core.DTOs;

public class QualityInspectionListDto
{
	public int Id { get; set; }
	public string InspectionNumber { get; set; } = string.Empty;
	public InspectionType InspectionType { get; set; }
	public InspectionStatus Status { get; set; }
	public UsageDecision? UsageDecision { get; set; }

	public int? PurchaseOrderId { get; set; }
	public string? PurchaseOrderNumber { get; set; }

	public int? PurchaseOrderItemId { get; set; }

	public int? WorkOrderId { get; set; }
	public string? WorkOrderNumber { get; set; }

	public string? InspectedByUserId { get; set; }
	public string? InspectedByUserName { get; set; }

	public DateTime InspectionDate { get; set; }
	public DateTime CreatedAt { get; set; }
	public int DefectCount { get; set; }
}
