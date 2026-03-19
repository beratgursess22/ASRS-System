namespace ASRS.Core.Entities;

public class Material
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; } = 0;
    public int MinStockLevel { get; set; } = 0;
    public decimal DefaultUnitPrice { get; set; } = 0m;
    public string DefaultCurrency { get; set; } = "TRY";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}