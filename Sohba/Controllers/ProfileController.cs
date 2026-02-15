using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.ViewModels.Profile;

namespace Sohba.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly ISocialService _socialService;
        private readonly IPostService _postService;

        public ProfileController(IUserService userService, ISocialService socialService, IPostService postService)
        {
            _userService = userService;
            _socialService = socialService;
            _postService = postService;
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
            var profileResult = await _userService.GetProfileAsync(userId);

            if (profileResult.IsFailure) return NotFound();

            var viewModel = new SettingsViewModel
            {
                Email = profileResult.Value.Email,
                IsPrivateAccount = false, // TODO: Get from user settings
                ShowActivityStatus = true,
                EmailNotifications = true,
                PushNotifications = true
            };

            return View(viewModel);
        }

        private Guid GetCurrentUserId()
        {
            //var userIdStr = HttpContext.Session.GetString("UserId");
            //return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);

            return new Guid("36FF9501-0409-F111-9291-902B34AC4276");
        }
    }
}
