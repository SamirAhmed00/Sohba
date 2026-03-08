using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.ViewModels.Friend;

namespace Sohba.Controllers
{
    [Authorize]
    public class FriendsController : BaseController
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

        [HttpGet]
        public async Task<IActionResult> Requests()
        {
            var userId = GetCurrentUserId();

            var pendingResult = await _friendshipService.GetPendingRequestsAsync(userId);
            var sentResult = await _friendshipService.GetSentRequestsAsync(userId);
            var pendingCountResult = await _friendshipService.GetPendingRequestsCountAsync(userId);

            var viewModel = new FriendRequestsViewModel
            {
                PendingRequests = pendingResult.Value ?? new List<FriendDto>(),
                SentRequests = sentResult.Value ?? new List<FriendDto>(),
                PendingCount = pendingCountResult.Value,
                SentCount = sentResult.Value?.Count() ?? 0
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Blocked()
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetBlockedUsersAsync(userId);
            return View(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest([FromBody] SendRequestModel model)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.SendFriendRequestAsync(currentUserId, model.receiverId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        public class SendRequestModel
        {
            public Guid receiverId { get; set; }
        }


        [HttpPost]
        public async Task<IActionResult> Unfriend(Guid friendId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.UnfriendAsync(currentUserId, friendId);
            return Json(new { success = result.IsSuccess });
        }

        [HttpPost]
        public async Task<IActionResult> BlockUser(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.BlockUserAsync(currentUserId, userId);
            return Json(new { success = result.IsSuccess });
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUser(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.UnblockUserAsync(currentUserId, userId);
            return Json(new { success = result.IsSuccess });
        }

        [HttpGet]
        public async Task<IActionResult> Suggestions()
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetFriendSuggestionsAsync(userId, 20);
            return View(result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetFriendSuggestions(int count = 5)
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetFriendSuggestionsAsync(userId, count);
            return Json(result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingRequestsCount()
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetPendingRequestsCountAsync(userId);
            return Json(new { count = result.Value });
        }

        [HttpGet]
        public async Task<IActionResult> Find()
        {
            var userId = GetCurrentUserId();
            var suggestions = await _friendshipService.GetFriendSuggestionsAsync(userId, 20);
            return View(suggestions.Value);
        }

        [HttpGet]
        public async Task<IActionResult> CheckStatus(Guid userId)
        {
            var currentUserId = GetCurrentUserId();

            var areFriends = await _friendshipService.AreFriendsAsync(currentUserId, userId);
            if (areFriends)
                return Json(new { status = "accepted" });

            var hasPending = await _friendshipService.HasPendingRequestAsync(currentUserId, userId);
            if (hasPending)
                return Json(new { status = "pending" });

            return Json(new { status = "none" });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest([FromBody] AcceptRequestModel model)
        {
            var currentUserId = GetCurrentUserId();
            Console.WriteLine($"🔍 AcceptRequest - requesterId: {model.requesterId}, currentUserId: {currentUserId}");
            var result = await _friendshipService.AcceptFriendRequestAsync(model.requesterId, currentUserId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest([FromBody] RejectRequestModel model)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.RejectFriendRequestAsync(model.requesterId, currentUserId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> CancelRequest([FromBody] CancelRequestModel model)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.CancelFriendRequestAsync(currentUserId, model.receiverId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        // Models for binding
        public class AcceptRequestModel
        {
            public Guid requesterId { get; set; }
        }

        public class RejectRequestModel
        {
            public Guid requesterId { get; set; }
        }

        public class CancelRequestModel
        {
            public Guid receiverId { get; set; }
        }
    }
}
