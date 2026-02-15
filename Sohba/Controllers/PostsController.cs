using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.ViewModels.Post;

namespace Sohba.Controllers
{
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        private readonly IInteractionService _interactionService;

        public PostsController(IPostService postService, IInteractionService interactionService)
        {
            _postService = postService;
            _interactionService = interactionService;
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

            var dto = new PostCreateDto
            {
                Title = model.Title,
                Content = model.Content,
                ImageUrl = model.ImageUrl,
                IsPrivate = model.IsPrivate
            };

            var result = await _postService.CreatePostAsync(dto, userId);

            if (result.IsSuccess)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", result.Error);
            return View(model);
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
        public async Task<IActionResult> React(Guid postId, string reactionType)
        {
            var userId = GetCurrentUserId();
            if (!Enum.TryParse<Domain.Enums.ReactionType>(reactionType, out var type))
                return BadRequest();

            var result = await _interactionService.ToggleReactionAsync(userId, postId, type);
            return Json(new { success = result.IsSuccess });
        }

        [HttpPost]
        public async Task<IActionResult> Comment(Guid postId, string content)
        {
            var userId = GetCurrentUserId();
            var result = await _interactionService.AddCommentAsync(userId, postId, content);
            return Json(new { success = result.IsSuccess });
        }

        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = GetCurrentUserId();
            // TODO: Get favorite posts from service
            // var result = await _postService.GetFavoritesAsync(userId);
            return View(new List<PostResponseDto>()); // مؤقتاً فارغ
        }

        [HttpGet]
        public async Task<IActionResult> SavedPosts()
        {
            var userId = GetCurrentUserId();
            // TODO: Get saved posts from service
            // var result = await _interactionService.GetSavedPostsAsync(userId);
            return View(new List<SavedPostDto>()); // مؤقتاً فارغ
        }

        private Guid GetCurrentUserId()
        {
            // Temporary until Identity is implemented
            //var userIdStr = HttpContext.Session.GetString("UserId");
            //return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);
            return new Guid("36FF9501-0409-F111-9291-902B34AC4276");

        }
    }
}

