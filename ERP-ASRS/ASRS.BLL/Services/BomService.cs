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
                Id = item.Id,
                ProductId = item.ProductId,
                ComponentProductId = item.ComponentProductId,
                MaterialId = item.MaterialId,
                ComponentCode = code,
                ComponentName = name,
                RequiredQuantity = item.RequiredQuantity,
                StockQuantity = stock,
                IsStockSufficient = stock >= item.RequiredQuantity,
                Notes = item.Notes,
                ComponentType = type
            };
            result.Add(dto);
        }
        return result;
    }

    public async Task<IReadOnlyList<BomRequirementNodeDto>> GetNestedBomRequirementsAsync(int productId, int workOrderQuantity)
    {
        if (workOrderQuantity <= 0)
            return new List<BomRequirementNodeDto>();

        var path = new HashSet<int>();
        var nodes = await BuildNodesForProductAsync(productId, workOrderQuantity, path);
        ApplyAggregateStockSufficiency(nodes);
        return nodes;

        async Task<List<BomRequirementNodeDto>> BuildNodesForProductAsync(int currentProductId, int multiplier, HashSet<int> visiting)
        {
            if (multiplier <= 0)
                return new List<BomRequirementNodeDto>();

            // Path-level cycle guard: A -> B -> A
            if (!visiting.Add(currentProductId))
            {
                return new List<BomRequirementNodeDto>
            {
                new BomRequirementNodeDto
                {
                    ComponentProductId = currentProductId,
                    ComponentType = "Product",
                    ComponentCode = "CYCLE",
                    ComponentName = "Döngü tespit edildi",
                    RequiredPerParent = 0,
                    TotalRequired = 0,
                    StockQuantity = 0,
                    IsStockSufficient = false,
                    IsCycleDetected = true
                }
            };
            }

            try
            {
                var items = await _context.BillOfMaterials
                    .Include(b => b.ComponentProduct)
                    .Include(b => b.Material)
                    .Where(b => b.ProductId == currentProductId)
                    .ToListAsync();

                var result = new List<BomRequirementNodeDto>();

                foreach (var item in items)
                {
                    var totalRequiredLong = (long)item.RequiredQuantity * multiplier;
                    if (totalRequiredLong <= 0 || totalRequiredLong > int.MaxValue)
                        continue;

                    var totalRequired = (int)totalRequiredLong;

                    if (item.ComponentProductId.HasValue && item.ComponentProduct != null)
                    {
                        var componentId = item.ComponentProductId.Value;
                        var cycleDetected = visiting.Contains(componentId);

                        var node = new BomRequirementNodeDto
                        {
                            ComponentProductId = componentId,
                            ComponentType = "Product",
                            ComponentCode = item.ComponentProduct.Code,
                            ComponentName = item.ComponentProduct.Name,
                            RequiredPerParent = item.RequiredQuantity,
                            TotalRequired = totalRequired,
                            StockQuantity = item.ComponentProduct.StockQuantity,
                            IsStockSufficient = item.ComponentProduct.StockQuantity >= totalRequired,
                            IsCycleDetected = cycleDetected
                        };

                        if (!cycleDetected)
                        {
                            node.Children = await BuildNodesForProductAsync(componentId, totalRequired, visiting);
                            if (node.Children.Any(c => c.IsCycleDetected))
                                node.IsCycleDetected = true;
                        }

                        result.Add(node);
                    }
                    else if (item.MaterialId.HasValue && item.Material != null)
                    {
                        result.Add(new BomRequirementNodeDto
                        {
                            MaterialId = item.MaterialId.Value,
                            ComponentType = "Material",
                            ComponentCode = item.Material.Code,
                            ComponentName = item.Material.Name,
                            RequiredPerParent = item.RequiredQuantity,
                            TotalRequired = totalRequired,
                            StockQuantity = item.Material.StockQuantity,
                            IsStockSufficient = item.Material.StockQuantity >= totalRequired,
                            IsCycleDetected = false
                        });
                    }
                }

                return result;
            }
            finally
            {
                visiting.Remove(currentProductId);
            }
        }
    }

    private static void ApplyAggregateStockSufficiency(List<BomRequirementNodeDto> rootNodes)
    {
        var aggregate = new Dictionary<string, (int required, int stock)>(StringComparer.Ordinal);
        CollectAggregates(rootNodes, aggregate);
        ApplyAggregateSufficiency(rootNodes, aggregate);
    }

    private static void CollectAggregates(IEnumerable<BomRequirementNodeDto> nodes, Dictionary<string, (int required, int stock)> aggregate)
    {
        foreach (var node in nodes)
        {
            var key = GetNodeKey(node);

            if (key != null)
            {
                if (aggregate.TryGetValue(key, out var current))
                {
                    aggregate[key] = (
                        required: current.required + node.TotalRequired,
                        stock: Math.Max(current.stock, node.StockQuantity)
                    );
                }
                else
                {
                    aggregate[key] = (node.TotalRequired, node.StockQuantity);
                }
            }

            if (node.Children != null && node.Children.Count > 0)
                CollectAggregates(node.Children, aggregate);
        }
    }

    private static void ApplyAggregateSufficiency(IEnumerable<BomRequirementNodeDto> nodes, Dictionary<string, (int required, int stock)> aggregate)
    {
        foreach (var node in nodes)
        {
            if (node.IsCycleDetected)
            {
                node.IsStockSufficient = false;
            }
            else
            {
                var key = GetNodeKey(node);
                if (key != null && aggregate.TryGetValue(key, out var total))
                    node.IsStockSufficient = total.stock >= total.required;
            }

            if (node.Children != null && node.Children.Count > 0)
                ApplyAggregateSufficiency(node.Children, aggregate);
        }
    }

    private static string? GetNodeKey(BomRequirementNodeDto node)
    {
        if (node.ComponentType == "Product" && node.ComponentProductId.HasValue)
            return "P:" + node.ComponentProductId.Value;

        if (node.ComponentType == "Material" && node.MaterialId.HasValue)
            return "M:" + node.MaterialId.Value;

        return null;
    }

    public async Task<bool> AddBomItemAsync(int productId, BomItemDto dto)
    {
        if (dto.RequiredQuantity <= 0)
            return false;

        var hasComponentProduct = dto.ComponentProductId.HasValue;
        var hasMaterial = dto.MaterialId.HasValue;

        // XOR rule: only one component type can be selected.
        if (hasComponentProduct == hasMaterial)
            return false;

        if (hasComponentProduct && dto.ComponentProductId is int componentProductId)
        {
            if (componentProductId == productId)
                return false;

            var componentExists = await _context.Products.AnyAsync(p => p.Id == componentProductId);
            if (!componentExists)
                return false;

            var createsCycle = await CreatesCycleAsync(productId, componentProductId);
            if (createsCycle)
                return false;

            var duplicateProductComponent = await _context.BillOfMaterials
                .AnyAsync(b => b.ProductId == productId && b.ComponentProductId == componentProductId);
            if (duplicateProductComponent)
                return false;
        }

        if (hasMaterial && dto.MaterialId is int materialId)
        {
            var materialExists = await _context.Materials.AnyAsync(m => m.Id == materialId);
            if (!materialExists)
                return false;
            var duplicateMaterialComponent = await _context.BillOfMaterials
                .AnyAsync(b => b.ProductId == productId && b.MaterialId == materialId);
            if (duplicateMaterialComponent)
                return false;
        }

        var item = new BillOfMaterial
        {
            ProductId = productId,
            ComponentProductId = dto.ComponentProductId,
            MaterialId = dto.MaterialId,
            RequiredQuantity = dto.RequiredQuantity,
            Notes = dto.Notes
        };
        _context.BillOfMaterials.Add(item);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> CreatesCycleAsync(int parentProductId, int childProductId)
    {
        if (parentProductId == childProductId)
            return true;

        var visited = new HashSet<int>();
        return await HasPathToTargetAsync(childProductId, parentProductId, visited);
    }

    private async Task<bool> HasPathToTargetAsync(int currentProductId, int targetProductId, HashSet<int> visited)
    {
        if (currentProductId == targetProductId)
            return true;

        if (!visited.Add(currentProductId))
            return false;

        var nextProductIds = await _context.BillOfMaterials
            .Where(b => b.ProductId == currentProductId && b.ComponentProductId.HasValue)
            .Select(b => b.ComponentProductId!.Value)
            .ToListAsync();

        foreach (var nextId in nextProductIds)
        {
            var found = await HasPathToTargetAsync(nextId, targetProductId, visited);
            if (found)
                return true;
        }

        return false;
    }


    public async Task<bool> UpdateBomItemAsync(int id, int requiredQuantity, string? notes)
    {
        if (requiredQuantity <= 0)
            return false;

        var item = await _context.BillOfMaterials.FindAsync(id);
        if (item == null)
            return false;

        item.RequiredQuantity = requiredQuantity;
        item.Notes = notes;

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