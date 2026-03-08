using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.ViewModels.Page;

namespace Sohba.Controllers
{
    [Authorize]
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
            try
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
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckFollowStatus(Guid pageId)
        {
            var userId = GetCurrentUserId();

            var result = await _pageService.IsFollowingAsync(userId, pageId);

            return Json(new { isFollowing = result.Value });
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

            string imageUrl = model.ImageUrl;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                // Validate file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageFile", "Only image files are allowed (jpg, jpeg, png, gif, webp)");
                    return View(model);
                }

                // Validate file size (max 5MB)
                if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Image size cannot exceed 5MB");
                    return View(model);
                }

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pages");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(model.ImageFile.FileName)}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                imageUrl = $"/uploads/pages/{uniqueFileName}";
            }

            var updateDto = new PageUpdateDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                ImageUrl = imageUrl
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
            try
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
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
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
