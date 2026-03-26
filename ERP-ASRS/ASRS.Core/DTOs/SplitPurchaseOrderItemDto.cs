namespace ASRS.Core.DTOs;

public class SplitPurchaseOrderItemDto
{
    public int PurchaseOrderId { get; set; }
    public int PurchaseOrderItemId { get; set; }
    public int SplitQuantity { get; set; }
    public string? Notes { get; set; }
}
