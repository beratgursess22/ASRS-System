using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ASRS.BLL.Services;

public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly AppDbContext _context;
    private readonly IBomService _bomService;

    public PurchaseRequestService(AppDbContext context, IBomService bomService)
    {
        _context = context;
        _bomService = bomService;
    }

    public async Task<bool> CreateFromWorkOrderAsync(int workOrderId, string requestedByUserId, string? notes)
    {
        if (string.IsNullOrWhiteSpace(requestedByUserId))
            return false;

        var workOrder = await _context.WorkOrders
            .Include(w => w.Product)
            .FirstOrDefaultAsync(w => w.Id == workOrderId);

        if (workOrder == null)
            return false;

        if (workOrder.Status == WorkOrderStatus.Completed || workOrder.Status == WorkOrderStatus.Cancelled)
            return false;
            
        var hasActiveRequest = await _context.PurchaseRequests
            .AnyAsync(pr => pr.WorkOrderId == workOrderId
                && pr.Status != PurchaseRequestStatus.Received
                && pr.Status != PurchaseRequestStatus.Rejected);

        if (hasActiveRequest)
            return false;

        var tree = await _bomService.GetNestedBomRequirementsAsync(workOrder.ProductId, workOrder.Quantity);
        var missingItems = FlattenMissing(tree);

        if (missingItems.Count == 0)
            return false;

        var request = new PurchaseRequest
        {
            WorkOrderId = workOrderId,
            RequestedByUserId = requestedByUserId,
            Status = PurchaseRequestStatus.Pending,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in missingItems)
        {
            request.Items.Add(new PurchaseRequestItem
            {
                ProductId = item.ProductId,
                MaterialId = item.MaterialId,
                RequiredQuantity = item.RequiredQuantity,
                CurrentStockQuantity = item.CurrentStockQuantity,
                MissingQuantity = item.MissingQuantity,
                Notes = item.Notes
            });
        }

        _context.PurchaseRequests.Add(request);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PurchaseRequestListDto>> GetAllAsync(PurchaseRequestStatus? status)
    {
        var query = _context.PurchaseRequests
            .Include(pr => pr.WorkOrder)
            .Include(pr => pr.RequestedByUser)
            .Include(pr => pr.Items)
                .ThenInclude(i => i.Product)
            .Include(pr => pr.Items)
                .ThenInclude(i => i.Material)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(pr => pr.Status == status.Value);

        var list = await query
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<PurchaseRequestListDto?> GetByIdAsync(int id)
    {
        var pr = await _context.PurchaseRequests
            .Include(x => x.WorkOrder)
            .Include(x => x.RequestedByUser)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .Include(x => x.Items)
                .ThenInclude(i => i.Material)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (pr == null)
            return null;

        return MapToDto(pr);
    }

    public async Task<bool> UpdateStatusAsync(int id, PurchaseRequestStatus status, string? notes)
    {
        var pr = await _context.PurchaseRequests
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (pr == null)
            return false;

        var currentStatus = pr.Status;

        if (currentStatus == status)
        {
            pr.Notes = notes;
            pr.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        if (!IsTransitionAllowed(currentStatus, status))
            return false;

        if (currentStatus == PurchaseRequestStatus.Ordered && status == PurchaseRequestStatus.Received)
            await ApplyReceivedStockAsync(pr.Items);

        pr.Status = status;
        pr.Notes = notes;
        pr.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private static bool IsTransitionAllowed(PurchaseRequestStatus currentStatus, PurchaseRequestStatus newStatus)
    {
        return currentStatus switch
        {
            PurchaseRequestStatus.Pending =>
                newStatus == PurchaseRequestStatus.Approved ||
                newStatus == PurchaseRequestStatus.Rejected,

            PurchaseRequestStatus.Approved =>
                false,

            PurchaseRequestStatus.Ordered =>
                newStatus == PurchaseRequestStatus.Received,

            PurchaseRequestStatus.Rejected => false,
            PurchaseRequestStatus.Received => false,
            _ => false
        };
    }

    private async Task ApplyReceivedStockAsync(IEnumerable<PurchaseRequestItem> items)
    {
        var productIds = items
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        var materialIds = items
            .Where(i => i.MaterialId.HasValue)
            .Select(i => i.MaterialId!.Value)
            .Distinct()
            .ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var materials = await _context.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        foreach (var item in items)
        {
            if (item.ProductId.HasValue && products.TryGetValue(item.ProductId.Value, out var product))
                product.StockQuantity += item.MissingQuantity;

            if (item.MaterialId.HasValue && materials.TryGetValue(item.MaterialId.Value, out var material))
                material.StockQuantity += item.MissingQuantity;
        }
    }

    private static PurchaseRequestListDto MapToDto(PurchaseRequest pr)
    {
        return new PurchaseRequestListDto
        {
            Id = pr.Id,
            WorkOrderId = pr.WorkOrderId,
            WorkOrderNumber = pr.WorkOrder?.OrderNumber ?? string.Empty,
            WorkOrderTitle = pr.WorkOrder?.Title ?? string.Empty,
            RequestedByUserId = pr.RequestedByUserId,
            RequestedByUserName = pr.RequestedByUser != null
                ? (pr.RequestedByUser.FirstName + " " + pr.RequestedByUser.LastName).Trim()
                : string.Empty,
            Status = pr.Status,
            Notes = pr.Notes,
            CreatedAt = pr.CreatedAt,
            UpdatedAt = pr.UpdatedAt,
            Items = pr.Items.Select(i => new PurchaseRequestItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductCode = i.Product?.Code,
                ProductName = i.Product?.Name,
                MaterialId = i.MaterialId,
                MaterialCode = i.Material?.Code,
                MaterialName = i.Material?.Name,
                RequiredQuantity = i.RequiredQuantity,
                CurrentStockQuantity = i.CurrentStockQuantity,
                MissingQuantity = i.MissingQuantity,
                Notes = i.Notes
            }).ToList()
        };
    }

    private static List<MissingComponent> FlattenMissing(IEnumerable<BomRequirementNodeDto> nodes)
    {
        var result = new List<MissingComponent>();

        foreach (var node in nodes)
        {
            if (!node.IsStockSufficient)
            {
                var missing = node.TotalRequired - node.StockQuantity;
                if (missing > 0)
                {
                    result.Add(new MissingComponent
                    {
                        ProductId = node.ComponentType == "Product" ? node.ComponentProductId : null,
                        MaterialId = node.ComponentType == "Material" ? node.MaterialId : null,
                        RequiredQuantity = node.TotalRequired,
                        CurrentStockQuantity = node.StockQuantity,
                        MissingQuantity = missing,
                        Notes = "İş emri ihtiyacı için otomatik oluşturuldu."
                    });
                }
            }

            if (node.Children != null && node.Children.Count > 0)
            {
                result.AddRange(FlattenMissing(node.Children));
            }
        }

        // Aynı ürün/malzeme birden fazla daldan gelirse birleştir
        var merged = result
            .GroupBy(x => new { x.ProductId, x.MaterialId })
            .Select(g => new MissingComponent
            {
                ProductId = g.Key.ProductId,
                MaterialId = g.Key.MaterialId,
                RequiredQuantity = g.Sum(x => x.RequiredQuantity),
                CurrentStockQuantity = g.Max(x => x.CurrentStockQuantity),
                MissingQuantity = g.Sum(x => x.MissingQuantity),
                Notes = "Birleştirilmiş eksik kalem"
            })
            .ToList();

        return merged;
    }

    private sealed class MissingComponent
    {
        public int? ProductId { get; set; }
        public int? MaterialId { get; set; }
        public int RequiredQuantity { get; set; }
        public int CurrentStockQuantity { get; set; }
        public int MissingQuantity { get; set; }
        public string? Notes { get; set; }
    }
}