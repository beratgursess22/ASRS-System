namespace ASRS.Core.DTOs;

public class BomItemListDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int ComponentProductId { get; set; }
    public string ComponentProductCode { get; set; } = string.Empty;
    public string ComponentProductName { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; }
    public int StockQuantity { get; set; }
    public bool IsStockSufficient { get; set; }
    public string? Notes { get; set; }
}
