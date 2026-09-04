using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Enums;

using Sohba.ViewModels.Page;

namespace Sohba.Controllers
{
    [Authorize]
    [EnableRateLimiting("Default")]

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
            var result = await _pageService.GetPagesToDiscoverAsync(userId, 5);
            if (!result.IsSuccess)
                return Json(new List<PageResponseDto>());

            return Json(result.Value ?? Enumerable.Empty<PageResponseDto>());
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _pageService.GetPageByIdAsync(id);
            if (result.IsFailure)
                return NotFound();

            var currentUserId = GetCurrentUserId();
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.CurrentUserRole = await _pageService.GetUserRoleInPageAsync(currentUserId, id);

            // Pre-populate follow state to prevent UI flicker
            if (currentUserId != Guid.Empty)
            {
                var followStatus = await _pageService.IsFollowingAsync(currentUserId, id);
                result.Value.IsFollowing = followStatus.IsSuccess && followStatus.Value;

                var pendingStatus = await _pageService.HasPendingRequestAsync(id, currentUserId);
                result.Value.HasPendingRequest = pendingStatus.IsSuccess && pendingStatus.Value;
            }

            return View(result.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            var allPagesResult = await _pageService.GetAllPagesAsync();
            var followedResult = await _pageService.GetUserFollowedPagesAsync(userId);

            var followedIds = (followedResult.IsSuccess && followedResult.Value != null)
                ? followedResult.Value.Select(p => p.Id).ToHashSet()
                : new HashSet<Guid>();

            var pages = allPagesResult.IsSuccess && allPagesResult.Value != null
                ? allPagesResult.Value.ToList()
                : new List<PageResponseDto>();

            foreach (var page in pages)
            {
                page.IsFollowing = followedIds.Contains(page.Id);
            }

            ViewBag.CurrentUserId = userId;
            return View(pages);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PageCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();

            string? imageUrl = null;
            if (model.ImageFile != null)
            {
                var uploadResult = await _fileStorage.SaveFileAsync(model.ImageFile, "pages");
                if (!uploadResult.IsSuccess)
                {
                    ModelState.AddModelError("ImageFile", uploadResult.Error);
                    return View(model);
                }
                imageUrl = uploadResult.Value;
            }

            string? backgroundImageUrl = null;
            if (model.BackgroundImageFile != null)
            {
                var bgUploadResult = await _fileStorage.SaveFileAsync(model.BackgroundImageFile, "pages");
                if (!bgUploadResult.IsSuccess)
                {
                    ModelState.AddModelError("BackgroundImageFile", bgUploadResult.Error);
                    return View(model);
                }
                backgroundImageUrl = bgUploadResult.Value;
            }

            var dto = new PageCreateDto
            {
                Name = model.Name,
                Description = model.Description,
                ImageUrl = imageUrl,
                BackgroundImageUrl = backgroundImageUrl,
                Rules = model.Rules,
                IsPrivate = model.IsPrivate,
                AdminId = userId
            };

            var result = await _pageService.CreatePageAsync(userId, dto);

            if (result.IsSuccess)
                return RedirectToAction("Details", new { id = result.Value.Id });

            // Compensating cleanup on failed creation
            if (!string.IsNullOrEmpty(imageUrl))
                await _fileStorage.DeleteFileAsync(imageUrl);
            if (!string.IsNullOrEmpty(backgroundImageUrl))
                await _fileStorage.DeleteFileAsync(backgroundImageUrl);

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
            if (!result.IsSuccess)
                return Json(new List<PageResponseDto>());

            return Json(result.Value ?? Enumerable.Empty<PageResponseDto>());
        }

