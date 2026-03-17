namespace ASRS.Core.DTOs;

public class BomRequirementNodeDto
{
    public int? ComponentProductId { get; set; }
    public int? MaterialId { get; set; }
    public string ComponentType { get; set; } = string.Empty; // Product | Material
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public int RequiredPerParent { get; set; }
    public int TotalRequired { get; set; }
    public int StockQuantity { get; set; }
    public bool IsStockSufficient { get; set; }
    public bool IsCycleDetected { get; set; }
    public List<BomRequirementNodeDto> Children { get; set; } = new();
}