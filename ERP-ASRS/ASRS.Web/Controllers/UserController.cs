using ASRS.Core.DTOs;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[Authorize(Roles = "Yönetici")]
public class UserController : Controller // kullanıcı yönetimi işlemlerini yöneten controller, sadece "Yönetici" rolüne sahip kullanıcılar erişebilir
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // Kullanıcı listesi + ekleme formu aynı sayfada
    public async Task<IActionResult> Index(string? search)
    {
        var users = await _userService.GetAllUsersAsync();
        var roles = await _userService.GetRolesAsync();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            users = users.Where(u =>
                u.FullName.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search)
            );
        }

        ViewBag.Roles = roles;
        ViewBag.Search = search;
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userService.GetAllUsersAsync();
            var roles = await _userService.GetRolesAsync();
            ViewBag.Roles = roles;
            ViewBag.Error = "Lütfen tüm alanları doldurun.";
            return View("Index", users);
        }

        var result = await _userService.CreateUserAsync(dto);
        if (!result)
        {
            var users = await _userService.GetAllUsersAsync();
            var roles = await _userService.GetRolesAsync();
            ViewBag.Roles = roles;
            ViewBag.Error = "Kullanıcı oluşturulamadı. E-posta zaten kullanımda olabilir.";
            return View("Index", users);
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        var roles = await _userService.GetRolesAsync();
        ViewBag.Roles = roles;
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, string fullName, string role, bool isActive)
    {
        var result = await _userService.UpdateUserAsync(id, fullName, role, isActive);
        if (!result)
        {
            ViewBag.Error = "Güncelleme başarısız.";
            return View();
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        await _userService.DeleteUserAsync(id);
        return RedirectToAction("Index");
    }
}