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
                {
                    var publicUserResult = await _userService.GetProfileAsync(profileUserId);
                    if (publicUserResult.IsFailure)
                        return NotFound();

                    var senderPendingPrivate = await _friendshipService.HasPendingRequestAsync(profileUserId, currentUserId);
                    var receiverPendingPrivate = await _friendshipService.HasPendingRequestAsync(currentUserId, profileUserId);
                    var privateFriendshipStatus = (senderPendingPrivate || receiverPendingPrivate) ? "pending" : "none";

                    var isBlockedPrivate = currentUserId != profileUserId &&
                        await _friendshipService.IsBlockedAsync(currentUserId, profileUserId);

                    var privateViewModel = new ProfileViewModel
                    {
                        Profile = publicUserResult.Value,
                        Friends = new List<FriendDto>(),
                        Posts = new List<PostResponseDto>(),
                        IsOwnProfile = false,
                        CanViewFriends = false,
                        IsBlocked = isBlockedPrivate,
                        FriendshipStatus = privateFriendshipStatus,
                        HasActiveStory = false
                    };

                    return View("PrivateProfile", privateViewModel);
                }

                if (profileResult.Error != null && profileResult.Error.Contains("block", StringComparison.OrdinalIgnoreCase))
                    return Forbid(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);

                return NotFound();
            }


            var isFriend = await _friendshipService.AreFriendsAsync(currentUserId, profileUserId);
            var isOwnProfile = profileUserId == currentUserId;
            var isPrivateAccount = profileResult.Value.IsPrivateAccount;

            var canViewFriends = isOwnProfile || isFriend || !isPrivateAccount;
            var canViewPosts = isOwnProfile || isFriend || !isPrivateAccount;

            var friendsResult = canViewFriends
                ? await _friendshipService.GetFriendsListAsync(profileUserId)
                : Result<IEnumerable<FriendDto>>.Success(new List<FriendDto>());

            var postsResult = canViewPosts
                ? await _postService.GetUserPostsAsync(profileUserId, currentUserId)
                : Result<IEnumerable<PostResponseDto>>.Success(new List<PostResponseDto>());

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

            var isBlocked = currentUserId != profileUserId &&
                   await _friendshipService.IsBlockedAsync(currentUserId, profileUserId);

            var storiesResult = await _storyService.GetUserStoriesAsync(profileUserId, currentUserId);
            var hasActiveStory = storiesResult.IsSuccess && storiesResult.Value.Any();

            var viewModel = new ProfileViewModel
            {
                Profile = profileResult.Value,
                Friends = friendsResult.Value ?? new List<FriendDto>(),
                Posts = postsResult.Value ?? new List<PostResponseDto>(),
                IsOwnProfile = isOwnProfile,
                CanViewFriends = canViewFriends,
                CanViewPosts = canViewPosts,
                IsPrivate = isPrivateAccount,
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

            // 1. Read current database values
            var profileResult = await _userService.GetProfileAsync(userId);
            if (profileResult.IsFailure)
            {
                ModelState.AddModelError(string.Empty, "User profile not found.");
                return View(model);
            }

            var currentProfile = profileResult.Value;
            var oldProfilePictureUrl = currentProfile.ProfilePictureUrl;
            var oldBackgroundImageUrl = currentProfile.BackgroundImageUrl;

            string? newProfilePictureUrl = null;
            string? newBackgroundImageUrl = null;

            try
            {
                // 2. Upload new profile picture if provided
                if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
                {
                    var uploadResult = await _fileStorage.SaveFileAsync(model.ProfileImageFile, "profiles");
                    if (!uploadResult.IsSuccess)
                    {
                        ModelState.AddModelError(nameof(model.ProfileImageFile), uploadResult.Error);
                        return View(model);
                    }
                    newProfilePictureUrl = uploadResult.Value;
                }

                // 3. Upload new background image if provided
                if (model.BackgroundImageFile != null && model.BackgroundImageFile.Length > 0)
                {
                    var uploadResult = await _fileStorage.SaveFileAsync(model.BackgroundImageFile, "profiles");
                    if (!uploadResult.IsSuccess)
                    {
                        // Clean up newly uploaded profile image if background upload fails
                        if (!string.IsNullOrEmpty(newProfilePictureUrl))
                        {
                            await _fileStorage.DeleteFileAsync(newProfilePictureUrl);
                        }
                        ModelState.AddModelError(nameof(model.BackgroundImageFile), uploadResult.Error);
                        return View(model);
                    }
                    newBackgroundImageUrl = uploadResult.Value;
                }

                // 4. Construct DTO with new URLs if uploaded, otherwise fallback to trusted database URLs
                var dto = new UserRequestDto
                {
                    Name = model.Name,
                    Bio = model.Bio,
                    ProfilePictureUrl = newProfilePictureUrl ?? oldProfilePictureUrl,
                    BackgroundImageUrl = newBackgroundImageUrl ?? oldBackgroundImageUrl
                };

                // 5. Commit database update
                var updateResult = await _userService.UpdateProfileAsync(userId, dto);
                if (!updateResult.IsSuccess)
                {
                    // Database update failed: rollback newly created physical files
                    if (!string.IsNullOrEmpty(newProfilePictureUrl))
                    {
                        await _fileStorage.DeleteFileAsync(newProfilePictureUrl);
                    }
                    if (!string.IsNullOrEmpty(newBackgroundImageUrl))
                    {
                        await _fileStorage.DeleteFileAsync(newBackgroundImageUrl);
                    }

                    ModelState.AddModelError(string.Empty, updateResult.Error);
                    return View(model);
                }

                // 6. ONLY AFTER successful database persistence, safely delete replaced old files
                if (!string.IsNullOrEmpty(newProfilePictureUrl) &&
                    !string.IsNullOrEmpty(oldProfilePictureUrl) &&
                    oldProfilePictureUrl.StartsWith("/uploads/profiles/", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await _fileStorage.DeleteFileAsync(oldProfilePictureUrl);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to delete old profile picture {Url} for user {UserId}", oldProfilePictureUrl, userId);
                    }
                }

                if (!string.IsNullOrEmpty(newBackgroundImageUrl) &&
                    !string.IsNullOrEmpty(oldBackgroundImageUrl) &&
                    oldBackgroundImageUrl.StartsWith("/uploads/profiles/", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await _fileStorage.DeleteFileAsync(oldBackgroundImageUrl);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to delete old background image {Url} for user {UserId}", oldBackgroundImageUrl, userId);
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Emergency cleanup on unexpected exception
                if (!string.IsNullOrEmpty(newProfilePictureUrl))
                {
                    await _fileStorage.DeleteFileAsync(newProfilePictureUrl);
                }
                if (!string.IsNullOrEmpty(newBackgroundImageUrl))
                {
                    await _fileStorage.DeleteFileAsync(newBackgroundImageUrl);
                }

                Logger.LogError(ex, "Unexpected error occurred while updating profile for user {UserId}", userId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving your changes.");
                return View(model);
            }
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.DeactivateAccountAsync(userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.DeleteMyAccountAsync(userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }


        [HttpGet]
        public async Task<IActionResult> Friends(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var targetUserId = id;

            // If navigating to own friends, redirect to standard Friends controller
            if (targetUserId == currentUserId)
            {
                return RedirectToAction("Index", "Friends");
            }

            var profileResult = await _userService.GetProfileAsync(targetUserId, currentUserId);
            if (profileResult.IsFailure)
            {
                if (profileResult.Error != null && profileResult.Error.Contains("private", StringComparison.OrdinalIgnoreCase))
                    return View("PrivateProfile", new ProfileViewModel { Profile = (await _userService.GetProfileAsync(targetUserId)).Value });

                if (profileResult.Error != null && profileResult.Error.Contains("block", StringComparison.OrdinalIgnoreCase))
                    return Forbid(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);

                return NotFound();
            }

            var isFriend = await _friendshipService.AreFriendsAsync(currentUserId, targetUserId);
            var canViewFriends = isFriend || currentUserId == targetUserId || !profileResult.Value.IsPrivateAccount;

            var friendsResult = canViewFriends
                ? await _friendshipService.GetFriendsListAsync(targetUserId)
                : Result<IEnumerable<FriendDto>>.Success(new List<FriendDto>());

            var viewModel = new ProfileFriendsViewModel
            {
                Profile = profileResult.Value,
                Friends = friendsResult.Value ?? new List<FriendDto>(),
                IsOwnProfile = false,
                CanViewFriends = canViewFriends
            };

            return View(viewModel);
        }
    }
}
