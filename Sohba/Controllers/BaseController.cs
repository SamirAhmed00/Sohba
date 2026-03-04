namespace Sohba.Controllers
{
    using global::Sohba.Application.DTOs.GroupAndPageAggregate;
    using global::Sohba.Application.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    namespace Sohba.Controllers
    {
        public class BaseController : Controller
        {
            private IGroupService _groupService;

            // Get GroupService from DI
            protected IGroupService GroupService =>
                _groupService ??= HttpContext.RequestServices.GetRequiredService<IGroupService>();

            // This runs before every action
            public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var userId = GetCurrentUserId();
                if (userId != Guid.Empty)
                {
                    // Get recommended groups and store in ViewBag
                    var recommendedGroups = await GroupService.GetRecommendedGroupsAsync(userId, 5);
                    ViewBag.RecommendedGroups = recommendedGroups.Value ?? new List<GroupResponseDto>();
                }

                await next(); // Continue to the action
            }

            protected Guid GetCurrentUserId()
            {
                // Temporary until Identity is implemented
                return new Guid("D1B8F3DC-0E18-F111-8D20-902B34AC4276");
            }

            protected string GetCurrentUserName()
            {
                return User.Identity?.Name ?? string.Empty;
            }
        }
    }
}
