using ASRS.Core.DTOs;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[Authorize]
public class ProductController : Controller
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
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
        return RedirectToAction("Index");
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
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteProductAsync(id);
        return RedirectToAction("Index");
    }
}