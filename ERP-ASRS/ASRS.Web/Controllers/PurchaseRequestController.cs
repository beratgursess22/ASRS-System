using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[Authorize(Roles = "Yönetici,Satın Alma")]
public class PurchaseRequestController : Controller
{
    private readonly IPurchaseRequestService _purchaseRequestService;

    public PurchaseRequestController(IPurchaseRequestService purchaseRequestService)
    {
        _purchaseRequestService = purchaseRequestService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(PurchaseRequestStatus? status)
    {
        var requests = await _purchaseRequestService.GetAllAsync(status);
        ViewBag.Status = status;
        ViewBag.Statuses = Enum.GetValues<PurchaseRequestStatus>();
        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _purchaseRequestService.GetByIdAsync(id);
        if (request == null)
            return NotFound();

        return View(request);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, PurchaseRequestStatus status, string? notes)
    {
        var updated = await _purchaseRequestService.UpdateStatusAsync(id, status, notes);

        if (!updated)
        {
            TempData["Error"] = "Gecersiz durum gecisi. Talep asamalari sirasiyla ilerlemelidir.";
            return RedirectToAction("Details", new { id });
        }

        TempData["Success"] = "Talep durumu güncellendi.";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateItemStock(int requestId, int itemId)
    {
        var updated = await _purchaseRequestService.UpdateItemStockAsync(requestId, itemId);

        if (!updated)
        {
            TempData["Error"] = "Stok guncellenemedi. Islem sadece Siparis Verildi asamasinda ve eksik kalemler icin yapilabilir.";
            return RedirectToAction("Details", new { id = requestId });
        }

        TempData["Success"] = "Kalem stogu basariyla guncellendi.";
        return RedirectToAction("Details", new { id = requestId });
    }
}