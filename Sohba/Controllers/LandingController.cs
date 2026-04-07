using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Sohba.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            // التحقق إذا كان المستخدم مسجل دخوله
            if (User.Identity.IsAuthenticated)
            {
                // جلب اسم المستخدم من الـ Claims
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ??
                               User.Identity.Name ??
                               "User";

                // تخزين اسم المستخدم في ViewBag لإرساله للـ View
                ViewBag.IsAuthenticated = true;
                ViewBag.UserName = userName;
            }
            else
            {
                ViewBag.IsAuthenticated = false;
            }

            return View();
        }
    }
}