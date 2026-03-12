namespace ASRS.Core.DTOs;

public class CreateMaterialDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; } = 0;
    public int MinStockLevel { get; set; } = 0;
    public string? Description { get; set; }
}