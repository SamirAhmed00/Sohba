using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;

using Sohba.ViewModels.Profile;

namespace Sohba.Controllers
{
    [Authorize]
    [EnableRateLimiting("Api")]

    public class ProfileController : BaseController
    {
        private readonly IUserService _userService;
        //private readonly ISocialService _socialService; // removed because it's The same As FriendshipService
        private readonly IPostService _postService;
        private readonly IUserSettingsService _userSettingsService;
        private readonly IFriendshipService _friendshipService;
        private readonly IFileStorageService _fileStorage;

        public ProfileController(IUserService userService, IPostService postService, IUserSettingsService userSettingsService, IFriendshipService friendshipService, IFileStorageService fileStorage)
        {
            _userService = userService;
            _postService = postService;
            _userSettingsService = userSettingsService;
            _friendshipService = friendshipService;
            _fileStorage = fileStorage;
        }


        [HttpGet]
        public async Task<IActionResult> Index(Guid? id)
        {
            var currentUserId = GetCurrentUserId();
            var profileUserId = id ?? currentUserId;

            // PRIVACY CHECK: Get profile with privacy enforcement
            var profileResult = await _userService.GetProfileAsync(profileUserId, currentUserId);

            if (profileResult.IsFailure)
            {
                if (profileResult.Error != null && profileResult.Error.Contains("private", StringComparison.OrdinalIgnoreCase))
                    return View("PrivateProfile", new { UserId = profileUserId });
                return NotFound();
            }

            // Get friends list (may be empty if not allowed to view)
            var friendsResult = await _friendshipService.GetFriendsListAsync(profileUserId);
            var postsResult = await _postService.GetUserPostsAsync(profileUserId, currentUserId);

            // Check if user can view friends list
            var isFriend = await _friendshipService.AreFriendsAsync(currentUserId, profileUserId);

            var friendshipStatus = "none";
            if (isFriend)
            {
                friendshipStatus = "accepted";
            }
            else
            {
                var senderPending = await _friendshipService.HasPendingRequestAsync(profileUserId, currentUserId);
                var receiverPending = await _friendshipService.HasPendingRequestAsync(currentUserId, profileUserId);
                if (senderPending || receiverPending)
                {
                    friendshipStatus = "pending";
                }
            }

            var canViewFriends = currentUserId == profileUserId || isFriend;

            var isBlocked = currentUserId != profileUserId &&
                   await _friendshipService.IsBlockedAsync(currentUserId, profileUserId);

            var viewModel = new ProfileViewModel
            {
                Profile = profileResult.Value,
                Friends = friendsResult.Value ?? new List<FriendDto>(),
                Posts = postsResult.Value ?? new List<PostResponseDto>(),
                IsOwnProfile = profileUserId == currentUserId,
                CanViewFriends = canViewFriends,
                IsBlocked = isBlocked,
                FriendshipStatus = friendshipStatus
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

            // Persist any new uploaded image through IFileStorageService
            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveFileAsync(model.ProfileImageFile, "profiles");
                if (uploadResult.IsSuccess)
                    dto.ProfilePictureUrl = uploadResult.Value;
                else
                    ModelState.AddModelError("ProfileImageFile", uploadResult.Error);
            }

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


        [HttpPost]
        public async Task<IActionResult> Deactivate()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.DeactivateAccountAsync(userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.DeleteMyAccountAsync(userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

    }
}
