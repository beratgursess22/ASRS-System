using ASRS.Core.DTOs;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ASRS.Web.Controllers;

[Authorize]
public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IBomService _bomService;
    private readonly IMaterialService _materialService;

    public ProductController(IProductService productService, IBomService bomService, IMaterialService materialService)
    {
        _productService = productService;
        _bomService = bomService;
        _materialService = materialService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var products = await _productService.GetAllProductsAsync(search);
        ViewBag.Search = search;
        return View(products);
    }

    [Authorize(Roles = "Yönetici,Depo")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            var products = await _productService.GetAllProductsAsync(null);
            ViewBag.Error = "Tüm alanları doldurun.";
            return View("Index", products);
        }

        await _productService.CreateProductAsync(dto);
        var allProducts = await _productService.GetAllProductsAsync(null);
        var created = allProducts.OrderByDescending(p => p.Id).First();
        return RedirectToAction("Bom", new { id = created.Id });
    }

    [Authorize(Roles = "Yönetici,Depo")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    [Authorize(Roles = "Yönetici,Depo")]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            var product = await _productService.GetProductByIdAsync(id);
            ViewBag.Error = "Tüm alanları doldurun.";
            return View(product);
        }

        await _productService.UpdateProductAsync(id, dto);
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Yönetici,Depo")]
    public async Task<IActionResult> Bom(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();

        var bomItems = await _bomService.GetBomByProductIdAsync(id);
        var allProducts = await _productService.GetAllProductsAsync(null);
        var allMaterials = await _materialService.GetAllMaterialsAsync(null);

        ViewBag.Product = product;
        ViewBag.BomItems = bomItems;
        ViewBag.AllProducts = allProducts.Where(p => p.Id != id).ToList();
        ViewBag.AllMaterials = allMaterials.ToList();
        ViewBag.Error = TempData["BomError"] as string;
        return View();
    }

    [Authorize(Roles = "Yönetici,Depo")]
    [HttpPost]
    public async Task<IActionResult> AddBomItem(int productId, BomItemDto dto)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("Bom", new { id = productId });

        var isAdded = await _bomService.AddBomItemAsync(productId, dto);
        if (!isAdded)
            TempData["BomError"] = "Bileşen eklenemedi. Aynı bileşen zaten ekli olabilir veya seçim/miktar geçersizdir.";

        return RedirectToAction("Bom", new { id = productId });
    }

    [Authorize(Roles = "Yönetici,Depo")]
    [HttpPost]
    public async Task<IActionResult> UpdateBomItem(int id, int productId, int requiredQuantity, string? notes)
    {
        var updated = await _bomService.UpdateBomItemAsync(id, requiredQuantity, notes);
        if (!updated)
            TempData["BomError"] = "BOM satırı güncellenemedi. Miktar geçersiz veya satır bulunamadı.";

        return RedirectToAction("Bom", new { id = productId });
    }

    [Authorize(Roles = "Yönetici,Depo")]
    [HttpPost]
    public async Task<IActionResult> DeleteBomItem(int id, int productId)
    {
        await _bomService.DeleteBomItemAsync(id);
        return RedirectToAction("Bom", new { id = productId });
    }

    [Authorize(Roles = "Yönetici,Depo")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteProductAsync(id);
        return RedirectToAction("Index");
    }
}