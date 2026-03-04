using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.ViewModels.Friend;

namespace Sohba.Controllers
{
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
        public async Task<IActionResult> RejectRequest(Guid requesterId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.RejectFriendRequestAsync(requesterId, currentUserId);
            return Json(new { success = result.IsSuccess });
        }

       
        [HttpPost]
        public async Task<IActionResult> CancelRequest(Guid receiverId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.CancelFriendRequestAsync(currentUserId, receiverId);
            return Json(new { success = result.IsSuccess });
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



    }
}
