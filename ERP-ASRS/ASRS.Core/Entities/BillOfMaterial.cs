namespace ASRS.Core.Entities;

public class BillOfMaterial
{
    public int Id { get; set; }

    // Bu BOM hangi ürüne ait (üretilen/montajlanan ürün)
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Bu BOM'da gereken bileşen ürün
    public int ComponentProductId { get; set; }
    public Product ComponentProduct { get; set; } = null!;
    public int RequiredQuantity { get; set; }  // kaç adet lazım
    public string? Notes { get; set; }
}