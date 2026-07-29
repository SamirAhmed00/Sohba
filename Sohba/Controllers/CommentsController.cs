using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.Interfaces;

using System;
using System.Threading.Tasks;

namespace Sohba.Controllers
{
    [Authorize]
    [EnableRateLimiting("Api")]

    public class CommentsController : BaseController
    {
        private readonly IInteractionService _interactionService;

        public CommentsController(IInteractionService interactionService)
        {
            _interactionService = interactionService;
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return Json(BaseResponseDto<object>.FailureResponse("Invalid comment ID."));

                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                    return Json(BaseResponseDto<object>.FailureResponse("User not authenticated."));

                // The domain rule (comment author or post owner) is strictly enforced inside DeleteCommentAsync
                bool isAdmin = User.IsInRole("Admin");
                var result = await _interactionService.DeleteCommentAsync(userId, id, isAdmin);

                if (result.IsSuccess)
                    return Json(BaseResponseDto<object>.SuccessResponse(null));

                return Json(BaseResponseDto<object>.FailureResponse(result.Error));
            }
            catch (Exception ex)
            {
                // Global exception handling standard per RULES.md §6
                return Json(BaseResponseDto<object>.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }
    }
}
