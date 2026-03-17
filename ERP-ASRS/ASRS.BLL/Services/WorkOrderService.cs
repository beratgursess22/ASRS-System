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
				ProductId = w.ProductId,
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
			ProductId = w.ProductId,
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

	public async Task<WorkOrderStatusUpdateResult> UpdateStatusAsync(int id, WorkOrderStatus newStatus)
	{
		var workOrder = await _context.WorkOrders.FindAsync(id);
		if (workOrder == null)
			return WorkOrderStatusUpdateResult.WorkOrderNotFound;

		var previousStatus = workOrder.Status;

		if (previousStatus == newStatus)
			return WorkOrderStatusUpdateResult.Success;

		if (!IsTransitionAllowed(previousStatus, newStatus))
			return WorkOrderStatusUpdateResult.InvalidTransition;

		if ((previousStatus == WorkOrderStatus.Draft || previousStatus == WorkOrderStatus.WaitingForMaterial)
			&& newStatus == WorkOrderStatus.Approved
			&& !workOrder.IsStockConsumed)
		{
			var requiredProducts = new Dictionary<int, int>();
			var requiredMaterials = new Dictionary<int, int>();

			var expanded = await BuildNestedRequirementsAsync(
				rootProductId: workOrder.ProductId,
				workOrderQuantity: workOrder.Quantity,
				requiredProducts: requiredProducts,
				requiredMaterials: requiredMaterials);

			if (!expanded)
				return WorkOrderStatusUpdateResult.BomCycleDetected;

			var consumed = await ConsumeBomStockAsync(workOrder);
			if (!consumed)
				return WorkOrderStatusUpdateResult.StockInsufficient;

			workOrder.IsStockConsumed = true;
			workOrder.StockConsumedAt = DateTime.UtcNow;
		}
		if ((previousStatus == WorkOrderStatus.Approved || previousStatus == WorkOrderStatus.InProgress)
			&& newStatus == WorkOrderStatus.Cancelled
			&& workOrder.IsStockConsumed)
		{
			var restoreProducts = new Dictionary<int, int>();
			var restoreMaterials = new Dictionary<int, int>();

			var restoreExpanded = await BuildNestedRequirementsAsync(
				rootProductId: workOrder.ProductId,
				workOrderQuantity: workOrder.Quantity,
				requiredProducts: restoreProducts,
				requiredMaterials: restoreMaterials);

			if (!restoreExpanded)
				return WorkOrderStatusUpdateResult.BomCycleDetected;

			var restored = await RestoreBomStockAsync(workOrder);
			if (!restored)
				return WorkOrderStatusUpdateResult.RestoreFailed;
			workOrder.IsStockConsumed = false;
			workOrder.StockConsumedAt = null;
		}
		if (newStatus == WorkOrderStatus.Completed && workOrder.CompletedAt == null)
			workOrder.CompletedAt = DateTime.UtcNow;

		workOrder.Status = newStatus;
		await _context.SaveChangesAsync();
		return WorkOrderStatusUpdateResult.Success;
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
		var requiredProducts = new Dictionary<int, int>();
		var requiredMaterials = new Dictionary<int, int>();

		var expanded = await BuildNestedRequirementsAsync(
		rootProductId: workOrder.ProductId,
		workOrderQuantity: workOrder.Quantity,
		requiredProducts: requiredProducts,
		requiredMaterials: requiredMaterials);

		if (!expanded)
			return false;

		var products = await _context.Products
			.Where(p => requiredProducts.Keys.Contains(p.Id))
			.ToDictionaryAsync(p => p.Id);

		var materials = await _context.Materials
			.Where(m => requiredMaterials.Keys.Contains(m.Id))
			.ToDictionaryAsync(m => m.Id);

		foreach (var kv in requiredProducts)
		{
			if (!products.TryGetValue(kv.Key, out var product))
				return false;

			if (product.StockQuantity < kv.Value)
				return false;
		}

		foreach (var kv in requiredMaterials)
		{
			if (!materials.TryGetValue(kv.Key, out var material))
				return false;

			if (material.StockQuantity < kv.Value)
				return false;
		}

		foreach (var kv in requiredProducts)
			products[kv.Key].StockQuantity -= kv.Value;

		foreach (var kv in requiredMaterials)
			materials[kv.Key].StockQuantity -= kv.Value;

		return true;
	}

	private async Task<bool> RestoreBomStockAsync(WorkOrder workOrder)
	{
		var requiredProducts = new Dictionary<int, int>();
		var requiredMaterials = new Dictionary<int, int>();

		var expanded = await BuildNestedRequirementsAsync(
			rootProductId: workOrder.ProductId,
			workOrderQuantity: workOrder.Quantity,
			requiredProducts: requiredProducts,
			requiredMaterials: requiredMaterials);

		if (!expanded)
			return false;

		var products = await _context.Products
			.Where(p => requiredProducts.Keys.Contains(p.Id))
			.ToDictionaryAsync(p => p.Id);

		var materials = await _context.Materials
			.Where(m => requiredMaterials.Keys.Contains(m.Id))
			.ToDictionaryAsync(m => m.Id);

		foreach (var kv in requiredProducts)
		{
			if (!products.TryGetValue(kv.Key, out var product))
				return false;

			product.StockQuantity += kv.Value;
		}

		foreach (var kv in requiredMaterials)
		{
			if (!materials.TryGetValue(kv.Key, out var material))
				return false;

			material.StockQuantity += kv.Value;
		}

		return true;
	}

	private async Task<bool> BuildNestedRequirementsAsync(int rootProductId, int workOrderQuantity, Dictionary<int, int> requiredProducts, Dictionary<int, int> requiredMaterials)
	{
		if (workOrderQuantity <= 0)
			return false;

		var visiting = new HashSet<int>();

		return await ExpandProductBomAsync(
			productId: rootProductId,
			multiplier: workOrderQuantity,
			consumeAsLeafProduct: false, // kök ürünü stoktan düşmüyoruz
			visiting: visiting,
			requiredProducts: requiredProducts,
			requiredMaterials: requiredMaterials);
	}

	private async Task<bool> ExpandProductBomAsync(int productId, int multiplier, bool consumeAsLeafProduct, HashSet<int> visiting, Dictionary<int, int> requiredProducts, Dictionary<int, int> requiredMaterials)
	{
		if (multiplier <= 0)
			return false;

		var bomItems = await _context.BillOfMaterials
			.Where(b => b.ProductId == productId)
			.ToListAsync();

		// Alt BOM yoksa bu bir leaf ürün gibi tüketilir (kök ürün hariç)
		if (bomItems.Count == 0)
		{
			if (consumeAsLeafProduct)
				AddRequiredQuantity(requiredProducts, productId, multiplier);
			return true;
		}

		// Döngü koruması: A -> B -> A
		if (!visiting.Add(productId))
			return false;

		foreach (var item in bomItems)
		{
			var requiredLong = (long)item.RequiredQuantity * multiplier;
			if (requiredLong <= 0 || requiredLong > int.MaxValue)
			{
				visiting.Remove(productId);
				return false;
			}

			var requiredQty = (int)requiredLong;
			if (item.ComponentProductId.HasValue)
			{
				var ok = await ExpandProductBomAsync(
					productId: item.ComponentProductId.Value,
					multiplier: requiredQty,
					consumeAsLeafProduct: true,
					visiting: visiting,
					requiredProducts: requiredProducts,
					requiredMaterials: requiredMaterials);
				if (!ok)
				{
					visiting.Remove(productId);
					return false;
				}
			}
			else if (item.MaterialId.HasValue)
			{
				AddRequiredQuantity(requiredMaterials, item.MaterialId.Value, requiredQty);
			}
			else
			{
				visiting.Remove(productId);
				return false;
			}
		}
		visiting.Remove(productId);
		return true;
	}

	private static void AddRequiredQuantity(Dictionary<int, int> map, int id, int amount)
	{
		if (map.TryGetValue(id, out var current))
			map[id] = checked(current + amount);
		else
			map[id] = amount;
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