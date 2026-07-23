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

        // ----- TODO: i Will Make it Injected In Constructor And Make All Controlles That Inherit From BaseController To Use Constructor Injection Instead Of Using RequestServices -----
        protected ILogger<BaseController> Logger =>
     HttpContext.RequestServices.GetRequiredService<ILogger<BaseController>>();


        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = GetCurrentUserId();
            if (userId != Guid.Empty)
            {
                var recommendedGroups = await GroupService.GetRecommendedGroupsAsync(userId, 5);
                ViewBag.RecommendedGroups = recommendedGroups.Value ?? new List<GroupResponseDto>();
                SetJwtTokenInViewBag();
            }

            await next();
        }

        protected Guid GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId != null ? Guid.Parse(userId) : Guid.Empty;
        }

        protected void SetJwtTokenInViewBag()
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
                    var user = userManager.FindByIdAsync(userId.ToString()).GetAwaiter().GetResult();
                    if (user != null)
                    {
                        var roles = userManager.GetRolesAsync(user).GetAwaiter().GetResult();
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