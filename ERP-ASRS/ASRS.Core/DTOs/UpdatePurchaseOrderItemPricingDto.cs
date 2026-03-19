namespace ASRS.Core.DTOs;

public class UpdatePurchaseOrderItemPricingDto
{
	public int PurchaseOrderId { get; set; }
	public int PurchaseOrderItemId { get; set; }
	public decimal UnitPrice { get; set; }
	public string? Currency { get; set; }
}
