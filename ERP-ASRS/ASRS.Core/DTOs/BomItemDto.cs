namespace ASRS.Core.DTOs;

public class BomItemDto
{
    public int? ComponentProductId { get; set; }
    public int? MaterialId { get; set; }
    public int RequiredQuantity { get; set; }
    public string? Notes { get; set; }
}