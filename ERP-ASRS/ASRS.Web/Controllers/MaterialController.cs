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

    [Authorize(Roles = "Yönetici,Depo")]
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

    [Authorize(Roles = "Yönetici,Depo")]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateMaterialDto dto)
    {
        if (!ModelState.IsValid)
        {
            var material = await _materialService.GetMaterialByIdAsync(id);
            ViewBag.Error = "Tüm alanları doldurun.";
            return View(material);
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