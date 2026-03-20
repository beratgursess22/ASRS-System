using ASRS.Core.Entities;
using ASRS.Core.Interfaces;
using ASRS.Core.DTOs;
using Microsoft.EntityFrameworkCore;
using ASRS.DAL.Context;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ASRS.BLL.Services;


public class SupplierService : ISupplierService
{
	private readonly AppDbContext _context;

	public SupplierService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<SupplierListDto>> GetAllAsync(string? search)
	{
		var query = _context.Suppliers.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
			query = query.Where(x => x.Name.Contains(search) || x.Code.Contains(search));

		var list = await query
			.OrderByDescending(x => x.CreatedAt)
			.ToListAsync();

		return list.Select(MapToDto);
	}

	public async Task<IEnumerable<SupplierListDto>> GetActiveAsync()
	{
		var list = await _context.Suppliers
			.Where(x => x.IsActive)
			.OrderBy(x => x.Name)
			.ToListAsync();

		return list.Select(MapToDto);
	}

	public async Task<SupplierListDto?> GetByIdAsync(int id)
	{
		var entity = await _context.Suppliers.FindAsync(id);
		return entity == null ? null : MapToDto(entity);
	}

	public async Task<bool> CreateAsync(CreateSupplierDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
			return false;

		var code = dto.Code.Trim().ToUpperInvariant();

		var exists = await _context.Suppliers.AnyAsync(x => x.Code == code);
		if (exists)
			return false;

		var entity = new Supplier
		{
			Code = code,
			Name = dto.Name.Trim(),
			ContactPerson = dto.ContactPerson?.Trim(),
			Email = dto.Email?.Trim(),
			Phone = dto.Phone?.Trim(),
			Address = dto.Address?.Trim(),
			IsActive = dto.IsActive,
			CreatedAt = DateTime.UtcNow
		};

		_context.Suppliers.Add(entity);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> UpdateAsync(int id, CreateSupplierDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
			return false;

		var entity = await _context.Suppliers.FindAsync(id);
		if (entity == null)
			return false;

		var code = dto.Code.Trim().ToUpperInvariant();
		var duplicateCode = await _context.Suppliers.AnyAsync(x => x.Id != id && x.Code == code);
		if (duplicateCode)
			return false;

		entity.Code = code;
		entity.Name = dto.Name.Trim();
		entity.ContactPerson = dto.ContactPerson?.Trim();
		entity.Email = dto.Email?.Trim();
		entity.Phone = dto.Phone?.Trim();
		entity.Address = dto.Address?.Trim();
		entity.IsActive = dto.IsActive;

		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> DeleteAsync(int id)
	{
		var entity = await _context.Suppliers.FindAsync(id);
		if (entity == null)
			return false;

		_context.Suppliers.Remove(entity);
		await _context.SaveChangesAsync();
		return true;
	}

	private static SupplierListDto MapToDto(Supplier x) => new()
	{
		Id = x.Id,
		Code = x.Code,
		Name = x.Name,
		ContactPerson = x.ContactPerson,
		Email = x.Email,
		Phone = x.Phone,
		Address = x.Address,
		IsActive = x.IsActive
	};
}
