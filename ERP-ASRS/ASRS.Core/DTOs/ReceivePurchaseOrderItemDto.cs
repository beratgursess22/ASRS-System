namespace ASRS.Core.DTOs;

public class ReceivePurchaseOrderItemDto
{
	public int PurchaseOrderId { get; set; }
	public int PurchaseOrderItemId { get; set; }
	public int ReceivedQuantity { get; set; }
}