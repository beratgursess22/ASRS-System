using System.ComponentModel.DataAnnotations;

namespace ASRS.Core.DTOs;

public class CreateProductDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; } = 0;
    public int MinStockLevel { get; set; } = 0;
    public decimal DefaultUnitPrice { get; set; } = 0m;
    public string DefaultCurrency { get; set; } = "TRY";
    public string? Description { get; set; }
}