using ASRS.Core.DTOs;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASRS.Web.Controllers;

[Authorize(Roles = "Yönetici,Satın Alma")]
public class PurchaseOrderController : Controller
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ISupplierService _supplierService;

    public PurchaseOrderController(IPurchaseOrderService purchaseOrderService, ISupplierService supplierService)
    {
        _purchaseOrderService = purchaseOrderService;
        _supplierService = supplierService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateFromRequest(int purchaseRequestId, string? notes)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "Kullanici bilgisi alinamadi.";
            return RedirectToAction("Details", "PurchaseRequest", new { id = purchaseRequestId });
        }

        var created = await _purchaseOrderService.CreateFromPurchaseRequestAsync(
            new CreatePurchaseOrderDto
            {
                PurchaseRequestId = purchaseRequestId,
                Notes = notes
            },
            userId);

        if (!created)
        {
            TempData["Error"] = "PO olusturulamadi. Talep durumu, eksik kalemler veya mevcut siparis kaydi kontrol edilmeli.";
            return RedirectToAction("Details", "PurchaseRequest", new { id = purchaseRequestId });
        }

        TempData["Success"] = "Satin alma siparisi basariyla olusturuldu.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Index(PurchaseOrderStatus? status)
    {
        var orders = await _purchaseOrderService.GetAllAsync(status);
        ViewBag.Status = status;
        ViewBag.Statuses = Enum.GetValues<PurchaseOrderStatus>();
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _purchaseOrderService.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        ViewBag.Suppliers = await _supplierService.GetActiveAsync();

        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, PurchaseOrderStatus newStatus)
    {
        var updated = await _purchaseOrderService.UpdateStatusAsync(id, newStatus);
        if (!updated)
        {
            TempData["Error"] = "PO durum gecisi gecersiz.";
            return RedirectToAction("Details", new { id });
        }

        TempData["Success"] = "PO durumu guncellendi.";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveItem(ReceivePurchaseOrderItemDto dto)
    {
        var ok = await _purchaseOrderService.ReceiveItemAsync(dto);
        if (!ok)
        {
            TempData["Error"] = "Kalem teslimi basarisiz. Miktar veya durum kontrol edin.";
            return RedirectToAction("Details", new { id = dto.PurchaseOrderId });
        }

        TempData["Success"] = "Kalem teslimi kaydedildi.";
        return RedirectToAction("Details", new { id = dto.PurchaseOrderId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateItemPricing(UpdatePurchaseOrderItemPricingDto dto)
    {
        var ok = await _purchaseOrderService.UpdateItemPricingAsync(dto);
        if (!ok)
        {
            TempData["Error"] = "Kalem fiyati guncellenemedi. Durum veya girilen degerleri kontrol edin.";
            return RedirectToAction("Details", new { id = dto.PurchaseOrderId });
        }

        TempData["Success"] = "Kalem fiyati guncellendi.";
        return RedirectToAction("Details", new { id = dto.PurchaseOrderId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateHeader(UpdatePurchaseOrderHeaderDto dto)
    {
        var ok = await _purchaseOrderService.UpdateHeaderAsync(dto);
        if (!ok)
        {
            TempData["Error"] = "PO baslik bilgileri guncellenemedi. Tedarikci, durum veya tarih bilgilerini kontrol edin.";
            return RedirectToAction("Details", new { id = dto.PurchaseOrderId });
        }

        TempData["Success"] = "PO baslik bilgileri guncellendi.";
        return RedirectToAction("Details", new { id = dto.PurchaseOrderId });
    }

}