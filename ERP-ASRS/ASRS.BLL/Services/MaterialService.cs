namespace ASRS.BLL.Services;

using ASRS.Core.DTOs;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using ASRS.Core.Entities;
using Microsoft.EntityFrameworkCore;


public class MaterialService : IMaterialService
{
	private readonly AppDbContext _context;

	public MaterialService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<MaterialListDto>> GetAllMaterialsAsync(string? search)
	{
		var query = _context.Materials.AsQueryable();
		if (!string.IsNullOrWhiteSpace(search))
			query = query.Where(m => m.Name.Contains(search) || m.Code.Contains(search));
		var materials = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
		return materials.Select(m => new MaterialListDto
		{
			Id = m.Id,
			Code = m.Code,
			Name = m.Name,
			Unit = m.Unit,
			StockQuantity = m.StockQuantity,
			MinStockLevel = m.MinStockLevel,
			Description = m.Description,
			IsActive = m.IsActive
		});
	}

	public async Task<MaterialListDto?> GetMaterialByIdAsync(int id)
	{
		var m = await _context.Materials.FindAsync(id);
		if (m == null)
			return null;

		return new MaterialListDto
		{
			Id = m.Id,
			Code = m.Code,
			Name = m.Name,
			Unit = m.Unit,
			StockQuantity = m.StockQuantity,
			MinStockLevel = m.MinStockLevel,
			Description = m.Description,
			IsActive = m.IsActive
		};
	}

	public async Task<bool> CreateMaterialAsync(CreateMaterialDto dto)
	{
		var material = new Material
		{
			Code = dto.Code,
			Name = dto.Name,
			Unit = dto.Unit,
			StockQuantity = dto.StockQuantity,
			MinStockLevel = dto.MinStockLevel,
			Description = dto.Description,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		_context.Materials.Add(material);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> UpdateMaterialAsync(int id, CreateMaterialDto dto)
	{
		var material = await _context.Materials.FindAsync(id);
		if (material == null)
			return false;

		material.Code = dto.Code;
		material.Name = dto.Name;
		material.Unit = dto.Unit;
		material.StockQuantity = dto.StockQuantity;
		material.MinStockLevel = dto.MinStockLevel;
		material.Description = dto.Description;

		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> DeleteMaterialAsync(int id)
	{
		var material = await _context.Materials.FindAsync(id);
		if (material == null)
			return false;

		_context.Materials.Remove(material);
		await _context.SaveChangesAsync();
		return true;
	}
}
