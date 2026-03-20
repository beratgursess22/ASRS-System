namespace ASRS.Core.DTOs;

public class SupplierItemPriceDto
{
    public int SupplierId { get; set; }
    public int? ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public int? MaterialId { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialName { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "TRY";
}
