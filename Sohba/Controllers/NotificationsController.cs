using Microsoft.AspNetCore.Mvc;
using Sohba.Application.Interfaces;

namespace Sohba.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly ISocialService _socialService;

        public NotificationsController(ISocialService socialService)
        {
            _socialService = socialService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var result = await _socialService.GetMyNotificationsAsync(userId);
            return View(result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var result = await _socialService.GetMyNotificationsAsync(userId);
            var count = result.Value?.Count() ?? 0;
            return Json(new { count });
        }

        private Guid GetCurrentUserId()
        {
            //var userIdStr = HttpContext.Session.GetString("UserId");
            //return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);
            return new Guid("36FF9501-0409-F111-9291-902B34AC4276");

        }
    }

}
