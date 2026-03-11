using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ASRS.BLL.Services;

public class BomService : IBomService
{
    private readonly AppDbContext _context;

    public BomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BomItemListDto>> GetBomByProductIdAsync(int productId)
    {
        var items = await _context.BillOfMaterials
            .Include(b => b.ComponentProduct)
            .Where(b => b.ProductId == productId)
            .ToListAsync();

        var result = new List<BomItemListDto>();
        foreach (var item in items)
        {
            var dto = new BomItemListDto
            {
                Id                   = item.Id,
                ProductId            = item.ProductId,
                ComponentProductId   = item.ComponentProductId,
                ComponentProductCode = item.ComponentProduct.Code,
                ComponentProductName = item.ComponentProduct.Name,
                RequiredQuantity     = item.RequiredQuantity,
                StockQuantity        = item.ComponentProduct.StockQuantity,
                IsStockSufficient    = item.ComponentProduct.StockQuantity >= item.RequiredQuantity,
                Notes                = item.Notes
            };
            result.Add(dto);
        }
        return result;
    }

    public async Task<bool> AddBomItemAsync(int productId, BomItemDto dto)
    {
        var item = new BillOfMaterial
        {
            ProductId          = productId,
            ComponentProductId = dto.ComponentProductId,
            RequiredQuantity   = dto.RequiredQuantity,
            Notes              = dto.Notes
        };
        _context.BillOfMaterials.Add(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteBomItemAsync(int id)
    {
        var item = await _context.BillOfMaterials.FindAsync(id);
        if (item == null)
            return false;
			
        _context.BillOfMaterials.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }
}