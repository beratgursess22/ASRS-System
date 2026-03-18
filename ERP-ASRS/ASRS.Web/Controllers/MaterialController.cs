using ASRS.Core.DTOs;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[Authorize]
public class MaterialController : Controller
{
    private readonly IMaterialService _materialService;

    public MaterialController(IMaterialService materialService)
    {
        _materialService = materialService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var materials = await _materialService.GetAllMaterialsAsync(search);
        ViewBag.Search = search;
        return View(materials);
    }

    [Authorize(Roles = "Yönetici,Satın Alma")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateMaterialDto dto)
    {
        if (!ModelState.IsValid)
        {
            var materials = await _materialService.GetAllMaterialsAsync(null);
            ViewBag.Error = "Tüm alanları doldurun.";
            return View("Index", materials);
        }

        await _materialService.CreateMaterialAsync(dto);
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Yönetici,Depo")]
    public async Task<IActionResult> Edit(int id)
    {
        var material = await _materialService.GetMaterialByIdAsync(id);
        if (material == null)
            return NotFound();
        return View(material);
    }

    [Authorize(Roles = "Yönetici,Depo,Satın Alma")]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateMaterialDto dto)
    {
        if (!ModelState.IsValid)
        {
            var material = await _materialService.GetMaterialByIdAsync(id);
            ViewBag.Error = "Tüm alanları doldurun.";
            return View(material);
        }

        var existing = await _materialService.GetMaterialByIdAsync(id);
        if (existing == null)
            return NotFound();

        var stockChanged = existing.StockQuantity != dto.StockQuantity
        || existing.MinStockLevel != dto.MinStockLevel;

        var canChangeStock = User.IsInRole("Yönetici") || User.IsInRole("Satın Alma");

        if (stockChanged && !canChangeStock)
        {
            ModelState.AddModelError(string.Empty, "Stok ve minimum stok sadece Yönetici veya Satın Alma tarafından değiştirilebilir.");
            dto.StockQuantity = existing.StockQuantity;
            dto.MinStockLevel = existing.MinStockLevel;
            return View(existing);
        }

        await _materialService.UpdateMaterialAsync(id, dto);
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Yönetici,Depo")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _materialService.DeleteMaterialAsync(id);
        return RedirectToAction("Index");
    }
}