using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ASRS.BLL.Services;

public class CapaService : ICapaService
{
	private readonly AppDbContext _context;

	public CapaService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<CapaActionDto>> GetByDefectIdAsync(int defectId)
	{
		var rows = await _context.CapaActions
			.Include(x => x.ResponsibleUser)
			.Where(x => x.QualityDefectId == defectId)
			.OrderByDescending(x => x.CreatedAt)
			.ToListAsync();

		return rows.Select(x => new CapaActionDto
		{
			Id = x.Id,
			QualityDefectId = x.QualityDefectId,
			ActionDescription = x.ActionDescription,
			ResponsibleUserId = x.ResponsibleUserId,
			ResponsibleUserName = x.ResponsibleUser == null
				? null
				: (x.ResponsibleUser.FirstName + " " + x.ResponsibleUser.LastName).Trim(),
			DueDate = x.DueDate,
			Status = x.Status,
			CompletionNote = x.CompletionNote,
			CreatedAt = x.CreatedAt,
			CompletedAt = x.CompletedAt
		}).ToList();
	}

	public async Task<bool> CreateAsync(CreateCapaActionDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.ActionDescription))
			return false;

		var defectExists = await _context.QualityDefects.AnyAsync(x => x.Id == dto.QualityDefectId);
		if (!defectExists)
			return false;

		if (!string.IsNullOrWhiteSpace(dto.ResponsibleUserId))
		{
			var userExists = await _context.Users.AnyAsync(x => x.Id == dto.ResponsibleUserId);
			if (!userExists)
				return false;
		}

		var row = new CapaAction
		{
			QualityDefectId = dto.QualityDefectId,
			ActionDescription = dto.ActionDescription.Trim(),
			ResponsibleUserId = string.IsNullOrWhiteSpace(dto.ResponsibleUserId) ? null : dto.ResponsibleUserId,
			DueDate = dto.DueDate,
			Status = CapaStatus.Open,
			CreatedAt = DateTime.UtcNow
		};

		_context.CapaActions.Add(row);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> UpdateStatusAsync(int capaId, CapaStatus status, string? completionNote)
	{
		var row = await _context.CapaActions.FirstOrDefaultAsync(x => x.Id == capaId);
		if (row == null)
			return false;

		row.Status = status;

		if (status == CapaStatus.Completed || status == CapaStatus.Closed)
		{
			row.CompletedAt = DateTime.UtcNow;
			row.CompletionNote = string.IsNullOrWhiteSpace(completionNote) ? row.CompletionNote : completionNote.Trim();
		}
		else
		{
			row.CompletionNote = string.IsNullOrWhiteSpace(completionNote) ? null : completionNote.Trim();
		}

		await _context.SaveChangesAsync();
		return true;
	}
}
