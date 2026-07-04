using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.ViewModels.Friend;

namespace Sohba.Controllers
{
    [Authorize]
    public class FriendsController : BaseController
    {
        //private readonly ISocialService _socialService; // removed because it's The same As FriendshipService
        private readonly IFriendshipService _friendshipService;

        public FriendsController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetFriendsListAsync(userId);
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
            try
            {
                // Guard: model binding can produce a null object or Guid.Empty if the
                // JSON payload is missing / malformed — catch it before hitting the service.
                if (model == null || model.receiverId == Guid.Empty)
                    return Json(BaseResponseDto.FailureResponse(
                        "Invalid request: receiver ID is missing or invalid."));

                var currentUserId = GetCurrentUserId();
                if (currentUserId == Guid.Empty)
                    return Json(BaseResponseDto.FailureResponse(
                        "User not authenticated."));

                var result = await _friendshipService.SendFriendRequestAsync(currentUserId, model.receiverId);
                return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
            }
            catch (Exception ex)
            {
                // Catches DbUpdateException (duplicate key), any service exception, etc.
                // Returns JSON so the JS caller never receives an HTML error page.
                return Json(BaseResponseDto.FailureResponse(
                    $"An unexpected error occurred: {ex.Message}"));
            }
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
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> BlockUser(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.BlockUserAsync(currentUserId, userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUser(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _friendshipService.UnblockUserAsync(currentUserId, userId);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
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
            return Json(BaseResponseDto<IEnumerable<UserResponseDto>>.SuccessResponse(result.Value));
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingRequestsCount()
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.GetPendingRequestsCountAsync(userId);
            return Json(BaseResponseDto<int>.SuccessResponse(result.Value));
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
                return Json(BaseResponseDto<string>.SuccessResponse("accepted"));

            var hasPending = await _friendshipService.HasPendingRequestAsync(currentUserId, userId);
            if (hasPending)
                return Json(BaseResponseDto<string>.SuccessResponse("pending"));

            return Json(BaseResponseDto<string>.SuccessResponse("none"));
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest([FromBody] AcceptRequestModel model)
        {
            try
            {
                if (model == null || model.senderId == Guid.Empty)
                    return Json(BaseResponseDto.FailureResponse("Invalid request: sender ID is missing."));

                var currentUserId = GetCurrentUserId();
                var result = await _friendshipService.AcceptFriendRequestAsync(model.senderId, currentUserId);
                return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
            }
            catch (Exception ex)
            {
                return Json(BaseResponseDto.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest([FromBody] RejectRequestModel model)
        {
            try
            {
                if (model == null || model.requesterId == Guid.Empty)
                    return Json(BaseResponseDto.FailureResponse("Invalid request: requester ID is missing."));

                var currentUserId = GetCurrentUserId();
                var result = await _friendshipService.RejectFriendRequestAsync(model.requesterId, currentUserId);
                return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
            }
            catch (Exception ex)
            {
                return Json(BaseResponseDto.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelRequest([FromBody] CancelRequestModel model)
        {
            try
            {
                if (model == null || model.receiverId == Guid.Empty)
                    return Json(BaseResponseDto.FailureResponse("Invalid request: receiver ID is missing."));

                var currentUserId = GetCurrentUserId();
                var result = await _friendshipService.CancelFriendRequestAsync(currentUserId, model.receiverId);
                return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
            }
            catch (Exception ex)
            {
                return Json(BaseResponseDto.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }

        // Models for binding
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
