using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ASRS.BLL.Services;

public class WorkOrderService : IWorkOrderService
{
	private readonly AppDbContext _context;

	public WorkOrderService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<WorkOrderListDto>> GetAllAsync(string? search, WorkOrderStatus? status)
	{
		var query = _context.WorkOrders
			.Include(w => w.Product)
			.Include(w => w.Department)
			.Include(w => w.AssignedUser)
			.Include(w => w.CreatedByUser)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			query = query.Where(w =>
				w.Title.Contains(search) ||
				w.OrderNumber.Contains(search) ||
				w.Product.Name.Contains(search));
		}

		if (status.HasValue)
			query = query.Where(w => w.Status == status.Value);

		var list = await query.OrderByDescending(w => w.CreatedAt).ToListAsync();

		var result = new List<WorkOrderListDto>();
		foreach (var w in list)
		{
			string assignedUserName = string.Empty;
			if (w.AssignedUser != null)
				assignedUserName = w.AssignedUser.FirstName + " " + w.AssignedUser.LastName;

			result.Add(new WorkOrderListDto
			{
				Id = w.Id,
				OrderNumber = w.OrderNumber,
				Title = w.Title,
				ProductName = w.Product.Name,
				ProductCode = w.Product.Code,
				Quantity = w.Quantity,
				Priority = w.Priority,
				Status = w.Status,
				DepartmentName = w.Department?.Name,
				AssignedUserName = assignedUserName,
				PlannedStartDate = w.PlannedStartDate,
				PlannedEndDate = w.PlannedEndDate,
				CreatedAt = w.CreatedAt,
				CreatedByUserName = w.CreatedByUser != null
					? w.CreatedByUser.FirstName + " " + w.CreatedByUser.LastName
					: null
			});
		}

