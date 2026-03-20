namespace ASRS.Core.Entities;

public class SupplierItemPrice
{
    public int Id { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
