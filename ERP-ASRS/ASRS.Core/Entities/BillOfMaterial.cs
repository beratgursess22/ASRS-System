namespace ASRS.Core.Entities;

public class BillOfMaterial
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public int? ComponentProductId { get; set; }
    public Product? ComponentProduct { get; set; }
    
    public int? MaterialId { get; set; }
    public Material? Material { get; set; }
    
    public int RequiredQuantity { get; set; }
    public string? Notes { get; set; }
}