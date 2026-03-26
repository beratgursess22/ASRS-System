namespace ASRS.Core.DTOs;

public class CancelRemainingPurchaseOrderItemDto
{
    public int PurchaseOrderId { get; set; }
    public int PurchaseOrderItemId { get; set; }
    public int CancelQuantity { get; set; }
    public string? Reason { get; set; }
}