using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ASRS.BLL.Services;

public class QualityDefectService : IQualityDefectService
{
	private readonly AppDbContext _context;

	public QualityDefectService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<QualityDefectDto>> GetByInspectionIdAsync(int inspectionId)
	{
		var rows = await _context.QualityDefects
			.Include(x => x.CapaActions)
				.ThenInclude(c => c.ResponsibleUser)
			.Where(x => x.QualityInspectionId == inspectionId)
			.OrderByDescending(x => x.CreatedAt)
			.ToListAsync();

		return rows.Select(x => new QualityDefectDto
		{
			Id = x.Id,
			QualityInspectionId = x.QualityInspectionId,
			Title = x.Title,
			Description = x.Description,
			Severity = x.Severity,
			IsResolved = x.IsResolved,
			CreatedAt = x.CreatedAt,
			ResolvedAt = x.ResolvedAt,
			CapaActions = x.CapaActions.Select(c => new CapaActionDto
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
		}).ToList();
	}

	public async Task<bool> CreateAsync(CreateQualityDefectDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Title))
			return false;

		var inspectionExists = await _context.QualityInspections.AnyAsync(x => x.Id == dto.QualityInspectionId);
		if (!inspectionExists)
			return false;

		var defect = new QualityDefect
		{
			QualityInspectionId = dto.QualityInspectionId,
			Title = dto.Title.Trim(),
			Description = dto.Description?.Trim(),
			Severity = dto.Severity,
			CreatedAt = DateTime.UtcNow
		};

		_context.QualityDefects.Add(defect);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> ResolveAsync(int defectId)
	{
		var defect = await _context.QualityDefects.FirstOrDefaultAsync(x => x.Id == defectId);
		if (defect == null)
			return false;

		if (defect.IsResolved)
			return true;

		defect.IsResolved = true;
		defect.ResolvedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return true;
	}
}
