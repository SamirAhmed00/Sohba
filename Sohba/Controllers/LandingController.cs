using Microsoft.AspNetCore.Mvc;

namespace Sohba.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}