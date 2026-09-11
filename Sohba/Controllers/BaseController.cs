using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Domain.Entities.UserAggregate;
using System.Security.Claims;

namespace Sohba.Controllers
{
    public class BaseController : Controller
    {
        private IGroupService _groupService;

        protected IGroupService GroupService =>
            _groupService ??= HttpContext.RequestServices.GetRequiredService<IGroupService>();
        
        protected ILogger<BaseController> Logger =>
     HttpContext.RequestServices.GetRequiredService<ILogger<BaseController>>();


        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = GetCurrentUserId();
            User? currentUser = null;

            if (userId != Guid.Empty)
            {
                var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                currentUser = await userManager.FindByIdAsync(userId.ToString());

                if (currentUser == null || currentUser.IsBlocked || !currentUser.IsActive)
                {
                    var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<User>>();
                    await signInManager.SignOutAsync();

                    var message = currentUser == null
                        ? "This account has been deleted and is no longer available."
                        : currentUser.IsBlocked
                            ? "Your account has been blocked. Please contact support."
                            : "Your account has been deactivated. Please log in again to reactivate.";

                    bool isAjaxOrJson = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                        || context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json");

                    if (isAjaxOrJson)
                    {
                        context.Result = new JsonResult(Sohba.Application.DTOs.Common.BaseResponseDto.FailureResponse(message))
                        {
                            StatusCode = StatusCodes.Status401Unauthorized
                        };
                    }
                    else
                    {
                        context.Result = new RedirectToActionResult("Login", "Auth", null);
                    }
                    return;
                }
            }

            // Skip heavy work for unauthenticated requests and JSON/AJAX endpoints
            var isJsonRequest = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || context.HttpContext.Request.Path.Value?.Contains("/Get", StringComparison.OrdinalIgnoreCase) == true
                || context.HttpContext.Request.Path.Value?.Contains("/Quick", StringComparison.OrdinalIgnoreCase) == true;

            if (userId != Guid.Empty && !isJsonRequest)
            {
                var recommendedGroups = await GroupService.GetRecommendedGroupsAsync(userId, 5);
                ViewBag.RecommendedGroups = recommendedGroups.Value ?? new List<GroupResponseDto>();
                await SetJwtTokenInViewBag(currentUser);
            }

            await next();
        }

        protected Guid GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var parsed) ? parsed : Guid.Empty;
        }

        protected async Task SetJwtTokenInViewBag(User? preloadedUser = null)
        {
            var userId = GetCurrentUserId();
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length);
                    ViewBag.JwtToken = token;
                    return;
                }

                if (userId != Guid.Empty)
                {
                    var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();
                    var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                    var user = preloadedUser ?? await userManager.FindByIdAsync(userId.ToString());
                    if (user != null)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        var token = jwtService.GenerateToken(user, roles);
                        ViewBag.JwtToken = token;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to set JWT token in ViewBag for user {UserId}", userId);
            }
        }
        protected string GetCurrentUserName()
        {
            return User.Identity?.Name ?? string.Empty;
        }
    }
}