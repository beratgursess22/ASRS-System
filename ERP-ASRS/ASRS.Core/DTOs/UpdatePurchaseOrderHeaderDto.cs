namespace ASRS.Core.DTOs;

public class UpdatePurchaseOrderHeaderDto
{
	public int PurchaseOrderId { get; set; }
	public int? SupplierId { get; set; }
	public DateTime? ExpectedDeliveryDate { get; set; }
	public string? Notes { get; set; }
}