using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.ViewModels.Landing;
using System.Security.Claims;

namespace Sohba.Controllers
{
    [EnableRateLimiting("Default")]
    public class LandingController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var model = new LandingIndexViewModel
            {
                IsAuthenticated = User.Identity?.IsAuthenticated == true,
                UserName = User.FindFirst(ClaimTypes.Name)?.Value
                           ?? User.Identity?.Name
                           ?? "User"
            };

            return View(model);
        }
    }
}
