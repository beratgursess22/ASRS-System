namespace ASRS.Core.Entities;

public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;      // Örn: PRD-001
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;  // Elektronik, Mekanik vs.
    public string Unit { get; set; } = string.Empty;      // Adet, Kg, Metre vs.
    public int StockQuantity { get; set; } = 0;
    public int MinStockLevel { get; set; } = 0;           // Minimum stok uyarı eşiği
    public decimal DefaultUnitPrice { get; set; } = 0m;
    public string DefaultCurrency { get; set; } = "TRY";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}