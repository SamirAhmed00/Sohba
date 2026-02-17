using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.StoryAggregate;
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
            var result = await _storyService.GetStoriesForFeedAsync(userId);
            return View(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] StoryCreateDto model)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.CreateStoryAsync(model, userId);

            if (result.IsSuccess)
                return Json(new { success = true, data = result.Value });

            return Json(new { success = false, error = result.Error });
        }

        [HttpGet]
        public async Task<IActionResult> GetStory(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.GetStoryByIdAsync(id, userId);

            if (result.IsSuccess)
                return Json(result.Value);

            return Json(new { success = false, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsViewed(Guid storyId)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.MarkStoryAsViewedAsync(storyId, userId);

            return Json(new { success = result.IsSuccess });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.DeleteStoryAsync(id, userId);
            return Json(new { success = result.IsSuccess });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStories(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _storyService.GetStoriesForFeedAsync(currentUserId);

            if (result.IsSuccess)
            {
                var userStories = result.Value.Where(s => s.UserId == userId).ToList();
                return Json(userStories);
            }

            return Json(new List<StoryResponseDto>());
        }

        private Guid GetCurrentUserId()
        {
            // Temporary until Identity is implemented
            return new Guid("36FF9501-0409-F111-9291-902B34AC4276");
        }
    }
}