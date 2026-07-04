using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Controllers.Sohba.Controllers;

namespace Sohba.Controllers
{
    [Authorize]
    public class NotificationsController : BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public NotificationsController(INotificationService notificationService, IMapper mapper)
        {
            _notificationService = notificationService;
            _mapper = mapper;
        }

        // Display notifications page
        public async Task<IActionResult> Index(int page = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            var result = await _notificationService.GetUserNotificationsAsync(userId, page, 20);

            if (result.IsFailure)
                return View(new List<NotificationResponseDto>());

            var dtos = _mapper.Map<IEnumerable<NotificationResponseDto>>(result.Value);
            return View(dtos);
        }

        // Get unread count for header
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Json(new { count = 0 });

            var result = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { count = result.IsSuccess ? result.Value : 0 });
        }

        // Get unread notifications for dropdown
        [HttpGet]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Json(BaseResponseDto<IEnumerable<NotificationResponseDto>>.FailureResponse("Unauthorized"));

            var result = await _notificationService.GetUnreadNotificationsAsync(userId);
            if (result.IsFailure)
                return Json(BaseResponseDto<IEnumerable<NotificationResponseDto>>.FailureResponse(result.Error));

            var dtos = _mapper.Map<IEnumerable<NotificationResponseDto>>(result.Value);
            return Json(BaseResponseDto<IEnumerable<NotificationResponseDto>>.SuccessResponse(dtos));
        }

        // Mark a notification as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Unauthorized"));

            var result = await _notificationService.MarkAsReadAsync(id, userId);
            if (result.IsFailure)
                return Json(BaseResponseDto.FailureResponse(result.Error));

            return Json(BaseResponseDto.SuccessResponse());
        }

        // Mark all notifications as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Unauthorized"));

            var result = await _notificationService.MarkAllAsReadAsync(userId);
            if (result.IsFailure)
                return Json(BaseResponseDto.FailureResponse(result.Error));

            return Json(BaseResponseDto.SuccessResponse());
        }

        // Delete a notification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Json(BaseResponseDto.FailureResponse("Unauthorized"));

            var result = await _notificationService.DeleteNotificationAsync(id, userId);
            if (result.IsFailure)
                return Json(BaseResponseDto.FailureResponse(result.Error));

            return Json(BaseResponseDto.SuccessResponse());
        }
    }
}