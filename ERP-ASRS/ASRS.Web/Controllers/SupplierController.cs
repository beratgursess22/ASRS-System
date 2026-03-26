using ASRS.Core.DTOs;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[Authorize(Roles = "Yönetici,Satın Alma")]
public class SupplierController : Controller
{
    private readonly ISupplierService _supplierService;
    private readonly IProductService _productService;
    private readonly IMaterialService _materialService;

    public SupplierController(ISupplierService supplierService, IProductService productService, IMaterialService materialService)
    {
        _supplierService = supplierService;
        _productService = productService;
        _materialService = materialService;
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

        await LoadEditDataAsync(id);

        return View(supplier);
    }

    [HttpGet]
    public async Task<IActionResult> ItemPrices(int id)
    {
        var rows = await _supplierService.GetItemPricesAsync(id);
        return Json(rows);
    }

    [HttpPost]
    public async Task<IActionResult> UpsertItemPrice(UpsertSupplierItemPriceDto dto)
    {
        var ok = await _supplierService.UpsertItemPriceAsync(dto);
        if (!ok)
        {
            TempData["Error"] = "Tedarikci urun/malzeme fiyati kaydedilemedi. Alanlari kontrol edin.";
            return RedirectToAction("Edit", new { id = dto.SupplierId });
        }

        TempData["Success"] = "Tedarikci urun/malzeme fiyati kaydedildi.";
        return RedirectToAction("Edit", new { id = dto.SupplierId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteItemPrice(int supplierId, int? productId, int? materialId)
    {
        var ok = await _supplierService.DeleteItemPriceAsync(supplierId, productId, materialId);
        if (!ok)
        {
            TempData["Error"] = "Silinecek fiyat kaydi bulunamadi.";
            return RedirectToAction("Edit", new { id = supplierId });
        }

        TempData["Success"] = "Tedarikci urun/malzeme fiyati silindi.";
        return RedirectToAction("Edit", new { id = supplierId });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateSupplierDto dto)
    {
        if (!ModelState.IsValid)
        {
            var existing = await _supplierService.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            await LoadEditDataAsync(id);
            ViewBag.Error = "Tum alanlari dogru doldurun.";
            return View(existing);
        }

        var updated = await _supplierService.UpdateAsync(id, dto);
        if (!updated)
        {
            var existing = await _supplierService.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            await LoadEditDataAsync(id);
            ViewBag.Error = "Guncelleme basarisiz. Kod benzersiz olmali ve kayit mevcut olmali.";
            return View(existing);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _supplierService.DeleteAsync(id);
        if (!deleted)
        {
            TempData["Error"] = "Tedarikci kaydi bulunamadi.";
            return RedirectToAction("Index");
        }

        TempData["Success"] = "Tedarikci pasife alindi.";
        return RedirectToAction("Index");
    }

    private async Task LoadEditDataAsync(int supplierId)
    {
        ViewBag.Products = (await _productService.GetAllProductsAsync(null)).Where(x => x.IsActive).ToList();
        ViewBag.Materials = (await _materialService.GetAllMaterialsAsync(null)).Where(x => x.IsActive).ToList();
        ViewBag.ItemPrices = (await _supplierService.GetItemPricesAsync(supplierId)).ToList();
    }
}
