using ASRS.Core.Enums;

namespace ASRS.Core.DTOs;

public class PurchaseOrderListDto
{
	public int Id { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public int PurchaseRequestId { get; set; }
	public string WorkOrderNumber { get; set; } = string.Empty;
	public PurchaseOrderStatus Status { get; set; }
	public DateTime CreatedAt { get; set; }
	public string? Notes { get; set; }
	public List<PurchaseOrderItemDto> Items { get; set; } = new();
}