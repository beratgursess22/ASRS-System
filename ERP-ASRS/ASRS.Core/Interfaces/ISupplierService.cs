using ASRS.Core.DTOs;

namespace ASRS.Core.Interfaces;

public interface ISupplierService
{
	Task<IEnumerable<SupplierListDto>> GetAllAsync(string? search);
	Task<IEnumerable<SupplierListDto>> GetActiveAsync();
	Task<IEnumerable<SupplierListDto>> GetActiveForPurchaseOrderAsync(int purchaseOrderId);
	Task<SupplierListDto?> GetByIdAsync(int id);
	Task<bool> CreateAsync(CreateSupplierDto dto);
	Task<bool> UpdateAsync(int id, CreateSupplierDto dto);
	Task<bool> DeleteAsync(int id);
	Task<IEnumerable<SupplierItemPriceDto>> GetItemPricesAsync(int supplierId);
	Task<bool> UpsertItemPriceAsync(UpsertSupplierItemPriceDto dto);
	Task<bool> DeleteItemPriceAsync(int supplierId, int? productId, int? materialId);
}