using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}