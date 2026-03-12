namespace ASRS.Core.DTOs;

public class MaterialListDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}