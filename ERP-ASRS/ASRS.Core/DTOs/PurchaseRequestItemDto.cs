namespace ASRS.Core.DTOs;

public class PurchaseRequestItemDto
{
    public int Id { get; set; }

    public int? ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }

    public int? MaterialId { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialName { get; set; }

    public int RequiredQuantity { get; set; }
    public int CurrentStockQuantity { get; set; }
    public int MissingQuantity { get; set; }
    public string? Notes { get; set; }
}