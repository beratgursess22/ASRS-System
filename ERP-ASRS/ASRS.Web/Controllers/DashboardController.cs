using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[Authorize]
public class DashboardController : Controller // kullanıcı giriş yaptıktan sonra yönlendirilecekleri ana sayfayı yöneten controller
{
    public IActionResult Index()
    {
        return View();
    }
}