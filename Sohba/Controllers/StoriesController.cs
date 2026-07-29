using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Application.Interfaces;

namespace Sohba.Controllers
{
    [Authorize]
    [EnableRateLimiting("Api")]

    public class StoriesController : BaseController
    {
        private readonly IStoryService _storyService;
        private readonly IFileStorageService _fileStorage;

        public StoriesController(IStoryService storyService, IFileStorageService fileStorage)
        {
            _storyService = storyService;
            _fileStorage = fileStorage;
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

            // Resolve the media URL here in the controller (Infrastructure boundary).
            if (model.MediaFile != null && model.MediaFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveFileAsync(model.MediaFile, "stories");
                if (!uploadResult.IsSuccess)
                    return Json(BaseResponseDto<StoryResponseDto>.FailureResponse(uploadResult.Error));
                    
                model.MediaUrl = uploadResult.Value;
            }

            var result = await _storyService.CreateStoryAsync(model, userId);

            if (result.IsSuccess)
                return Json(BaseResponseDto<StoryResponseDto>.SuccessResponse(result.Value));

            return Json(BaseResponseDto<StoryResponseDto>.FailureResponse(result.Error));
        }

        [HttpGet]
        public async Task<IActionResult> GetStory(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.GetStoryByIdAsync(id, userId);

            if (result.IsSuccess)
                return Json(BaseResponseDto<StoryResponseDto>.SuccessResponse(result.Value));

            return Json(BaseResponseDto<StoryResponseDto>.FailureResponse(result.Error));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsViewed(Guid storyId)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.MarkStoryAsViewedAsync(storyId, userId);

            return Json(new BaseResponseDto { Success = result.IsSuccess });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.DeleteStoryAsync(id, userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStories(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _storyService.GetStoriesForFeedAsync(currentUserId);

            if (result.IsSuccess)
            {
                var userStories = result.Value.Where(s => s.UserId == userId).ToList();
                return Json(BaseResponseDto<IEnumerable<StoryResponseDto>>.SuccessResponse(userStories));
            }

            return Json(BaseResponseDto<IEnumerable<StoryResponseDto>>.SuccessResponse(new List<StoryResponseDto>()));
        }

    }
}