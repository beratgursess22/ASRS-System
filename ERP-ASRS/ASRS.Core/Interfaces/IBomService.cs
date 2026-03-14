using ASRS.Core.DTOs;

namespace ASRS.Core.Interfaces;

public interface IBomService
{
    Task<IEnumerable<BomItemListDto>> GetBomByProductIdAsync(int productId);
    Task<bool> AddBomItemAsync(int productId, BomItemDto dto);
    Task<bool> UpdateBomItemAsync(int id, int requiredQuantity, string? notes);
    Task<bool> DeleteBomItemAsync(int id);
}