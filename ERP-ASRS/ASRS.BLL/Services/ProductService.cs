using ASRS.Core.DTOs;
using ASRS.Core.Entities;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ASRS.BLL.Services;

public class ProductService : IProductService
{
	private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase)
	{
		"TRY",
		"USD",
		"EUR"
	};

	private readonly AppDbContext _context;

	public ProductService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<ProductListDto>> GetAllProductsAsync(string? search)
	{
		var query = _context.Products.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
			query = query.Where(p => p.Name.Contains(search) || p.Code.Contains(search) || p.Category.Contains(search));

		var products = await query.ToListAsync();

		var result = new List<ProductListDto>();
		foreach (var p in products)
		{
			result.Add(new ProductListDto
			{
				Id = p.Id,
				Code = p.Code,
				Name = p.Name,
				Category = p.Category,
				Unit = p.Unit,
				StockQuantity = p.StockQuantity,
				MinStockLevel = p.MinStockLevel,
				DefaultUnitPrice = p.DefaultUnitPrice,
				DefaultCurrency = NormalizeCurrency(p.DefaultCurrency),
				IsActive = p.IsActive
			});
		}
		return result;
	}

	public async Task<ProductListDto?> GetProductByIdAsync(int id)
	{
		var p = await _context.Products.FindAsync(id);
		if (p == null)
			return null;

		return new ProductListDto
		{
			Id = p.Id,
			Code = p.Code,
			Name = p.Name,
			Category = p.Category,
			Unit = p.Unit,
			StockQuantity = p.StockQuantity,
			MinStockLevel = p.MinStockLevel,
			DefaultUnitPrice = p.DefaultUnitPrice,
			DefaultCurrency = NormalizeCurrency(p.DefaultCurrency),
			IsActive = p.IsActive
		};
	}

	public async Task<bool> CreateProductAsync(CreateProductDto dto)
	{
		if (dto.DefaultUnitPrice < 0)
			return false;

		if (!TryNormalizeCurrency(dto.DefaultCurrency, out var normalizedCurrency))
			return false;

		var product = new Product
		{
			Code = dto.Code,
			Name = dto.Name,
			Category = dto.Category,
			Unit = dto.Unit,
			StockQuantity = dto.StockQuantity,
			MinStockLevel = dto.MinStockLevel,
			DefaultUnitPrice = dto.DefaultUnitPrice,
			DefaultCurrency = normalizedCurrency,
			Description = dto.Description,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		_context.Products.Add(product);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> UpdateProductAsync(int id, CreateProductDto dto)
	{
		if (dto.DefaultUnitPrice < 0)
			return false;

		if (!TryNormalizeCurrency(dto.DefaultCurrency, out var normalizedCurrency))
			return false;

		var product = await _context.Products.FindAsync(id);
		if (product == null)
			return false;

		product.Code = dto.Code;
		product.Name = dto.Name;
		product.Category = dto.Category;
		product.Unit = dto.Unit;
		product.StockQuantity = dto.StockQuantity;
		product.MinStockLevel = dto.MinStockLevel;
		product.DefaultUnitPrice = dto.DefaultUnitPrice;
		product.DefaultCurrency = normalizedCurrency;
		product.Description = dto.Description;

		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> DeleteProductAsync(int id)
	{
		var product = await _context.Products.FindAsync(id);
		if (product == null)
			return false;

		_context.Products.Remove(product);
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

	private static string NormalizeCurrency(string? currency)
	{
		var normalized = string.IsNullOrWhiteSpace(currency)
			? "TRY"
			: currency.Trim().ToUpperInvariant();

		return AllowedCurrencies.Contains(normalized) ? normalized : "TRY";
	}
}