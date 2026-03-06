using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Controllers.Sohba.Controllers;
using Sohba.Domain.Common;
using Sohba.ViewModels.Dashboard;

namespace Sohba.Controllers
{
    [Authorize(Roles = "Admin")]
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

            // Get counts
            var users = await _userService.GetAllUsersAsync();
            var posts = await _postService.GetAllPostsAsync();
            var groups = await _groupService.GetAllGroupsAsync();
            var pages = await _pageService.GetAllPagesAsync();
            var reports = await _reportingService.GetAllReportsAsync();

            viewModel.TotalUsers = users.Value?.Count() ?? 0;
            viewModel.TotalPosts = posts.Value?.Count() ?? 0;
            viewModel.TotalGroups = groups.Value?.Count() ?? 0;
            viewModel.TotalPages = pages.Value?.Count() ?? 0;
            viewModel.PendingReports = reports.Value?.Count(r => !r.IsResolved) ?? 0;

            // Get recent users (last 5)
            viewModel.RecentUsers = users.Value?
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToList() ?? new();

            // Get recent posts (last 5)
            viewModel.RecentPosts = posts.Value?
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToList() ?? new();

            // Get recent reports (last 5 pending)
            viewModel.RecentReports = reports.Value?
                .Where(r => !r.IsResolved)
                .OrderByDescending(r => r.ReportedAt)
                .Take(5)
                .ToList() ?? new();

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
        public async Task<IActionResult> BlockUser(Guid userId)
        {
            var result = await _friendshipService.BlockUserAsync(GetCurrentUserId(), userId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUser(Guid userId)
        {
            var result = await _friendshipService.UnblockUserAsync(GetCurrentUserId(), userId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
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
        public async Task<IActionResult> DeletePost(Guid postId)
        {
            var result = await _postService.DeletePostAsync(postId, GetCurrentUserId());
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> HidePost(Guid postId)
        {
            var result = await _postService.HidePostAsync(postId, GetCurrentUserId());
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
        public async Task<IActionResult> ResolveReport(Guid reportId)
        {
            var result = await _reportingService.ResolveReportAsync(reportId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DismissReport(Guid reportId)
        {
            // Dismiss is same as resolve for now
            var result = await _reportingService.ResolveReportAsync(reportId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReportedPost(Guid postId, Guid reportId)
        {
            var deleteResult = await _postService.DeletePostAsync(postId, GetCurrentUserId());
            if (deleteResult.IsSuccess)
            {
                await _reportingService.ResolveReportAsync(reportId);
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
            var result = await _postService.GetPostByIdAsync(postId);
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
    }
}