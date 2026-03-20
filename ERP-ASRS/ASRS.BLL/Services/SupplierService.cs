using ASRS.Core.Entities;
using ASRS.Core.Interfaces;
using ASRS.Core.DTOs;
using Microsoft.EntityFrameworkCore;
using ASRS.DAL.Context;

namespace ASRS.BLL.Services;


public class SupplierService : ISupplierService
{
	private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase)
	{
		"TRY",
		"USD",
		"EUR"
	};

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

	public async Task<IEnumerable<SupplierListDto>> GetActiveForPurchaseOrderAsync(int purchaseOrderId)
	{
		var po = await _context.PurchaseOrders
			.Include(x => x.Items)
			.FirstOrDefaultAsync(x => x.Id == purchaseOrderId);

		if (po == null)
			return Enumerable.Empty<SupplierListDto>();

		var productIds = po.Items
			.Where(i => i.ProductId.HasValue)
			.Select(i => i.ProductId!.Value)
			.Distinct()
			.ToList();

		var materialIds = po.Items
			.Where(i => i.MaterialId.HasValue)
			.Select(i => i.MaterialId!.Value)
			.Distinct()
			.ToList();

		if (!productIds.Any() && !materialIds.Any())
			return await GetActiveAsync();

		var supplierIds = await _context.SupplierItemPrices
			.Where(x =>
				(x.ProductId.HasValue && productIds.Contains(x.ProductId.Value)) ||
				(x.MaterialId.HasValue && materialIds.Contains(x.MaterialId.Value)))
			.Select(x => x.SupplierId)
			.Distinct()
			.ToListAsync();

		var list = await _context.Suppliers
			.Where(x => x.IsActive && supplierIds.Contains(x.Id))
			.OrderBy(x => x.Name)
			.ToListAsync();

		if (po.SupplierId.HasValue && !list.Any(x => x.Id == po.SupplierId.Value))
		{
			var selected = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == po.SupplierId.Value);
			if (selected != null)
				list.Add(selected);
		}

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

	public async Task<IEnumerable<SupplierItemPriceDto>> GetItemPricesAsync(int supplierId)
	{
		var rows = await _context.SupplierItemPrices
			.Include(x => x.Product)
			.Include(x => x.Material)
			.Where(x => x.SupplierId == supplierId)
			.OrderBy(x => x.ProductId.HasValue ? 0 : 1)
			.ThenBy(x => x.Product != null ? x.Product.Name : x.Material!.Name)
			.ToListAsync();

		return rows.Select(x => new SupplierItemPriceDto
		{
			SupplierId = x.SupplierId,
			ProductId = x.ProductId,
			ProductCode = x.Product?.Code,
			ProductName = x.Product?.Name,
			MaterialId = x.MaterialId,
			MaterialCode = x.Material?.Code,
			MaterialName = x.Material?.Name,
			UnitPrice = x.UnitPrice,
			Currency = string.IsNullOrWhiteSpace(x.Currency) ? "TRY" : x.Currency
		});
	}

	public async Task<bool> UpsertItemPriceAsync(UpsertSupplierItemPriceDto dto)
	{
		if (dto.UnitPrice < 0)
			return false;

		var hasProduct = dto.ProductId.HasValue;
		var hasMaterial = dto.MaterialId.HasValue;
		if (hasProduct == hasMaterial)
			return false;

		if (!TryNormalizeCurrency(dto.Currency, out var normalizedCurrency))
			return false;

		var supplierExists = await _context.Suppliers.AnyAsync(x => x.Id == dto.SupplierId && x.IsActive);
		if (!supplierExists)
			return false;

		if (dto.ProductId.HasValue)
		{
			var productExists = await _context.Products.AnyAsync(x => x.Id == dto.ProductId.Value && x.IsActive);
			if (!productExists)
				return false;
		}

		if (dto.MaterialId.HasValue)
		{
			var materialExists = await _context.Materials.AnyAsync(x => x.Id == dto.MaterialId.Value && x.IsActive);
			if (!materialExists)
				return false;
		}

		var row = await _context.SupplierItemPrices.FirstOrDefaultAsync(x =>
			x.SupplierId == dto.SupplierId &&
			x.ProductId == dto.ProductId &&
			x.MaterialId == dto.MaterialId);

		if (row == null)
		{
			row = new SupplierItemPrice
			{
				SupplierId = dto.SupplierId,
				ProductId = dto.ProductId,
				MaterialId = dto.MaterialId,
				UnitPrice = dto.UnitPrice,
				Currency = normalizedCurrency,
				UpdatedAt = DateTime.UtcNow
			};

			_context.SupplierItemPrices.Add(row);
		}
		else
		{
			row.UnitPrice = dto.UnitPrice;
			row.Currency = normalizedCurrency;
			row.UpdatedAt = DateTime.UtcNow;
		}

		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> DeleteItemPriceAsync(int supplierId, int? productId, int? materialId)
	{
		var row = await _context.SupplierItemPrices.FirstOrDefaultAsync(x =>
			x.SupplierId == supplierId &&
			x.ProductId == productId &&
			x.MaterialId == materialId);

		if (row == null)
			return false;

		_context.SupplierItemPrices.Remove(row);
		await _context.SaveChangesAsync();
		return true;
	}

	private static bool TryNormalizeCurrency(string? currency, out string normalized)
	{
		normalized = string.IsNullOrWhiteSpace(currency)
			? "TRY"
			: currency.Trim().ToUpperInvariant();

		return AllowedCurrencies.Contains(normalized);
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
