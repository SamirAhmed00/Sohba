using Microsoft.AspNetCore.Mvc;
using Sohba.Application.Interfaces;

namespace Sohba.Controllers
{
    public class FriendsController : Controller
    {
        private readonly ISocialService _socialService;
        private readonly IFriendshipService _friendshipService;

        public FriendsController(ISocialService socialService, IFriendshipService friendshipService)
        {
            _socialService = socialService;
            _friendshipService = friendshipService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var result = await _socialService.GetFriendsListAsync(userId);
            return View(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.SendFriendRequestAsync(currentUserId, userId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(Guid requesterId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.AcceptFriendRequestAsync(requesterId, currentUserId);
            return Json(new { success = result.IsSuccess });
        }

        [HttpPost]
        public async Task<IActionResult> Unfriend(Guid friendId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.UnfriendAsync(currentUserId, friendId);
            return Json(new { success = result.IsSuccess });
        }

        private Guid GetCurrentUserId()
        {
            //var userIdStr = HttpContext.Session.GetString("UserId");
            //return string.IsNullOrEmpty(userIdStr) ? Guid.Empty : Guid.Parse(userIdStr);
            return new Guid("36FF9501-0409-F111-9291-902B34AC4276");

        }
    }
}
