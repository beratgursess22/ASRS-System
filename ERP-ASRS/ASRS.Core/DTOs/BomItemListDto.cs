namespace ASRS.Core.DTOs;

public class BomItemListDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    
    // Bileşen ürün ise
    public int? ComponentProductId { get; set; }
    
    // Bileşen malzeme ise
    public int? MaterialId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; }
    public int StockQuantity { get; set; }
    public bool IsStockSufficient { get; set; }
    public string? Notes { get; set; }
    public string ComponentType { get; set; } = string.Empty; // "Product" veya "Material"
}
