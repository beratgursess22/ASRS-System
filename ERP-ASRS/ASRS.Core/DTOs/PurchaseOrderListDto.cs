using ASRS.Core.Enums;

namespace ASRS.Core.DTOs;

public class PurchaseOrderListDto
{
	public int Id { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public int PurchaseRequestId { get; set; }
	public int? WorkOrderId { get; set; }
	public string WorkOrderNumber { get; set; } = string.Empty;
	public PurchaseOrderStatus Status { get; set; }
	public DateTime CreatedAt { get; set; }
	public string? Notes { get; set; }
	public List<PurchaseOrderItemDto> Items { get; set; } = new();
	public decimal TotalAmount => Items.Sum(x => x.LineTotal);
	public int? SupplierId { get; set; }
	public string? SupplierName { get; set; }
	public DateTime? ExpectedDeliveryDate { get; set; }
}
