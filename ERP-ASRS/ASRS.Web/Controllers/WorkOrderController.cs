using ASRS.Core.DTOs;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASRS.Web.Controllers;

[Authorize(Roles = "Yönetici,Üretim")]
public class WorkOrderController : Controller
{
    private readonly IWorkOrderService _workOrderService;
    private readonly IProductService _productService;
    private readonly IBomService _bomService;

    public WorkOrderController(IWorkOrderService workOrderService, IProductService productService, IBomService bomService)
    {
        _workOrderService = workOrderService;
        _productService = productService;
        _bomService = bomService;
    }

    public async Task<IActionResult> Index(string? search, WorkOrderStatus? status)
    {
        var orders = await _workOrderService.GetAllAsync(search, status);
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Statuses = Enum.GetValues<WorkOrderStatus>();
        return View(orders);
    }

    [Authorize(Roles = "Yönetici,Üretim")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var products = await _productService.GetAllProductsAsync(null);
        ViewBag.Products = products;
        return View();
    }

    [Authorize(Roles = "Yönetici,Üretim")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            var products = await _productService.GetAllProductsAsync(null);
            ViewBag.Products = products;
            ViewBag.Error = "Tüm zorunlu alanları doldurun.";
            return View(dto);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _workOrderService.CreateAsync(dto, userId);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetBom(int productId)
    {
        var bom = await _bomService.GetBomByProductIdAsync(productId);
        return Json(bom);
    }

    [Authorize(Roles = "Yönetici,Üretim")]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, WorkOrderStatus status)
    {
        var result = await _workOrderService.UpdateStatusAsync(id, status);

        if (result != WorkOrderStatusUpdateResult.Success)
        {
            TempData["Error"] = result switch
            {
                WorkOrderStatusUpdateResult.WorkOrderNotFound =>
                    "İş emri bulunamadı.",
                WorkOrderStatusUpdateResult.InvalidTransition =>
                    "Geçersiz durum geçişi.",
                WorkOrderStatusUpdateResult.BomCycleDetected =>
                    "BOM döngüsü tespit edildi (A -> B -> A). Reçeteyi düzeltmeden işlem yapılamaz.",
                WorkOrderStatusUpdateResult.StockInsufficient =>
                    "Stok yetersiz: iş emri için gerekli ürün/malzeme miktarı karşılanamıyor.",
                WorkOrderStatusUpdateResult.RestoreFailed =>
                    "Stok iadesi yapılamadı. BOM yapısı veya bileşen kayıtları kontrol edilmeli.",
                _ =>
                    "Durum güncellenemedi."
            };
        }
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Yönetici,Üretim")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _workOrderService.DeleteAsync(id);
        return RedirectToAction("Index");
    }


    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var workOrder = await _workOrderService.GetByIdAsync(id);
        if (workOrder == null)
            return NotFound();

        var tree = await _bomService.GetNestedBomRequirementsAsync(workOrder.ProductId, workOrder.Quantity);
        ViewBag.BomTree = tree;
        return View(workOrder);
    }
}
