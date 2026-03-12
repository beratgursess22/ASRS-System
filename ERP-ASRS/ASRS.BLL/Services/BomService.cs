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
            .Include(b => b.Material)
            .Where(b => b.ProductId == productId)
            .ToListAsync();

        var result = new List<BomItemListDto>();
        foreach (var item in items)
        {
            string code = string.Empty;
            string name = string.Empty;
            int stock = 0;
            string type = string.Empty;

            if (item.ComponentProduct != null)
            {
                code = item.ComponentProduct.Code;
                name = item.ComponentProduct.Name;
                stock = item.ComponentProduct.StockQuantity;
                type = "Product";
            }
            else if (item.Material != null)
            {
                code = item.Material.Code;
                name = item.Material.Name;
                stock = item.Material.StockQuantity;
                type = "Material";
            }

            var dto = new BomItemListDto
            {
                Id                   = item.Id,
                ProductId            = item.ProductId,
                ComponentProductId   = item.ComponentProductId,
                MaterialId           = item.MaterialId,
                ComponentCode        = code,
                ComponentName        = name,
                RequiredQuantity     = item.RequiredQuantity,
                StockQuantity        = stock,
                IsStockSufficient    = stock >= item.RequiredQuantity,
                Notes                = item.Notes,
                ComponentType        = type
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
            MaterialId         = dto.MaterialId,
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