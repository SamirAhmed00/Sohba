using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Sohba.Controllers
{
    [EnableRateLimiting("Default")]
    public class LandingController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ??
                               User.Identity.Name ??
                               "User";

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