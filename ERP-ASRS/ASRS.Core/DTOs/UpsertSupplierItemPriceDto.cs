namespace ASRS.Core.DTOs;

public class UpsertSupplierItemPriceDto
{
    public int SupplierId { get; set; }
    public int? ProductId { get; set; }
    public int? MaterialId { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "TRY";
}
