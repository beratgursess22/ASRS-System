using ASRS.Core.DTOs;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[Authorize(Roles = "Yönetici,Satın Alma")]
public class SupplierController : Controller
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var suppliers = await _supplierService.GetAllAsync(search);
        ViewBag.Search = search;
        return View(suppliers);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierDto dto)
    {
        if (!ModelState.IsValid)
        {
            var suppliers = await _supplierService.GetAllAsync(null);
            ViewBag.Error = "Tum alanlari dogru doldurun.";
            return View("Index", suppliers);
        }

        var created = await _supplierService.CreateAsync(dto);
        if (!created)
        {
            var suppliers = await _supplierService.GetAllAsync(null);
            ViewBag.Error = "Kayit basarisiz. Kod benzersiz olmali ve zorunlu alanlar dolu olmali.";
            return View("Index", suppliers);
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);
        if (supplier == null)
            return NotFound();

        return View(supplier);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateSupplierDto dto)
    {
        if (!ModelState.IsValid)
        {
            var existing = await _supplierService.GetByIdAsync(id);
            ViewBag.Error = "Tum alanlari dogru doldurun.";
            return View(existing);
        }

        var updated = await _supplierService.UpdateAsync(id, dto);
        if (!updated)
        {
            var existing = await _supplierService.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            ViewBag.Error = "Guncelleme basarisiz. Kod benzersiz olmali ve kayit mevcut olmali.";
            return View(existing);
        }

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Yönetici")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _supplierService.DeleteAsync(id);
        return RedirectToAction("Index");
    }
}