        [HttpGet]
        public async Task<IActionResult> GetPagePosts(Guid pageId)
        {
            var userId = GetCurrentUserId();

            if (pageId == Guid.Empty)
                return BadRequest();

            // Server-side privacy enforcement
            var pageResult = await _pageService.GetPageByIdAsync(pageId);
            if (pageResult.IsSuccess && pageResult.Value.IsPrivate)
            {
                var role = await _pageService.GetUserRoleInPageAsync(userId, pageId);
                if (role == null)
                    return Forbid();
            }

            var postsResult = await _postService.GetPagePostsAsync(pageId, userId);

            if (!postsResult.IsSuccess)
                return StatusCode(500, new
                {
                    success = false,
                    error = postsResult.Error
                });

            return PartialView(
                "Partials/_PostCard",
                postsResult.Value ?? Enumerable.Empty<PostResponseDto>());
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowersPreview(Guid pageId, int count = 10)
        {
            var userId = GetCurrentUserId();

            // Server-side privacy enforcement
            var pageResult = await _pageService.GetPageByIdAsync(pageId);
            if (pageResult.IsSuccess && pageResult.Value.IsPrivate)
            {
                var role = await _pageService.GetUserRoleInPageAsync(userId, pageId);
                if (role == null)
                    return Json(new List<PageFollowerDto>());
            }

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

            // Server-side privacy enforcement
            var pageResult = await _pageService.GetPageByIdAsync(pageId);
            if (pageResult.IsSuccess && pageResult.Value.IsPrivate)
            {
                var role = await _pageService.GetUserRoleInPageAsync(userId, pageId);
                if (role == null)
                    return Json(new { success = false, error = "This page is private." });
            }

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
            if (pageId == Guid.Empty)
                return Json(new { success = false, error = "Invalid page ID." });

            var userId = GetCurrentUserId();
            var role = await _pageService.GetUserRoleInPageAsync(userId, pageId);
            var isFollowing = role != null;
            var isAdmin = role.HasValue && role.Value >= PageRole.Admin;

            return Json(new
            {
                success = true,
                isFollowing = isFollowing,
                isAdmin = isAdmin,
                role = role?.ToString() ?? "None"
            });
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

            var actorRole = await _pageService.GetUserRoleInPageAsync(userId, id);
            if (actorRole == null || actorRole < PageRole.Admin)
                return Forbid();

            var viewModel = new PageEditViewModel
            {
                Id = pageResult.Value.Id,
                Name = pageResult.Value.Name,
                Description = pageResult.Value.Description,
                ImageUrl = pageResult.Value.ImageUrl,
                BackgroundImageUrl = pageResult.Value.BackgroundImageUrl,
                Rules = pageResult.Value.Rules,
                IsPrivate = pageResult.Value.IsPrivate
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

            var pageResult = await _pageService.GetPageByIdAsync(model.Id);
            if (pageResult.IsFailure)
                return NotFound();

            var actorRole = await _pageService.GetUserRoleInPageAsync(userId, model.Id);
            if (actorRole == null || actorRole < PageRole.Admin)
                return Forbid();

            string imageUrl = model.ImageUrl;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveFileAsync(model.ImageFile, "pages");
                if (!uploadResult.IsSuccess)
                {
                    ModelState.AddModelError("ImageFile", uploadResult.Error);
                    return View(model);
                }
                if (uploadResult.Value != null) imageUrl = uploadResult.Value;
            }

            string backgroundImageUrl = model.BackgroundImageUrl;
            if (model.BackgroundImageFile != null && model.BackgroundImageFile.Length > 0)
            {
                var backgroundUploadResult = await _fileStorage.SaveFileAsync(model.BackgroundImageFile, "pages");
                if (!backgroundUploadResult.IsSuccess)
                {
                    ModelState.AddModelError("BackgroundImageFile", backgroundUploadResult.Error);
                    return View(model);
                }
                if (backgroundUploadResult.Value != null) backgroundImageUrl = backgroundUploadResult.Value;
            }

            var updateDto = new PageUpdateDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                ImageUrl = imageUrl,
                BackgroundImageUrl = backgroundImageUrl,
                Rules = model.Rules,
                IsPrivate = model.IsPrivate
            };

            var result = await _pageService.UpdatePageAsync(updateDto, userId);

            if (result.IsSuccess)
            {
                // Clean up superseded files after confirmed DB update
                if (model.ImageFile != null && !string.IsNullOrEmpty(pageResult.Value.ImageUrl) && pageResult.Value.ImageUrl != imageUrl)
                    await _fileStorage.DeleteFileAsync(pageResult.Value.ImageUrl);
                if (model.BackgroundImageFile != null && !string.IsNullOrEmpty(pageResult.Value.BackgroundImageUrl) && pageResult.Value.BackgroundImageUrl != backgroundImageUrl)
                    await _fileStorage.DeleteFileAsync(pageResult.Value.BackgroundImageUrl);

                TempData["SuccessMessage"] = "Page updated successfully";
                return RedirectToAction("Details", new { id = model.Id });
            }

            // Compensating cleanup on failed DB update
            if (model.ImageFile != null && !string.IsNullOrEmpty(imageUrl) && imageUrl != model.ImageUrl)
                await _fileStorage.DeleteFileAsync(imageUrl);
            if (model.BackgroundImageFile != null && !string.IsNullOrEmpty(backgroundImageUrl) && backgroundImageUrl != model.BackgroundImageUrl)
                await _fileStorage.DeleteFileAsync(backgroundImageUrl);

            ModelState.AddModelError("", result.Error);
            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> GetPageStats(Guid pageId)
        {
            if (pageId == Guid.Empty)
                return Json(new { success = false, error = "Invalid page ID." });

            var postsResult = await _postService.GetPagePostsAsync(pageId, Guid.Empty);
            var followersCountResult = await _pageService.GetFollowersCountAsync(pageId);

            var postsCount = postsResult.IsSuccess ? postsResult.Value?.Count() ?? 0 : 0;
            var followersCount = followersCountResult.IsSuccess ? followersCountResult.Value : 0;

            return Json(new
            {
                success = true,
                postsCount = postsCount,
                followersCount = followersCount
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
        public async Task<IActionResult> PromoteMember([FromBody] PromoteDemoteRequest request)
        {
            var adminId = GetCurrentUserId();
            if (request == null || request.PageId == Guid.Empty || request.TargetUserId == Guid.Empty)
                return Json(new { success = false, error = "Invalid request." });

            // Default promotion: Member -> CoAdmin. Optional NewRole override in body for power users.
            var newRole = string.IsNullOrWhiteSpace(request.NewRole)
                ? PageRole.CoAdmin
                : Enum.TryParse<PageRole>(request.NewRole, out var parsed) ? parsed : PageRole.CoAdmin;

            var result = await _pageService.PromotePageMemberAsync(request.PageId, request.TargetUserId, adminId, newRole);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteMember([FromBody] PromoteDemoteRequest request)
        {
            var adminId = GetCurrentUserId();
            if (request == null || request.PageId == Guid.Empty || request.TargetUserId == Guid.Empty)
                return Json(new { success = false, error = "Invalid request." });

            var newRole = string.IsNullOrWhiteSpace(request.NewRole)
                ? PageRole.Member
                : Enum.TryParse<PageRole>(request.NewRole, out var parsed) ? parsed : PageRole.Member;

            var result = await _pageService.DemotePageMemberAsync(request.PageId, request.TargetUserId, adminId, newRole);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferOwnership([FromBody] PageMemberActionRequest request)
        {
            var adminId = GetCurrentUserId();
            if (request == null || request.PageId == Guid.Empty || request.TargetUserId == Guid.Empty)
                return Json(new { success = false, error = "Invalid request." });
            var result = await _pageService.TransferOwnershipAsync(request.PageId, request.TargetUserId, adminId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave([FromBody] LeavePageRequest request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.PageId == Guid.Empty)
                return Json(new { success = false, error = "Invalid page ID." });

            var result = await _pageService.LeavePageAsync(request.PageId, userId, request.Reason);
            return Json(new { success = result.IsSuccess, error = result.Error, outcome = result.IsSuccess ? result.Value : null });
        }

        [HttpGet]
        public async Task<IActionResult> PageRequests(Guid? pageId)
        {
            var userId = GetCurrentUserId();
            var requestsResult = await _pageService.GetPendingRequestsAsync(userId, pageId);
            if (!requestsResult.IsSuccess)
                return Forbid();

            ViewBag.ActivePageId = pageId;
            return View(requestsResult.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("Api")]
        public async Task<IActionResult> SubmitFollowRequest([FromBody] SubmitPageFollowRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.PageId == Guid.Empty)
                return Json(new { success = false, error = "Invalid request." });

            var result = await _pageService.SubmitFollowRequestAsync(userId, request);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("Api")]
        public async Task<IActionResult> AcceptFollowRequest([FromBody] ReviewPageFollowRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.RequestId == Guid.Empty)
                return Json(new { success = false, error = "Invalid request." });

            request.Approve = true;
            var result = await _pageService.ReviewFollowRequestAsync(userId, request);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("Api")]
        public async Task<IActionResult> RejectFollowRequest([FromBody] ReviewPageFollowRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (request == null || request.RequestId == Guid.Empty)
                return Json(new { success = false, error = "Invalid request." });

            request.Approve = false;
            var result = await _pageService.ReviewFollowRequestAsync(userId, request);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        public class PageMemberActionRequest { public Guid PageId { get; set; } public Guid TargetUserId { get; set; } }
        public class PromoteDemoteRequest : PageMemberActionRequest { public string? NewRole { get; set; } }
        public class LeavePageRequest
        {
            public Guid PageId { get; set; }
            public string? Reason { get; set; }
        }



        public class ToggleFollowRequest
        {
            public Guid PageId { get; set; }
        }

    }

}
