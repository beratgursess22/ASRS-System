using ASRS.Core.DTOs;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASRS.Web.Controllers;

[Authorize(Roles = "Yönetici,Satın Alma")]
public class PurchaseRequestController : Controller
{
    private readonly IPurchaseRequestService _purchaseRequestService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IWorkOrderService _workOrderService;

    public PurchaseRequestController(
        IPurchaseRequestService purchaseRequestService,
        IPurchaseOrderService purchaseOrderService,
        IWorkOrderService workOrderService)
    {
        _purchaseRequestService = purchaseRequestService;
        _purchaseOrderService = purchaseOrderService;
        _workOrderService = workOrderService;
    }

    [Authorize(Roles = "Yönetici,Satın Alma")]
    [HttpPost]
    public async Task<IActionResult> CreateFromWorkOrder(int workOrderId, string? notes)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "Kullanici bilgisi alinamadi.";
            return RedirectToAction("Details", "WorkOrder", new { id = workOrderId });
        }

        var created = await _purchaseRequestService.CreateFromWorkOrderAsync(workOrderId, userId, notes);
        if (!created)
        {
            var workOrder = await _workOrderService.GetByIdAsync(workOrderId);

            TempData["Error"] = workOrder?.Status switch
            {
                WorkOrderStatus.Completed =>
                    "Bu is emri tamamlandigi icin satin alma talebi olusturulamaz.",
                WorkOrderStatus.Cancelled =>
                    "Bu is emri iptal edildigi icin satin alma talebi olusturulamaz.",
                _ =>
                    "Talep olusturulamadi. Yetersiz kalem bulunmuyor olabilir veya bu is emri icin aktif talep zaten var."
            };
            return RedirectToAction("Details", "WorkOrder", new { id = workOrderId });
        }

        TempData["Success"] = "Satin alma talebi olusturuldu.";
        return RedirectToAction("Index");
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
        if (status == PurchaseRequestStatus.Received)
        {
            TempData["Error"] = "Talep durumu Teslim Alindi olarak manuel guncellenemez. Teslim PO uzerinden yapilmalidir.";
            return RedirectToAction("Details", new { id });
        }

        var existing = await _purchaseRequestService.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var previousStatus = existing.Status;
        var updated = await _purchaseRequestService.UpdateStatusAsync(id, status, notes);
        if (!updated)
        {
            TempData["Error"] = "Gecersiz durum gecisi. Talep asamalari sirasiyla ilerlemelidir.";
            return RedirectToAction("Details", new { id });
        }

        if (previousStatus == PurchaseRequestStatus.Pending && status == PurchaseRequestStatus.Approved)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var created = await _purchaseOrderService.CreateFromPurchaseRequestAsync(
                    new CreatePurchaseOrderDto
                    {
                        PurchaseRequestId = id,
                        Notes = "Talep onayi ile otomatik olusturuldu."
                    },
                    userId);

                if (created)
                {
                    TempData["Success"] = "Talep onaylandi ve satin alma siparisi otomatik olusturuldu.";
                    return RedirectToAction("Index", "PurchaseOrder");
                }

                TempData["Error"] = "Talep onaylandi ancak PO otomatik olusturulamadi. Talep detayindan manuel olusturabilirsiniz.";
                return RedirectToAction("Details", new { id });
            }

            TempData["Error"] = "Talep onaylandi ancak kullanici bilgisi alinamadigi icin PO otomatik olusturulamadi.";
            return RedirectToAction("Details", new { id });
        }

        TempData["Success"] = "Talep durumu güncellendi.";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateItem(int requestId, int itemId, int missingQuantity, string? notes)
    {
        var updated = await _purchaseRequestService.UpdateItemAsync(requestId, itemId, missingQuantity, notes);

        if (!updated)
        {
            TempData["Error"] = "Kalem guncellenemedi. Talep sadece beklemede iken revize edilebilir.";
            return RedirectToAction("Details", new { id = requestId });
        }

        TempData["Success"] = "Talep kalemi guncellendi.";
        return RedirectToAction("Details", new { id = requestId });
    }

}


