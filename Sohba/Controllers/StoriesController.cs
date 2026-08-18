using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.StoryAggregate;
using Sohba.Domain.Enums;

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
        public async Task<IActionResult> MarkAsViewed([FromBody] MarkAsViewedModel model)
        {
            var userId = GetCurrentUserId();
            if (model == null || model.storyId == Guid.Empty)
                    return Json(new BaseResponseDto { Success = false, Error = "Invalid story ID." });
            var result = await _storyService.MarkStoryAsViewedAsync(model.storyId, userId);

            return Json(new BaseResponseDto { Success = result.IsSuccess });
        }

        public class MarkAsViewedModel
        {
            public Guid storyId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] IdRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.Id == Guid.Empty)
                    return Json(new BaseResponseDto { Success = false, Error = "Invalid story ID." });
            var result = await _storyService.DeleteStoryAsync(request.Id, userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStories(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _storyService.GetUserStoriesAsync(userId, currentUserId);

            if (result.IsSuccess)
            {
                return Json(BaseResponseDto<IEnumerable<StoryResponseDto>>.SuccessResponse(result.Value));
            }

            return Json(BaseResponseDto<IEnumerable<StoryResponseDto>>.SuccessResponse(new List<StoryResponseDto>()));
        }


        [HttpPost]
        public async Task<IActionResult> React([FromBody] StoryReactionRequestDto request)
        {
            if (request == null || request.StoryId == Guid.Empty || string.IsNullOrWhiteSpace(request.ReactionType))
                return Json(new { success = false, error = "Invalid request." });

            var userId = GetCurrentUserId();
            if (!Enum.TryParse<ReactionType>(request.ReactionType, true, out var type))
                return Json(new { success = false, error = "Invalid reaction type." });

            var result = await _storyService.ToggleStoryReactionAsync(userId, request.StoryId, type);
            if (!result.IsSuccess)
                return Json(new { success = false, error = result.Error });

            return Json(new { success = true, action = result.Value.Added ? "added" : "removed", newCount = result.Value.NewCount });
        }

        // [ADDED]
        [HttpGet]
        public async Task<IActionResult> GetStoryViewers(Guid storyId)
        {
            var userId = GetCurrentUserId();
            var result = await _storyService.GetStoryViewersAsync(storyId, userId);

            if (!result.IsSuccess)
                return Json(BaseResponseDto<IEnumerable<StoryViewerDto>>.FailureResponse(result.Error));

            return Json(BaseResponseDto<IEnumerable<StoryViewerDto>>.SuccessResponse(result.Value));
        }

    }
}