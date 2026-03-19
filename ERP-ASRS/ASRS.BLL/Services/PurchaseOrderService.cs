using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ASRS.BLL.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
	private readonly AppDbContext _context;

	public PurchaseOrderService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<bool> CreateFromPurchaseRequestAsync(CreatePurchaseOrderDto dto, string createdByUserId)
	{
		if (string.IsNullOrWhiteSpace(createdByUserId))
			return false;

		var pr = await _context.PurchaseRequests
		.Include(x => x.WorkOrder)
		.Include(x => x.Items)
		.FirstOrDefaultAsync(x => x.Id == dto.PurchaseRequestId);

		if (pr == null)
			return false;

		if (pr.Status != PurchaseRequestStatus.Approved)
			return false;

		var alreadyExists = await _context.PurchaseOrders
		.AnyAsync(po => po.PurchaseRequestId == dto.PurchaseRequestId);
		if (alreadyExists)
			return false;

		var seq = await _context.PurchaseOrders.CountAsync() + 1;
		var orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{seq:D3}";

		var po = new PurchaseOrder
		{
			OrderNumber = orderNumber,
			PurchaseRequestId = pr.Id,
			CreatedByUserId = createdByUserId,
			Status = PurchaseOrderStatus.Draft,
			CreatedAt = DateTime.UtcNow,
			Notes = dto.Notes
		};

		foreach (var item in pr.Items.Where(i => i.MissingQuantity > 0))
		{
			po.Items.Add(new PurchaseOrderItem
			{
				ProductId = item.ProductId,
				MaterialId = item.MaterialId,
				OrderedQuantity = item.MissingQuantity,
				ReceivedQuantity = 0,
				UnitPrice = 0m,
				Currency = "TRY",
				Notes = "PurchaseRequest kaleminden olusturuldu."
			});
		}

		if (!po.Items.Any())
			return false;

		_context.PurchaseOrders.Add(po);
		await _context.SaveChangesAsync();

		pr.Status = PurchaseRequestStatus.Ordered;
		pr.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return true;
	}

	public async Task<IEnumerable<PurchaseOrderListDto>> GetAllAsync(PurchaseOrderStatus? status)
	{
		var query = _context.PurchaseOrders
		.Include(po => po.PurchaseRequest)
		.ThenInclude(pr => pr.WorkOrder)
		.Include(po => po.Items)
		.ThenInclude(i => i.Product)
		.Include(po => po.Items)
		.ThenInclude(i => i.Material)
		.AsQueryable();

		if (status.HasValue)
			query = query.Where(po => po.Status == status.Value);

		var list = await query
		.OrderByDescending(po => po.CreatedAt)
		.ToListAsync();

		return list.Select(MapToDto).ToList();
	}

	public async Task<PurchaseOrderListDto?> GetByIdAsync(int id)
	{
		var po = await _context.PurchaseOrders
		.Include(x => x.PurchaseRequest)
		.ThenInclude(pr => pr.WorkOrder)
		.Include(x => x.Items)
		.ThenInclude(i => i.Product)
		.Include(x => x.Items)
		.ThenInclude(i => i.Material)
		.FirstOrDefaultAsync(x => x.Id == id);

		if (po == null)
			return null;

		return MapToDto(po);
	}

	public async Task<bool> UpdateStatusAsync(int id, PurchaseOrderStatus newStatus)
	{
		var po = await _context.PurchaseOrders.FindAsync(id);
		if (po == null)
			return false;
		if (newStatus == PurchaseOrderStatus.PartiallyReceived || newStatus == PurchaseOrderStatus.Received)
			return false;
		if (po.Status == newStatus)
			return true;
		if (!IsTransitionAllowed(po.Status, newStatus))
			return false;

		po.Status = newStatus;
		if (newStatus == PurchaseOrderStatus.Approved)
			po.ApprovedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> ReceiveItemAsync(ReceivePurchaseOrderItemDto dto)
	{
		if (dto.ReceivedQuantity <= 0)
			return false;

		var po = await _context.PurchaseOrders
		.Include(x => x.Items)
		.Include(x => x.PurchaseRequest)
		.FirstOrDefaultAsync(x => x.Id == dto.PurchaseOrderId);

		if (po == null)
			return false;
		if (po.Status != PurchaseOrderStatus.Approved && po.Status != PurchaseOrderStatus.PartiallyReceived)
			return false;
		var item = po.Items.FirstOrDefault(i => i.Id == dto.PurchaseOrderItemId);
		if (item == null)
			return false;

		var remaining = item.OrderedQuantity - item.ReceivedQuantity;
		if (dto.ReceivedQuantity > remaining)
			return false;
		if (item.ProductId.HasValue)
		{
			var product = await _context.Products.FindAsync(item.ProductId.Value);
			if (product == null) return false;
			product.StockQuantity += dto.ReceivedQuantity;
		}
		else if (item.MaterialId.HasValue)
		{
			var material = await _context.Materials.FindAsync(item.MaterialId.Value);
			if (material == null) return false;
			material.StockQuantity += dto.ReceivedQuantity;
		}
		else
		{
			return false;
		}

		item.ReceivedQuantity += dto.ReceivedQuantity;
		var allReceived = po.Items.All(i => i.ReceivedQuantity >= i.OrderedQuantity);
		po.Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
		if (allReceived)
		{
			po.CompletedAt = DateTime.UtcNow;
			po.PurchaseRequest.Status = PurchaseRequestStatus.Received;
			po.PurchaseRequest.UpdatedAt = DateTime.UtcNow;
		}

		await _context.SaveChangesAsync();
		return true;
	}

	private static bool IsTransitionAllowed(PurchaseOrderStatus current, PurchaseOrderStatus next)
	{
		return current switch
		{
			PurchaseOrderStatus.Draft =>
			next == PurchaseOrderStatus.Approved || next == PurchaseOrderStatus.Cancelled,

			PurchaseOrderStatus.Approved =>
			next == PurchaseOrderStatus.Cancelled,

			PurchaseOrderStatus.PartiallyReceived =>
			next == PurchaseOrderStatus.Cancelled,

			PurchaseOrderStatus.Received => false,
			PurchaseOrderStatus.Cancelled => false,
			_ => false
		};
	}

	private static PurchaseOrderListDto MapToDto(PurchaseOrder po)
	{
		return new PurchaseOrderListDto
		{
			Id = po.Id,
			OrderNumber = po.OrderNumber,
			PurchaseRequestId = po.PurchaseRequestId,
			WorkOrderNumber = po.PurchaseRequest?.WorkOrder?.OrderNumber ?? string.Empty,
			Status = po.Status,
			CreatedAt = po.CreatedAt,
			Notes = po.Notes,
			Items = po.Items.Select(i => new PurchaseOrderItemDto
			{
				Id = i.Id,
				ProductId = i.ProductId,
				ProductCode = i.Product?.Code,
				ProductName = i.Product?.Name,
				MaterialId = i.MaterialId,
				MaterialCode = i.Material?.Code,
				MaterialName = i.Material?.Name,
				OrderedQuantity = i.OrderedQuantity,
				ReceivedQuantity = i.ReceivedQuantity,
				UnitPrice = i.UnitPrice,
				Currency = i.Currency
			}).ToList()
		};
	}
}