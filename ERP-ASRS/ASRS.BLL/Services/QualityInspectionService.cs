using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ASRS.BLL.Services;

public class QualityInspectionService : IQualityInspectionService
{
	private readonly AppDbContext _context;

	public QualityInspectionService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<QualityInspectionListDto>> GetAllAsync(InspectionType? inspectionType, InspectionStatus? status)
	{
		var query = _context.QualityInspections
			.Include(x => x.PurchaseOrder)
			.Include(x => x.WorkOrder)
			.Include(x => x.InspectedByUser)
			.Include(x => x.Defects)
			.AsQueryable();

		if (inspectionType.HasValue)
			query = query.Where(x => x.InspectionType == inspectionType.Value);
		if (status.HasValue)
			query = query.Where(x => x.Status == status.Value);

		var list = await query
			.OrderByDescending(x => x.CreatedAt)
			.ToListAsync();

		return list.Select(x => new QualityInspectionListDto
		{
			Id = x.Id,
			InspectionNumber = x.InspectionNumber,
			InspectionType = x.InspectionType,
			Status = x.Status,
			UsageDecision = x.UsageDecision,
			PurchaseOrderId = x.PurchaseOrderId,
			PurchaseOrderNumber = x.PurchaseOrder?.OrderNumber,
			PurchaseOrderItemId = x.PurchaseOrderItemId,
			WorkOrderId = x.WorkOrderId,
			WorkOrderNumber = x.WorkOrder?.OrderNumber,
			InspectedByUserId = x.InspectedByUserId,
			InspectedByUserName = x.InspectedByUser == null
				? null
				: (x.InspectedByUser.FirstName + " " + x.InspectedByUser.LastName).Trim(),
			InspectionDate = x.InspectionDate,
			CreatedAt = x.CreatedAt,
			DefectCount = x.Defects.Count
		}).ToList();
	}

	public async Task<QualityInspectionDetailDto?> GetByIdAsync(int id)
	{
		var inspection = await _context.QualityInspections
			.Include(x => x.PurchaseOrder)
			.Include(x => x.WorkOrder)
			.Include(x => x.InspectedByUser)
			.Include(x => x.Items)
			.Include(x => x.Defects)
				.ThenInclude(d => d.CapaActions)
					.ThenInclude(c => c.ResponsibleUser)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (inspection == null)
			return null;

		return new QualityInspectionDetailDto
		{
			Id = inspection.Id,
			InspectionNumber = inspection.InspectionNumber,
			InspectionType = inspection.InspectionType,
			Status = inspection.Status,
			UsageDecision = inspection.UsageDecision,
			PurchaseOrderId = inspection.PurchaseOrderId,
			PurchaseOrderNumber = inspection.PurchaseOrder?.OrderNumber,
			PurchaseOrderItemId = inspection.PurchaseOrderItemId,
			WorkOrderId = inspection.WorkOrderId,
			WorkOrderNumber = inspection.WorkOrder?.OrderNumber,
			InspectedByUserId = inspection.InspectedByUserId,
			InspectedByUserName = inspection.InspectedByUser == null
				? null
				: (inspection.InspectedByUser.FirstName + " " + inspection.InspectedByUser.LastName).Trim(),
			InspectionDate = inspection.InspectionDate,
			Notes = inspection.Notes,
			CreatedAt = inspection.CreatedAt,
			UpdatedAt = inspection.UpdatedAt,
			ClosedAt = inspection.ClosedAt,
			Items = inspection.Items.Select(i => new QualityInspectionItemDto
			{
				Id = i.Id,
				CharacteristicName = i.CharacteristicName,
				ExpectedValue = i.ExpectedValue,
				ActualValue = i.ActualValue,
				IsPassed = i.IsPassed,
				Notes = i.Notes
			}).ToList(),
			Defects = inspection.Defects.Select(d => new QualityDefectDto
			{
				Id = d.Id,
				QualityInspectionId = d.QualityInspectionId,
				Title = d.Title,
				Description = d.Description,
				Severity = d.Severity,
				IsResolved = d.IsResolved,
				CreatedAt = d.CreatedAt,
				ResolvedAt = d.ResolvedAt,
				CapaActions = d.CapaActions.Select(c => new CapaActionDto
				{
					Id = c.Id,
					QualityDefectId = c.QualityDefectId,
					ActionDescription = c.ActionDescription,
					ResponsibleUserId = c.ResponsibleUserId,
					ResponsibleUserName = c.ResponsibleUser == null
						? null
						: (c.ResponsibleUser.FirstName + " " + c.ResponsibleUser.LastName).Trim(),
					DueDate = c.DueDate,
					Status = c.Status,
					CompletionNote = c.CompletionNote,
					CreatedAt = c.CreatedAt,
					CompletedAt = c.CompletedAt
				}).ToList()
			}).ToList()
		};
	}

