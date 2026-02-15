using Microsoft.AspNetCore.Mvc;
using Sohba.Application.Interfaces;

namespace Sohba.Controllers
{
    public class StoriesController : Controller
    {
        private readonly IStoryService _storyService;

        public StoriesController(IStoryService storyService)
        {
            _storyService = storyService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.GetActiveStoriesAsync(userId);
            return View(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string content, string? imageUrl)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.CreateStoryAsync(content, imageUrl, userId);

            if (result.IsSuccess)
                return Json(new { success = true, data = result.Value });

            return Json(new { success = false, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.DeleteStoryAsync(id, userId);
            return Json(new { success = result.IsSuccess });
        }

        private Guid GetCurrentUserId()
        {
            //var userIdStr = HttpContext.Session.GetString("UserId");
            //return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);
            return new Guid("36FF9501-0409-F111-9291-902B34AC4276");

        }
    }
}
