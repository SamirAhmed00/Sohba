using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.ViewModels.Profile;

namespace Sohba.Controllers
{
    public class ProfileController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ISocialService _socialService;
        private readonly IPostService _postService;
        private readonly IUserSettingsService _userSettingsService;

        public ProfileController(IUserService userService, ISocialService socialService, IPostService postService, IUserSettingsService userSettingsService)
        {
            _userService = userService;
            _socialService = socialService;
            _postService = postService;
            _userSettingsService = userSettingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Guid? id)
        {
            var userId = id ?? GetCurrentUserId();

            var profileResult = await _userService.GetProfileAsync(userId);
            if (profileResult.IsFailure) return NotFound();

            var friendsResult = await _socialService.GetFriendsListAsync(userId);
            var postsResult = await _postService.GetFeedAsync(userId); // Should be GetUserPosts

            var viewModel = new ProfileViewModel
            {
                Profile = profileResult.Value,
                Friends = friendsResult.Value ?? new List<FriendDto>(),
                Posts = postsResult.Value ?? new List<PostResponseDto>(),
                IsOwnProfile = userId == GetCurrentUserId()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.GetProfileAsync(userId);

            if (result.IsFailure) return NotFound();

            var viewModel = new EditProfileViewModel
            {
                Name = result.Value.Name,
                Bio = result.Value.Bio,
                ProfilePictureUrl = result.Value.ProfilePictureUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            var dto = new UserRequestDto
            {
                Name = model.Name,
                Bio = model.Bio,
                ProfilePictureUrl = model.ProfilePictureUrl
            };

            var result = await _userService.UpdateProfileAsync(userId, dto);

            if (result.IsSuccess)
                return RedirectToAction("Index");

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var userId = GetCurrentUserId();
            var result = await _userSettingsService.GetSettingsAsync(userId);

            if (result.IsFailure)
                return NotFound();

            var viewModel = new SettingsViewModel
            {
                Email = result.Value.Email,
                Name = result.Value.Name,
                Bio = result.Value.Bio,
                ProfilePictureUrl = result.Value.ProfilePictureUrl,
                IsPrivateAccount = result.Value.IsPrivateAccount,
                ShowActivityStatus = result.Value.ShowActivityStatus,
                EmailNotifications = result.Value.EmailNotifications,
                PushNotifications = result.Value.PushNotifications,
                WeeklyDigest = result.Value.WeeklyDigest
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();
            var dto = new UserSettingsDto
            {
                Email = model.Email,
                Name = model.Name,
                Bio = model.Bio,
                ProfilePictureUrl = model.ProfilePictureUrl,
                IsPrivateAccount = model.IsPrivateAccount,
                ShowActivityStatus = model.ShowActivityStatus,
                EmailNotifications = model.EmailNotifications,
                PushNotifications = model.PushNotifications,
                WeeklyDigest = model.WeeklyDigest
            };

            var result = await _userSettingsService.UpdateSettingsAsync(userId, dto);
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Settings updated successfully";
                return RedirectToAction("Settings");
            }

            ModelState.AddModelError("", result.Error);
            return View(model);
        }

    }
}
