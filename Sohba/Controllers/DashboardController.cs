using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.ViewModels.Dashboard;

namespace Sohba.Controllers
{
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("Dashboard")]
    public class DashboardController : BaseController 
    {
        private readonly IUserService _userService;
        private readonly IPostService _postService;
        private readonly IGroupService _groupService;
        private readonly IPageService _pageService;
        private readonly IReportingService _reportingService;
        private readonly IFriendshipService _friendshipService;

        public DashboardController(
            IUserService userService,
            IPostService postService,
            IGroupService groupService,
            IPageService pageService,
            IReportingService reportingService,
            IFriendshipService friendshipService)
        {
            _userService = userService;
            _postService = postService;
            _groupService = groupService;
            _pageService = pageService;
            _reportingService = reportingService;
            _friendshipService = friendshipService;
        }

        // GET: /Dashboard
        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();

            var usersCount = await _userService.GetUsersCountAsync();
            var postsCount = await _postService.GetPostsCountAsync();
            var groupsCount = await _groupService.GetGroupsCountAsync();
            var pagesCount = await _pageService.GetPagesCountAsync();
            var pendingReportsCount = await _reportingService.GetPendingReportsCountAsync();
            
            viewModel.TotalUsers = usersCount.Value;
            viewModel.TotalPosts = postsCount.Value;
            viewModel.TotalGroups = groupsCount.Value;
            viewModel.TotalPages = pagesCount.Value;
            viewModel.PendingReports = pendingReportsCount.Value;
            
            
            var recentUsers = await _userService.GetRecentUsersAsync(5);
            var recentPosts = await _postService.GetRecentPostsAsync(5);
            var recentReports = await _reportingService.GetRecentPendingReportsAsync(5);
            
            viewModel.RecentUsers = recentUsers.Value?.ToList() ?? new();
            viewModel.RecentPosts = recentPosts.Value?.ToList() ?? new();
            viewModel.RecentReports = recentReports.Value?.ToList() ?? new();

            // TODO: Get users count for last 7 days
            viewModel.UsersLast7Days = new List<int> { 5, 8, 12, 7, 15, 10, 20 };
            viewModel.Last7DaysLabels = new List<string> {
                DateTime.Now.AddDays(-6).ToString("MMM dd"),
                DateTime.Now.AddDays(-5).ToString("MMM dd"),
                DateTime.Now.AddDays(-4).ToString("MMM dd"),
                DateTime.Now.AddDays(-3).ToString("MMM dd"),
                DateTime.Now.AddDays(-2).ToString("MMM dd"),
                DateTime.Now.AddDays(-1).ToString("MMM dd"),
                DateTime.Now.ToString("MMM dd")
            };

            return View(viewModel);
        }

        // ==================== Users Management ====================

        [HttpGet]
        public async Task<IActionResult> Users(string search = "", string status = "all", int page = 1)
        {
            var viewModel = new DashboardUsersViewModel
            {
                SearchTerm = search,
                StatusFilter = status,
                CurrentPage = page,
                PageSize = 20
            };

            Result<IEnumerable<UserResponseDto>> usersResult;

            if (status == "active" || status == "blocked")
            {
                usersResult = await _userService.GetUsersByStatusAsync(status);
            }
            else
            {
                usersResult = await _userService.GetAllUsersAsync();
            }

            if (usersResult.IsSuccess)
            {
                var query = usersResult.Value.AsQueryable();

                // Apply search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(u =>
                        u.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                viewModel.TotalCount = query.Count();
                viewModel.Users = query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * viewModel.PageSize)
                    .Take(viewModel.PageSize)
                    .ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> BlockUser([FromBody] IdWrapperUserId model)
        {
            if (model == null || model.userId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid user ID." });
            var result = await _friendshipService.BlockUserAsync(GetCurrentUserId(), model.userId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUser([FromBody] IdWrapperUserId model)
        {
            if (model == null || model.userId == Guid.Empty)
                         return Json(new { success = false, error = "Invalid user ID." });

            var result = await _friendshipService.UnblockUserAsync(GetCurrentUserId(), model.userId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] IdWrapperUserId model)
        {
            if (model == null || model.userId == Guid.Empty)
                        return Json(new { success = false, error = "Invalid user ID." });
            var result = await _userService.DeleteUserAsync(model.userId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        // ==================== Posts Management ====================

        [HttpGet]
        public async Task<IActionResult> Posts(string search = "", string source = "all", int page = 1)
        {
            var viewModel = new DashboardPostsViewModel 
            {
                SearchTerm = search,
                SourceFilter = source,
                CurrentPage = page,
                PageSize = 20
            };

            var postsResult = await _postService.GetAllPostsAsync();
            if (postsResult.IsSuccess)
            {
                var query = postsResult.Value.AsQueryable();

                // Apply search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p =>
                        p.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        p.Content.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                // Apply source filter
                if (source != "all")
                {
                    query = query.Where(p => p.SourceType.Equals(source, StringComparison.OrdinalIgnoreCase));
                }

                viewModel.TotalCount = query.Count();
                viewModel.Posts = query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * viewModel.PageSize)
                    .Take(viewModel.PageSize)
                    .ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost([FromBody] IdWrapperPostId model)
        {
            if (model == null || model.postId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid post ID." });
            var result = await _postService.DeletePostAsync(model.postId, GetCurrentUserId(), isAdmin: true);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> HidePost([FromBody] IdWrapperPostId model)
        {
            if (model == null || model.postId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid post ID." });            
            var result = await _postService.HidePostAsync(model.postId, GetCurrentUserId());
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        // ==================== Reports Management ====================

        [HttpGet]
        public async Task<IActionResult> Reports(string status = "pending", int page = 1)
        {
            var viewModel = new DashboardReportsViewModel 
            {
                StatusFilter = status,
                CurrentPage = page,
                PageSize = 20
            };

            var reportsResult = await _reportingService.GetAllReportsAsync();
            if (reportsResult.IsSuccess)
            {
                var query = reportsResult.Value.AsQueryable();

                // Apply status filter
                if (status == "pending")
                {
                    query = query.Where(r => !r.IsResolved);
                }
                else if (status == "resolved")
                {
                    query = query.Where(r => r.IsResolved);
                }

                viewModel.TotalCount = query.Count();
                viewModel.Reports = query
                    .OrderByDescending(r => r.ReportedAt)
                    .Skip((page - 1) * viewModel.PageSize)
                    .Take(viewModel.PageSize)
                    .ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ResolveReport([FromBody] IdWrapperReportId model)
        {
            if (model == null || model.reportId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid report ID." });
            var result = await _reportingService.ResolveReportAsync(model.reportId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DismissReport([FromBody] IdWrapperReportId model)
        {
            if (model == null || model.reportId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid report ID." });
            var result = await _reportingService.ResolveReportAsync(model.reportId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReportedPost([FromBody] DeleteReportedPostModel model)
        {
            if (model == null || model.postId == Guid.Empty || model.reportId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid post or report ID." });            
            var deleteResult = await _postService.DeletePostAsync(model.postId, GetCurrentUserId());
            if (deleteResult.IsSuccess)
            {
                await _reportingService.ResolveReportAsync(model.reportId);
            }
            return Json(new { success = deleteResult.IsSuccess, error = deleteResult.Error });
        }

        // ==================== Modal Actions ====================

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(Guid userId)
        {
            var result = await _userService.GetProfileAsync(userId);
            if (result.IsSuccess)
            {
                return PartialView("Partials/_UserDetails", result.Value);
            }
            return Content("User not found");
        }

        [HttpGet]
        public async Task<IActionResult> GetPostDetails(Guid postId)
        {            
            var result = await _postService.GetPostByIdAsync(postId, GetCurrentUserId());
            if (result.IsSuccess)
            {
                return PartialView("Partials/_PostDetails", result.Value);
            }
            return Content("Post not found");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportDetails(Guid reportId)
        {
            var reports = await _reportingService.GetAllReportsAsync();
            var report = reports.Value?.FirstOrDefault(r => r.Id == reportId);
            if (report != null)
            {
                return PartialView("Partials/_ReportDetails", report);
            }
            return Content("Report not found");
        }


        public class IdWrapperUserId { public Guid userId { get; set; } }
        public class IdWrapperPostId { public Guid postId { get; set; } }
        public class IdWrapperReportId { public Guid reportId { get; set; } }
        public class DeleteReportedPostModel { public Guid postId { get; set; } public Guid reportId { get; set; } }
    }
}