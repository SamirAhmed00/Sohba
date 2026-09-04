using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.ViewModels.Friend;
using static Sohba.Controllers.FriendsController;

namespace Sohba.Controllers
{
    [Authorize]
    public class FriendsController : BaseController
    {
        private readonly IFriendshipService _friendshipService;

        public FriendsController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q = null, int page = 1)
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetFriendsListPagedAsync(userId, q, page, 12);
            ViewBag.SearchQuery = q;
            return View(result.Value ?? new PagedResult<FriendDto>());
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
            return View(result.Value ?? Enumerable.Empty<FriendDto>());
        }

        [HttpPost]
        [EnableRateLimiting("FriendRequest")]
        public async Task<IActionResult> SendRequest([FromBody] SendRequestModel model)
        {
            if (model == null || model.receiverId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request: receiver ID is missing or invalid."));

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("User not authenticated."));

            var result = await _friendshipService.SendFriendRequestAsync(currentUserId, model.receiverId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        [EnableRateLimiting("FriendRequest")]
        public async Task<IActionResult> Unfriend([FromBody] UnfriendModel model)
        {
            var currentUserId = GetCurrentUserId();
            if (model == null || model.friendId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request: friend ID is missing."));

            var result = await _friendshipService.UnfriendAsync(currentUserId, model.friendId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        [EnableRateLimiting("FriendRequest")]
        public async Task<IActionResult> BlockUser([FromBody] BlockUserModel model)
        {
            if (model == null || model.userId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request: user ID is missing."));

            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.BlockUserAsync(currentUserId, model.userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        [EnableRateLimiting("FriendRequest")]
        public async Task<IActionResult> UnblockUser([FromBody] UnblockUserModel model)
        {
            var currentUserId = GetCurrentUserId();
            if (model == null || model.userId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request: user ID is missing."));

            var result = await _friendshipService.UnblockUserAsync(currentUserId, model.userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpGet]
        public async Task<IActionResult> Suggestions()
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetFriendSuggestionsAsync(userId, 20);
            return View(result.Value ?? Enumerable.Empty<UserResponseDto>());
        }

        [HttpGet]
        public async Task<IActionResult> GetFriendSuggestions(int count = 5)
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetFriendSuggestionsAsync(userId, count);
            return Json(BaseResponseDto<IEnumerable<UserResponseDto>>.SuccessResponse(result.Value ?? Enumerable.Empty<UserResponseDto>()));
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingRequestsCount()
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetPendingRequestsCountAsync(userId);
            return Json(BaseResponseDto<int>.SuccessResponse(result.Value));
        }

        [HttpGet]
        public async Task<IActionResult> CheckStatus(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty || userId == Guid.Empty)
                return Json(BaseResponseDto<string>.SuccessResponse("none"));

            var areFriends = await _friendshipService.AreFriendsAsync(currentUserId, userId);
            if (areFriends)
                return Json(BaseResponseDto<string>.SuccessResponse("accepted"));

            var sentRequests = await _friendshipService.GetSentRequestsAsync(currentUserId);
            if (sentRequests.Value != null && sentRequests.Value.Any(r => r.FriendUserId == userId))
                return Json(BaseResponseDto<string>.SuccessResponse("pending_sent"));

            var receivedRequests = await _friendshipService.GetPendingRequestsAsync(currentUserId);
            if (receivedRequests.Value != null && receivedRequests.Value.Any(r => r.FriendUserId == userId))
                return Json(BaseResponseDto<string>.SuccessResponse("pending_received"));

            return Json(BaseResponseDto<string>.SuccessResponse("none"));
        }

        [HttpPost]
        [EnableRateLimiting("FriendRequest")]
        public async Task<IActionResult> AcceptRequest([FromBody] AcceptRequestModel model)
        {
            if (model == null || model.senderId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request: sender ID is missing."));

            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.AcceptFriendRequestAsync(model.senderId, currentUserId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        [EnableRateLimiting("FriendRequest")]
        public async Task<IActionResult> RejectRequest([FromBody] RejectRequestModel model)
        {
            if (model == null || model.requesterId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request: requester ID is missing."));

            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.RejectFriendRequestAsync(model.requesterId, currentUserId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        [EnableRateLimiting("FriendRequest")]
        public async Task<IActionResult> CancelRequest([FromBody] CancelRequestModel model)
        {
            if (model == null || model.receiverId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Invalid request: receiver ID is missing."));

            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.CancelFriendRequestAsync(currentUserId, model.receiverId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        // Models for binding
        public class SendRequestModel
        {
            public Guid receiverId { get; set; }
        }

        public class UnfriendModel
        {
            public Guid friendId { get; set; }
        }

        public class BlockUserModel
        {
            public Guid userId { get; set; }
        }

        public class UnblockUserModel
        {
            public Guid userId { get; set; }
        }

        public class AcceptRequestModel
        {
            public Guid senderId { get; set; }
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
