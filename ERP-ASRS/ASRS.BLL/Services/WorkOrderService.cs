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

		workOrder.Status = newStatus;

		if (newStatus == WorkOrderStatus.Completed)
			workOrder.CompletedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return true;
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