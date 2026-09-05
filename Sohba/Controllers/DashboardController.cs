using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.ViewModels.Dashboard;
using Sohba.Domain.Enums;
using Sohba.Application.DTOs.PostAggregate;

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
        private readonly INotificationService _notificationService;

        public DashboardController(
            IUserService userService,
            IPostService postService,
            IGroupService groupService,
            IPageService pageService,
            IReportingService reportingService,
            IFriendshipService friendshipService,
            INotificationService notificationService)
        {
            _userService = userService;
            _postService = postService;
            _groupService = groupService;
            _pageService = pageService;
            _reportingService = reportingService;
            _friendshipService = friendshipService;
            _notificationService = notificationService;
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

            var allUsersResult = await _userService.GetAllUsersAsync();
            var allUsers = allUsersResult.Value?.ToList() ?? new List<UserResponseDto>();
            var allPostsResult = await _postService.GetAllPostsAsync();
            var allPosts = allPostsResult.Value?.ToList() ?? new List<PostResponseDto>();

            var todayUtc = DateTime.UtcNow.Date; 
            viewModel.NewUsersToday = allUsers.Count(u => u.CreatedAt.Date == todayUtc); 
            viewModel.NewPostsToday = allPosts.Count(p => p.CreatedAt.Date == todayUtc); 
            var labels = new List<string>(); 
            var counts = new List<int>(); 
            for (int i = 6; i >= 0; i--) 
            { 
                var targetDate = DateTime.UtcNow.Date.AddDays(-i); 
                labels.Add(targetDate.ToString("MMM dd"));
                counts.Add(allUsers.Count(u => u.CreatedAt.Date == targetDate)); 
            } 
            viewModel.Last7DaysLabels = labels;
            viewModel.UsersLast7Days = counts; 

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserActivity(int days = 7)
        { 
            if (days <= 0) days = 7; 
            var allUsersResult = await _userService.GetAllUsersAsync(); 
            var allUsers = allUsersResult.Value?.ToList() ?? new List<UserResponseDto>(); 
            var labels = new List<string>(); 
            var counts = new List<int>(); 
            for (int i = days - 1; i >= 0; i--) 
            { 
                var targetDate = DateTime.UtcNow.Date.AddDays(-i); 
                labels.Add(targetDate.ToString("MMM dd")); 
                counts.Add(allUsers.Count(u => u.CreatedAt.Date == targetDate));
            } 
            return Json(new { labels, data = counts }); 
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
            var result = await _userService.BlockUserAccountAsync(model.userId);
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUser([FromBody] IdWrapperUserId model)
        {
            if (model == null || model.userId == Guid.Empty)
                         return Json(new { success = false, error = "Invalid user ID." });

            var result = await _userService.UnblockUserAccountAsync(model.userId);

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
        public async Task<IActionResult> DeletePost([FromBody] AdminDeletePostModel model)
        {
            if (model == null || model.postId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid post ID." });
            if (string.IsNullOrWhiteSpace(model.reason))
                    return Json(new { success = false, error = "A deletion reason is required." });
            // Fetch owner + title BEFORE deleting: Post has a global query filter on
                        // IsDeleted, so it becomes unfetchable via the normal EF path immediately
                        // after the soft-delete completes.
            var postResult = await _postService.GetPostByIdAsync(model.postId, GetCurrentUserId());
            if (postResult.IsFailure)
                    return Json(new { success = false, error = postResult.Error });
            
            var postOwnerId = postResult.Value.UserId;
            var postTitle = postResult.Value.Title;
            
            var result = await _postService.DeletePostAsync(model.postId, GetCurrentUserId(), isAdmin: true);
            
            if (result.IsSuccess && postOwnerId != GetCurrentUserId())
            {
                await _notificationService.CreateNotificationAsync(
                receiverId: postOwnerId,
                message: $"Your post \"{postTitle}\" was removed by an administrator. Reason: {model.reason.Trim()}",
                type: NotificationType.SystemAlert,
                senderId: GetCurrentUserId());
            }
            
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
        // ==================== Deleted Groups Moderation ====================

        [HttpGet]
        public async Task<IActionResult> DeletedGroups(string search = "", int page = 1)
        {
            var viewModel = new DashboardDeletedGroupsViewModel
            {
                SearchTerm = search,
                CurrentPage = page,
                PageSize = 20
            };

            var deletedGroupsResult = await _groupService.GetDeletedGroupsAsync();
            if (deletedGroupsResult.IsSuccess)
            {
                var query = deletedGroupsResult.Value.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(g =>
                        g.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        g.DeletionReason.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        g.OwnerName.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                viewModel.TotalCount = query.Count();
                viewModel.DeletedGroups = query
                    .Skip((page - 1) * viewModel.PageSize)
                    .Take(viewModel.PageSize)
                    .ToList();
            }

            return View(viewModel);
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
            var deleteResult = await _postService.DeletePostAsync(model.postId, GetCurrentUserId(), isAdmin: true);
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
        public class AdminDeletePostModel { public Guid postId { get; set; } public string reason { get; set; } }
        public class IdWrapperReportId { public Guid reportId { get; set; } }
        public class DeleteReportedPostModel { public Guid postId { get; set; } public Guid reportId { get; set; } }
    }
}