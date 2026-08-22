using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;

using Sohba.ViewModels.Page;

namespace Sohba.Controllers
{
    [Authorize]
    [EnableRateLimiting("Api")]

    public class PagesController : BaseController
    {
        private readonly IPageService _pageService;
        private readonly IPostService _postService;
        private readonly IFriendshipService _friendshipService;
        private readonly IFileStorageService _fileStorage;

        public PagesController(
            IPageService pageService,
            IPostService postService,
            IFriendshipService friendshipService,
            IFileStorageService fileStorage)
        {
            _pageService = pageService;
            _postService = postService;
            _friendshipService = friendshipService;
            _fileStorage = fileStorage;
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


            ViewBag.CurrentUserId = GetCurrentUserId();
            return View(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] DeletePageRequest request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.Id == Guid.Empty)
                return Json(new { success = false, error = "Invalid page ID." });
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Json(new { success = false, error = "A deletion reason is required." });

            var result = await _pageService.DeletePageAsync(userId, request.Id, request.Reason);

            if (result.IsSuccess)
                return Json(new { success = true, message = "Page deleted successfully" });

            return Json(new { success = false, error = result.Error });
        }
        public class DeletePageRequest { public Guid Id { get; set; } public string Reason { get; set; } }

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

            // Delegate all file I/O to IFileStorageService.
            // Extension and size validation are enforced centrally in LocalFileStorageService.
            string imageUrl = null;
            var uploadResult = await _fileStorage.SaveFileAsync(model.ImageFile, "pages");
            if (!uploadResult.IsSuccess)
            {
                ModelState.AddModelError("ImageFile", uploadResult.Error);
                return View(model);
            }
            imageUrl = uploadResult.Value;

            var dto = new PageCreateDto
            {
                Name = model.Name,
                Description = model.Description,
                ImageUrl = imageUrl,
                AdminId = userId
            };

            var result = await _pageService.CreatePageAsync(userId, dto);

            if (result.IsSuccess)
                return RedirectToAction("Details", new { id = result.Value.Id });

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFollow([FromBody] ToggleFollowRequest request)
        {
            var userId = GetCurrentUserId();

            if (request == null || request.PageId == Guid.Empty)
                return Json(new { success = false, error = "Invalid page ID." });
            
            var result = await _pageService.ToggleFollowPageAsync(userId, request.PageId);

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
            var followersResult = await _pageService.GetFollowersAsync(pageId, page, pageSize);

            if (!followersResult.IsSuccess)
                return Json(new { success = false, error = followersResult.Error });

            return Json(new
            {
                success = true,
                followers = followersResult.Value,
                page = page,
                pageSize = pageSize,
                hasMore = followersResult.Value.Count() == pageSize
            });
        }

        [HttpGet]
        public async Task<IActionResult> CheckFollowStatus(Guid pageId)
        {
            var userId = GetCurrentUserId();

            var result = await _pageService.IsFollowingAsync(userId, pageId);

            var isAdmin = await _pageService.IsPageAdminAsync(userId, pageId);
            return Json(new { isFollowing = result.Value, isAdmin = isAdmin.Value });

        }


        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            var pageResult = await _pageService.GetPageByIdAsync(id);

            if (pageResult.IsFailure)
                return NotFound();

            if (pageResult.Value.AdminId != userId)
                return Forbid();

            var viewModel = new PageEditViewModel
            {
                Id = pageResult.Value.Id,
                Name = pageResult.Value.Name,
                Description = pageResult.Value.Description,
                ImageUrl = pageResult.Value.ImageUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PageEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            // Delegate file I/O to IFileStorageService (validation included).
            // If no new file is provided, keep the existing image URL.
            string imageUrl = model.ImageUrl;
            var uploadResult = await _fileStorage.SaveFileAsync(model.ImageFile, "pages");
            if (!uploadResult.IsSuccess)
            {
                ModelState.AddModelError("ImageFile", uploadResult.Error);
                return View(model);
            }
            if (uploadResult.Value != null) imageUrl = uploadResult.Value;

            string backgroundImageUrl = model.BackgroundImageUrl;
            var backgroundUploadResult = await _fileStorage.SaveFileAsync(model.BackgroundImageFile, "pages");
            if (!backgroundUploadResult.IsSuccess)
            {
                ModelState.AddModelError("BackgroundImageFile", backgroundUploadResult.Error);
                return View(model);
            }
            if (backgroundUploadResult.Value != null) backgroundImageUrl = backgroundUploadResult.Value;


            var updateDto = new PageUpdateDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                ImageUrl = imageUrl,
                BackgroundImageUrl = backgroundImageUrl
            };

            var result = await _pageService.UpdatePageAsync(updateDto, userId);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Page updated successfully";
                return RedirectToAction("Details", new { id = model.Id });
            }

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        

        [HttpGet]
        public async Task<IActionResult> GetPageStats(Guid pageId)
        {
            var postsResult = await _postService.GetPagePostsAsync(pageId, Guid.Empty);
            var followersCount = await _pageService.GetFollowersCountAsync(pageId);

            var postsCount = postsResult.IsSuccess ? postsResult.Value?.Count() ?? 0 : 0;

            return Json(new
            {
                success = true,
                postsCount = postsCount,
                followersCount = followersCount.Value
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KickMember([FromBody] PageMemberActionRequest request)
        {
            var adminId = GetCurrentUserId();
            if (request == null || request.PageId == Guid.Empty || request.TargetUserId == Guid.Empty)
                return Json(new { success = false, error = "Invalid request." });
            var result = await _pageService.KickPageMemberAsync(request.PageId, request.TargetUserId, adminId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteMember([FromBody] PageMemberActionRequest request)
        {
            var adminId = GetCurrentUserId();
            if (request == null || request.PageId == Guid.Empty || request.TargetUserId == Guid.Empty)
                return Json(new { success = false, error = "Invalid request." });
            var result = await _pageService.PromotePageMemberAsync(request.PageId, request.TargetUserId, adminId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave([FromBody] LeavePageRequest request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.PageId == Guid.Empty)
                return Json(new { success = false, error = "Invalid page ID." });

            var result = await _pageService.LeavePageAsync(request.PageId, userId);
            return Json(new { success = result.IsSuccess, error = result.Error, outcome = result.IsSuccess ? result.Value : null });
        }

        public class PageMemberActionRequest { public Guid PageId { get; set; } public Guid TargetUserId { get; set; } }
        public class LeavePageRequest { public Guid PageId { get; set; } }



        public class ToggleFollowRequest
        {
            public Guid PageId { get; set; }
        }
        
    }

}
