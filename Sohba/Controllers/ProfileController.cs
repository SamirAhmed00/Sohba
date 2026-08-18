using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Domain.Common;
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
        private readonly IStoryService _storyService;

        public ProfileController(IUserService userService, IPostService postService, IUserSettingsService userSettingsService, IFriendshipService friendshipService, IFileStorageService fileStorage, IStoryService storyService)
        {
            _userService = userService;
            _postService = postService;
            _userSettingsService = userSettingsService;
            _friendshipService = friendshipService;
            _fileStorage = fileStorage;
            _storyService = storyService;
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


            // Check if user can view friends list
            var isFriend = await _friendshipService.AreFriendsAsync(currentUserId, profileUserId);

            var friendsResult = isFriend || currentUserId == profileUserId
                ? await _friendshipService.GetFriendsListAsync(profileUserId)
                : Result<IEnumerable<FriendDto>>.Success(new List<FriendDto>());

            var postsResult = await _postService.GetUserPostsAsync(profileUserId, currentUserId);

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

            var storiesResult = await _storyService.GetUserStoriesAsync(profileUserId, currentUserId);
            var hasActiveStory = storiesResult.IsSuccess && storiesResult.Value.Any();

            var viewModel = new ProfileViewModel
            {
                Profile = profileResult.Value,
                Friends = friendsResult.Value ?? new List<FriendDto>(),
                Posts = postsResult.Value ?? new List<PostResponseDto>(),
                IsOwnProfile = profileUserId == currentUserId,
                CanViewFriends = canViewFriends,
                IsBlocked = isBlocked,
                FriendshipStatus = friendshipStatus,
                HasActiveStory = hasActiveStory
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
                ProfilePictureUrl = result.Value.ProfilePictureUrl,
                BackgroundImageUrl = result.Value.BackgroundImageUrl
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
                ProfilePictureUrl = model.ProfilePictureUrl,
                BackgroundImageUrl = model.BackgroundImageUrl
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

            // Persist any new uploaded background/banner image through the same storage service.
            if (model.BackgroundImageFile != null && model.BackgroundImageFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveFileAsync(model.BackgroundImageFile, "profiles");
                if (uploadResult.IsSuccess)
                    dto.BackgroundImageUrl = uploadResult.Value;
                else
                    ModelState.AddModelError("BackgroundImageFile", uploadResult.Error);
            }

            if (!ModelState.IsValid)
                return View(model);

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
