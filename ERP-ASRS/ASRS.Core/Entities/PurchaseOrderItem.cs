namespace ASRS.Core.Entities;

public class PurchaseOrderItem
{
	public int Id { get; set; }

	public int PurchaseOrderId { get; set; }
	public PurchaseOrder PurchaseOrder { get; set; } = null!;

	public int? ProductId { get; set; }
	public Product? Product { get; set; }

	public int? MaterialId { get; set; }
	public Material? Material { get; set; }

	public int OrderedQuantity { get; set; }
	public int ReceivedQuantity { get; set; } = 0;
	public decimal UnitPrice { get; set; } = 0m;
	public string? Currency { get; set; } = "TRY";
	public string? Notes { get; set; }
}