using ASRS.Core.DTOs;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASRS.Web.Controllers;

[Authorize]
public class WorkOrderController : Controller
{
    private readonly IWorkOrderService _workOrderService;
    private readonly IProductService _productService;

    public WorkOrderController(IWorkOrderService workOrderService, IProductService productService)
    {
        _workOrderService = workOrderService;
        _productService = productService;
    }

    public async Task<IActionResult> Index(string? search, WorkOrderStatus? status)
    {
        var orders = await _workOrderService.GetAllAsync(search, status);
        var products = await _productService.GetAllProductsAsync(null);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Products = products;
        ViewBag.Statuses = Enum.GetValues<WorkOrderStatus>();
        return View(orders);
    }

    [Authorize(Roles = "Yönetici,Üretim")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            var orders = await _workOrderService.GetAllAsync(null, null);
            var products = await _productService.GetAllProductsAsync(null);
            ViewBag.Products = products;
            ViewBag.Statuses = Enum.GetValues<WorkOrderStatus>();
            ViewBag.Error = "Tüm alanları doldurun.";
            return View("Index", orders);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _workOrderService.CreateAsync(dto, userId);
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Yönetici,Üretim,Depo")]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, WorkOrderStatus status)
    {
        await _workOrderService.UpdateStatusAsync(id, status);
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Yönetici")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _workOrderService.DeleteAsync(id);
        return RedirectToAction("Index");
    }
}