namespace ASRS.Core.DTOs;

public class PurchaseOrderItemDto
{
	public int Id { get; set; }
	public int? ProductId { get; set; }
	public string? ProductCode { get; set; }
	public string? ProductName { get; set; }

	public int? MaterialId { get; set; }
	public string? MaterialCode { get; set; }
	public string? MaterialName { get; set; }

	public int OrderedQuantity { get; set; }
	public int ReceivedQuantity { get; set; }
	public int RemainingQuantity => OrderedQuantity - ReceivedQuantity;

	public decimal UnitPrice { get; set; }
	public string? Currency { get; set; }
}