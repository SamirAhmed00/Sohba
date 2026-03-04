using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.ViewModels.Page;

namespace Sohba.Controllers
{
    public class PagesController : BaseController
    {
        private readonly IPageService _pageService;
        private readonly IPostService _postService; 
        private readonly IFriendshipService _friendshipService;
        public PagesController(IPageService pageService, IPostService postService, IFriendshipService friendshipService)
        {
            _pageService = pageService;
            _postService = postService;
            _friendshipService = friendshipService;
        }

        [HttpGet]
        public async Task<IActionResult> Discover()
        {
            var userId = GetCurrentUserId();

            var result = await _pageService.GetAllPagesAsync();

            if (result.IsSuccess)
            {
                var followedPages = await _pageService.GetUserFollowedPagesAsync(userId);
                var followedIds = followedPages.Value.Select(p => p.Id);

                var pagesToFollow = result.Value
                    .Where(p => !followedIds.Contains(p.Id))
                    .Take(5)
                    .ToList();

                return Json(pagesToFollow);
            }
            return Json(new List<PageResponseDto>());
        }


        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _pageService.GetPageByIdAsync(id);

            if (result.IsFailure)
                return NotFound();

            return View(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId(); 
            var result = await _pageService.DeletePageAsync(userId, id);

            if (result.IsSuccess)
                return Json(new { success = true, message = "Page deleted successfully" });

            return Json(new { success = false, error = result.Error });
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            var result = await _pageService.GetUserFollowedPagesAsync(userId);
            if (result.IsSuccess)
            {
                foreach (var page in result.Value)
                {
                    page.IsFollowing = true;
                }
            }
            ViewBag.CurrentUserId = userId;
            return View(result.Value);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PageCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            string imageUrl = null;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pages");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(model.ImageFile.FileName)}{Path.GetExtension(model.ImageFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                imageUrl = $"/uploads/pages/{uniqueFileName}";
            }

            var dto = new PageCreateDto
            {
                Name = model.Name,
                Description = model.Description,
                ImageUrl = imageUrl,
                AdminId = userId
            };

            var result = await _pageService.CreatePageAsync(userId, dto);

            if (result.IsSuccess)
                return RedirectToAction("Index");

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFollow(Guid pageId)
        {
            var userId = GetCurrentUserId();

            var result = await _pageService.ToggleFollowPageAsync(userId, pageId);

            return Json(new
            {
                success = result.IsSuccess,
                isFollowing = result.Value 
            });
        }


        [HttpGet]
        public async Task<IActionResult> GetPagesList()
        {
            var userId = GetCurrentUserId();
            var result = await _pageService.GetUserFollowedPagesAsync(userId);
            return Json(result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetPagePosts(Guid pageId)
        {
            var userId = GetCurrentUserId();
            var postsResult = await _postService.GetPagePostsAsync(pageId, userId);

            if (postsResult.IsSuccess && postsResult.Value != null && postsResult.Value.Any())
            {
                return PartialView("Partials/_PostCard", postsResult.Value);
            }

            return Content("<div class='text-center py-10 text-gray-500'>No posts yet</div>");
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowersPreview(Guid pageId, int count = 10)
        {
            var userId = GetCurrentUserId();

            var followersResult = await _pageService.GetFollowersAsync(pageId, 1, count);

            if (followersResult.IsSuccess)
            {
                return Json(followersResult.Value);
            }

            return Json(new List<PageFollowerDto>());
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFollowers(Guid pageId, int page = 1, int pageSize = 20)
        {
            var userId = GetCurrentUserId();

            var followersResult = await _pageService.GetFollowersAsync(pageId, page, pageSize);

            if (followersResult.IsSuccess)
            {
                return Json(new
                {
                    success = true,
                    followers = followersResult.Value,
                    page = page,
                    pageSize = pageSize,
                    hasMore = followersResult.Value.Count() == pageSize
                });
            }

            return Json(new { success = false, error = followersResult.Error });
        }

        [HttpGet]
        public async Task<IActionResult> CheckFollowStatus(Guid pageId)
        {
            var userId = GetCurrentUserId();

            var result = await _pageService.IsFollowingAsync(userId, pageId);

            return Json(new { isFollowing = result.Value });
        }

        public class ToggleFollowRequest
        {
            public Guid PageId { get; set; }
        }
        //private Guid GetCurrentUserId()
        //{
        //    //var userIdStr = HttpContext.Session.GetString("UserId");
        //    //return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);
        //    return new Guid("36FF9501-0409-F111-9291-902B34AC4276");

        //}
    }

}
