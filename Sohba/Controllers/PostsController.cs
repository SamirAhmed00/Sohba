using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.Domain.Common;
using Sohba.Domain.Enums;
using Sohba.ViewModels.Post;

namespace Sohba.Controllers
{

    [Authorize]
    public class PostsController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IReportingService _reportingService;
        private readonly IInteractionService _interactionService;
        private readonly IHashtagService _hashtagService;

        public PostsController(IPostService postService, IInteractionService interactionService, IReportingService reportingService, IHashtagService hashtagService)
        {
            _postService = postService;
            _interactionService = interactionService;
            _reportingService = reportingService;
            _hashtagService = hashtagService;
        }

        [HttpGet]
        public IActionResult Create() => View();



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return RedirectToAction("Login", "Auth");

            string imageUrl = model.ImageUrl; 

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(model.ImageFile.FileName)}{Path.GetExtension(model.ImageFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                // TODO : Change To DB
                imageUrl = $"/uploads/posts/{uniqueFileName}";
            }

            var dto = new PostCreateDto
            {
                Title = model.Title,
                Content = model.Content,
                ImageUrl = imageUrl,
                IsPrivate = model.IsPrivate
            };

            var result = await _postService.CreatePostAsync(dto, userId);

            if (result.IsSuccess)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPostDetails(Guid postId)
        {
            var postResult = await _postService.GetPostByIdAsync(postId);
            if (postResult.IsFailure)
                return NotFound(new { error = postResult.Error });

            var comments = await _interactionService.GetCommentsByPostIdAsync(postId);
            return Json(new { post = postResult.Value, comments });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _postService.GetPostByIdAsync(id);

            if (result.IsFailure || result.Value == null)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _postService.DeletePostAsync(id, userId);

            if (result.IsSuccess)
                return Json(new { success = true });

            return Json(new { success = false, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> React([FromBody] ReactionRequestDto request)
        {
            if (request == null || request.PostId == Guid.Empty || string.IsNullOrWhiteSpace(request.ReactionType))
                return BadRequest(new { success = false, error = "Invalid request data." });

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { success = false, error = "User not authenticated." });

            if (!Enum.TryParse<Domain.Enums.ReactionType>(request.ReactionType, true, out var type))
                return BadRequest(new { success = false, error = "Invalid reaction type." });

            var existingReaction = await _interactionService.GetUserReactionAsync(userId, request.PostId);

            if (existingReaction != null)
            {
                var removeResult = await _interactionService.RemoveReactionAsync(userId, request.PostId);

                if (!removeResult.IsSuccess)
                    return Json(new { success = false, error = removeResult.Error });

                var newCount = await _interactionService.GetReactionCountAsync(request.PostId);

                return Json(new
                {
                    success = true,
                    action = "removed",
                    newCount
                });
            }
            else
            {
                var addResult = await _interactionService.AddReactionAsync(userId, request.PostId, type);

                if (!addResult.IsSuccess)
                    return Json(new { success = false, error = addResult.Error });

                var newCount = await _interactionService.GetReactionCountAsync(request.PostId);

                return Json(new
                {
                    success = true,
                    action = "added",
                    newCount,
                    reactionType = request.ReactionType
                });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Comment([FromBody] CommentRequestDto request)
        {
            if (request == null || request.PostId == Guid.Empty || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { success = false, error = "Invalid data." });

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { success = false, error = "Unauthorized." });

            var result = await _interactionService.AddCommentAsync(userId, request.PostId, request.Content);

            if (!result.IsSuccess)
                return Json(new { success = false, error = result.Error });

            
            var comments = await _interactionService.GetCommentsByPostIdAsync(request.PostId);
            var latest = comments.First();

            return Json(new
            {
                success = true,
                comment = latest
            });
        }


        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.GetFavoritePostsAsync(userId);
            return View(result.Value ?? new List<PostResponseDto>());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSavePost([FromBody] ToggleSaveRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { success = false, error = "User not authenticated." });

            var tag = request.IsFavorite ? SavedTag.Favorite : SavedTag.General;

            var existingSave = (await _interactionService.GetSavedPostsAsync(userId)).Value?
                .FirstOrDefault(sp => sp.Id == request.PostId); // here 

            if (existingSave != null)
            {
                var removeResult = await _interactionService.RemoveSavedPostAsync(userId, request.PostId);
                if (removeResult.IsSuccess)
                    return Json(new { success = true, saved = false, message = "Post removed from saved." });
                else
                    return Json(new { success = false, error = removeResult.Error });
            }
            else
            {
                var saveResult = await _interactionService.SavePostAsync(userId, request.PostId, tag);
                if (saveResult.IsSuccess)
                    return Json(new { success = true, saved = true, message = "Post saved.", data = saveResult.Value });
                else
                    return Json(new { success = false, error = saveResult.Error });
            }
        }

        [HttpPost]
        
        public async Task<IActionResult> ReportPost([FromBody] PostReportRequestDto request)
        {

            System.Diagnostics.Debug.WriteLine($"PostId: {request?.PostId}, Reason: {request?.Reason}, UserId: {request?.UserId}");
            if (request == null || request.PostId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { success = false, error = "Invalid request data." });

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new { success = false, error = "User not authenticated." });

            var result = await _reportingService.ReportPostWithDetailsAsync(request, userId);

            if (result.IsSuccess)
            {
                return Json(new
                {
                    success = true,
                    message = "Post reported successfully.",
                    report = result.Value,
                    postId = request.PostId
                });
            }

            return Json(new { success = false, error = result.Error });
        }


        [HttpPost]
        public async Task<IActionResult> ChangeSavedPostTag([FromBody] ChangeTagRequest request)
        {
            var userId = GetCurrentUserId();

            if (!Enum.TryParse<SavedTag>(request.Tag, true, out var tag))
                return Json(new { success = false, error = "Invalid tag" });

            var result = await _interactionService.SavePostAsync(userId, request.PostId, tag);

            if (result.IsSuccess)
                return Json(new { success = true });

            return Json(new { success = false, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveSavedPost([FromBody] RemoveSavedRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.RemoveSavedPostAsync(userId, request.PostId);

            if (result.IsSuccess)
                return Json(new { success = true });

            return Json(new { success = false, error = result.Error });
        }

        [HttpGet]
        public async Task<IActionResult> SavedPosts(string tag = "all")
        {
            var userId = GetCurrentUserId();
            Result<IEnumerable<PostResponseDto>> result;

            if (tag == "all")
                result = await _interactionService.GetSavedPostsAsync(userId);
            else if (Enum.TryParse<SavedTag>(tag, true, out var savedTag))
                result = await _interactionService.GetSavedPostsByTagAsync(userId, savedTag);
            else
                result = await _interactionService.GetSavedPostsAsync(userId);

            ViewBag.CurrentTag = tag;
            return View(result.Value ?? new List<PostResponseDto>());
        }

        [HttpGet]
        public async Task<IActionResult> Hashtag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return RedirectToAction("Index", "Home");

            var userId = GetCurrentUserId();
            var result = await _hashtagService.GetPostsByHashtagAsync(tag, userId);

            ViewBag.Hashtag = tag;
            return View(result.Value ?? new List<PostResponseDto>());
        }

        [HttpGet]
        public async Task<IActionResult> SearchByHashtag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return Json(new { success = false, error = "Tag is required" });

            var userId = GetCurrentUserId();
            var result = await _hashtagService.GetPostsByHashtagAsync(tag, userId);

            return Json(new { success = true, posts = result.Value });
        }
        public class ToggleSaveRequest
        {
            public Guid PostId { get; set; }
            public bool IsFavorite { get; set; } // true = Favorite, false = General
        }

        public class ChangeTagRequest
        {
            public Guid PostId { get; set; }
            public string Tag { get; set; }
        }

        public class RemoveSavedRequest
        {
            public Guid PostId { get; set; }
        }
    }
}