		return result;
	}

	public async Task<WorkOrderListDto?> GetByIdAsync(int id)
	{
		var w = await _context.WorkOrders
			.Include(w => w.Product)
			.Include(w => w.Department)
			.Include(w => w.AssignedUser)
			.Include(w => w.CreatedByUser)
			.FirstOrDefaultAsync(w => w.Id == id);

		if (w == null)
			return null;

		string assignedUserName = string.Empty;
		if (w.AssignedUser != null)
			assignedUserName = w.AssignedUser.FirstName + " " + w.AssignedUser.LastName;

		return new WorkOrderListDto
		{
			Id = w.Id,
			OrderNumber = w.OrderNumber,
			Title = w.Title,
			ProductName = w.Product.Name,
			ProductCode = w.Product.Code,
			Quantity = w.Quantity,
			Priority = w.Priority,
			Status = w.Status,
			DepartmentName = w.Department?.Name,
			AssignedUserName = assignedUserName,
			PlannedStartDate = w.PlannedStartDate,
			PlannedEndDate = w.PlannedEndDate,
			CreatedAt = w.CreatedAt,
			CreatedByUserName = w.CreatedByUser != null
				? w.CreatedByUser.FirstName + " " + w.CreatedByUser.LastName
				: null
		};
	}

	public async Task<bool> CreateAsync(CreateWorkOrderDto dto, string createdByUserId)
	{
		var count = await _context.WorkOrders.CountAsync();
		var orderNumber = "WO-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + (count + 1).ToString("D3");

		var workOrder = new WorkOrder
		{
			OrderNumber = orderNumber,
			Title = dto.Title,
			ProductId = dto.ProductId,
			Quantity = dto.Quantity,
			Priority = dto.Priority,
			Status = WorkOrderStatus.Draft,
			DepartmentId = dto.DepartmentId,
			AssignedUserId = dto.AssignedUserId,
			PlannedStartDate = dto.PlannedStartDate,
			PlannedEndDate = dto.PlannedEndDate,
			Notes = dto.Notes,
			CreatedByUserId = createdByUserId,
			CreatedAt = DateTime.UtcNow
		};

		_context.WorkOrders.Add(workOrder);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> UpdateStatusAsync(int id, WorkOrderStatus newStatus)
	{
		var workOrder = await _context.WorkOrders.FindAsync(id);
		if (workOrder == null)
			return false;

		var previousStatus = workOrder.Status;

		if (previousStatus == newStatus)
			return true;

		if (!IsTransitionAllowed(previousStatus, newStatus))
			return false;

		// Üretim akışına ilk girişte stok tüket (taslak / malzeme bekleniyor -> onaylandı)
		if ((previousStatus == WorkOrderStatus.Draft || previousStatus == WorkOrderStatus.WaitingForMaterial)
			&& newStatus == WorkOrderStatus.Approved
			&& !workOrder.IsStockConsumed)
		{
			var consumed = await ConsumeBomStockAsync(workOrder);
			if (!consumed)
				return false;

			workOrder.IsStockConsumed = true;
			workOrder.StockConsumedAt = DateTime.UtcNow;
		}

		// Onaylandı / Devam Ediyor -> İptal dönüşünde stok iade et
		if ((previousStatus == WorkOrderStatus.Approved || previousStatus == WorkOrderStatus.InProgress)
			&& newStatus == WorkOrderStatus.Cancelled
			&& workOrder.IsStockConsumed)
		{
			await RestoreBomStockAsync(workOrder);
			workOrder.IsStockConsumed = false;
			workOrder.StockConsumedAt = null;
		}

		if (newStatus == WorkOrderStatus.Completed && workOrder.CompletedAt == null)
			workOrder.CompletedAt = DateTime.UtcNow;

		workOrder.Status = newStatus;
		await _context.SaveChangesAsync();
		return true;
	}

	private static bool IsTransitionAllowed(WorkOrderStatus currentStatus, WorkOrderStatus newStatus)
	{
		return currentStatus switch
		{
			WorkOrderStatus.Draft =>
				newStatus == WorkOrderStatus.Approved ||
				newStatus == WorkOrderStatus.WaitingForMaterial ||
				newStatus == WorkOrderStatus.Cancelled,

			WorkOrderStatus.WaitingForMaterial =>
				newStatus == WorkOrderStatus.Approved ||
				newStatus == WorkOrderStatus.Cancelled,

			WorkOrderStatus.Approved =>
				newStatus == WorkOrderStatus.InProgress ||
				newStatus == WorkOrderStatus.Completed ||
				newStatus == WorkOrderStatus.Cancelled,

			WorkOrderStatus.InProgress =>
				newStatus == WorkOrderStatus.Completed ||
				newStatus == WorkOrderStatus.Cancelled,

			WorkOrderStatus.Completed => false,
			WorkOrderStatus.Cancelled => false,
			_ => false
		};
	}

	private async Task<bool> ConsumeBomStockAsync(WorkOrder workOrder)
	{
		var bomItems = await _context.BillOfMaterials
			.Include(b => b.ComponentProduct)
			.Include(b => b.Material)
			.Where(b => b.ProductId == workOrder.ProductId)
			.ToListAsync();

		foreach (var item in bomItems)
		{
			var totalRequired = item.RequiredQuantity * workOrder.Quantity;

			if (item.ComponentProductId.HasValue)
			{
				if (item.ComponentProduct == null || item.ComponentProduct.StockQuantity < totalRequired)
					return false;
			}
			else if (item.MaterialId.HasValue)
			{
				if (item.Material == null || item.Material.StockQuantity < totalRequired)
					return false;
			}
		}

		foreach (var item in bomItems)
		{
			var totalRequired = item.RequiredQuantity * workOrder.Quantity;

			if (item.ComponentProductId.HasValue && item.ComponentProduct != null)
				item.ComponentProduct.StockQuantity -= totalRequired;
			else if (item.MaterialId.HasValue && item.Material != null)
				item.Material.StockQuantity -= totalRequired;
		}

		return true;
	}

	private async Task RestoreBomStockAsync(WorkOrder workOrder)
	{
		var bomItems = await _context.BillOfMaterials
			.Include(b => b.ComponentProduct)
			.Include(b => b.Material)
			.Where(b => b.ProductId == workOrder.ProductId)
			.ToListAsync();

		foreach (var item in bomItems)
		{
			var totalRequired = item.RequiredQuantity * workOrder.Quantity;

			if (item.ComponentProductId.HasValue && item.ComponentProduct != null)
				item.ComponentProduct.StockQuantity += totalRequired;
			else if (item.MaterialId.HasValue && item.Material != null)
				item.Material.StockQuantity += totalRequired;
		}
	}

	public async Task<bool> DeleteAsync(int id)
	{
		var workOrder = await _context.WorkOrders.FindAsync(id);
		if (workOrder == null)
			return false;

		_context.WorkOrders.Remove(workOrder);
		await _context.SaveChangesAsync();
		return true;
	}
}