	public async Task<bool> CreateAsync(CreateQualityInspectionDto dto, string inspectedByUserId)
	{
		if (string.IsNullOrWhiteSpace(inspectedByUserId))
			return false;
		if (dto.PurchaseOrderId.HasValue)
		{
			var poExists = await _context.PurchaseOrders.AnyAsync(x => x.Id == dto.PurchaseOrderId.Value);
			if (!poExists)
				return false;
		}
		if (dto.PurchaseOrderItemId.HasValue)
		{
			var itemExists = await _context.PurchaseOrderItems.AnyAsync(x => x.Id == dto.PurchaseOrderItemId.Value);
			if (!itemExists)
				return false;
		}
		if (dto.WorkOrderId.HasValue)
		{
			var woExists = await _context.WorkOrders.AnyAsync(x => x.Id == dto.WorkOrderId.Value);
			if (!woExists)
				return false;
		}

		var seq = await _context.QualityInspections.CountAsync() + 1;
		var number = $"QIN-{DateTime.UtcNow:yyyyMMdd}-{seq:D3}";

		var inspection = new QualityInspection
		{
			InspectionNumber = number,
			InspectionType = dto.InspectionType,
			Status = InspectionStatus.Pending,
			PurchaseOrderId = dto.PurchaseOrderId,
			PurchaseOrderItemId = dto.PurchaseOrderItemId,
			WorkOrderId = dto.WorkOrderId,
			InspectedByUserId = inspectedByUserId,
			InspectionDate = dto.InspectionDate ?? DateTime.UtcNow,
			Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
			CreatedAt = DateTime.UtcNow
		};

		foreach (var item in dto.Items)
		{
			if (string.IsNullOrWhiteSpace(item.CharacteristicName))
				continue;

			inspection.Items.Add(new QualityInspectionItem
			{
				CharacteristicName = item.CharacteristicName.Trim(),
				ExpectedValue = item.ExpectedValue?.Trim(),
				ActualValue = item.ActualValue?.Trim(),
				IsPassed = item.IsPassed,
				Notes = item.Notes?.Trim()
			});
		}

		_context.QualityInspections.Add(inspection);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> UpdateStatusAsync(int id, InspectionStatus status, string? notes)
	{
		var inspection = await _context.QualityInspections.FirstOrDefaultAsync(x => x.Id == id);
		if (inspection == null)
			return false;

		if (inspection.Status == status)
		{
			inspection.Notes = string.IsNullOrWhiteSpace(notes) ? inspection.Notes : notes.Trim();
			inspection.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return true;
		}

		if (!IsTransitionAllowed(inspection.Status, status))
			return false;

		inspection.Status = status;
		if (!string.IsNullOrWhiteSpace(notes))
			inspection.Notes = notes.Trim();

		inspection.UpdatedAt = DateTime.UtcNow;
		if (status == InspectionStatus.Closed)
			inspection.ClosedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> DecideAsync(int id, UsageDecision decision, string? notes)
	{
		var inspection = await _context.QualityInspections.FirstOrDefaultAsync(x => x.Id == id);
		if (inspection == null)
			return false;

		if (inspection.Status == InspectionStatus.Closed)
			return false;

		inspection.UsageDecision = decision;
		inspection.Status = decision switch
		{
			UsageDecision.Passed => InspectionStatus.Passed,
			UsageDecision.ConditionalPass => InspectionStatus.ConditionalPass,
			UsageDecision.Rejected => InspectionStatus.Rejected,
			_ => inspection.Status
		};

		if (!string.IsNullOrWhiteSpace(notes))
			inspection.Notes = notes.Trim();

		inspection.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> AddInspectionItemAsync(int inspectionId, CreateQualityInspectionItemDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.CharacteristicName))
			return false;

		var inspection = await _context.QualityInspections.FirstOrDefaultAsync(x => x.Id == inspectionId);
		if (inspection == null)
			return false;

		if (inspection.Status == InspectionStatus.Closed)
			return false;

		var item = new QualityInspectionItem
		{
			QualityInspectionId = inspectionId,
			CharacteristicName = dto.CharacteristicName.Trim(),
			ExpectedValue = dto.ExpectedValue?.Trim(),
			ActualValue = dto.ActualValue?.Trim(),
			IsPassed = dto.IsPassed,
			Notes = dto.Notes?.Trim()
		};

		_context.QualityInspectionItems.Add(item);

		inspection.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return true;
	}

	private static bool IsTransitionAllowed(InspectionStatus current, InspectionStatus next)
	{
		return current switch
		{
			InspectionStatus.Pending =>
				next == InspectionStatus.InReview ||
				next == InspectionStatus.Closed,

			InspectionStatus.InReview =>
				next == InspectionStatus.Passed ||
				next == InspectionStatus.ConditionalPass ||
				next == InspectionStatus.Rejected ||
				next == InspectionStatus.Closed,

			InspectionStatus.Passed =>
				next == InspectionStatus.Closed,

			InspectionStatus.ConditionalPass =>
				next == InspectionStatus.Closed,

			InspectionStatus.Rejected =>
				next == InspectionStatus.Closed,

			InspectionStatus.Closed => false,
			_ => false
		};
	}
}
