namespace ASRS.Core.Entities;

public class PurchaseRequestItem
{
    public int Id { get; set; }

    public int PurchaseRequestId { get; set; }
    public PurchaseRequest PurchaseRequest { get; set; } = null!;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    public int RequiredQuantity { get; set; }
    public int CurrentStockQuantity { get; set; }
    public int MissingQuantity { get; set; }
    public string? Notes { get; set; }
}