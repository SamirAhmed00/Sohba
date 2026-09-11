using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.AdminAggregate;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.ViewModels.Dashboard;

namespace Sohba.Controllers
{
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("Dashboard")]
    [AutoValidateAntiforgeryToken]
    public class DashboardController : BaseController 
    {
        private readonly IUserService _userService;
        private readonly IPostService _postService;
        private readonly IGroupService _groupService;
        private readonly IPageService _pageService;
        private readonly IReportingService _reportingService;
        private readonly IFriendshipService _friendshipService;
        private readonly INotificationService _notificationService;
        private readonly IStoryService _storyService;
        private readonly IInteractionService _interactionService;
        private readonly IUnitOfWork _unitOfWork;

        public DashboardController(
            IUserService userService,
            IPostService postService,
            IGroupService groupService,
            IPageService pageService,
            IReportingService reportingService,
            IFriendshipService friendshipService,
            INotificationService notificationService,
            IStoryService storyService,
            IInteractionService interactionService,
            IUnitOfWork unitOfWork)
        {
            _userService = userService;
            _postService = postService;
            _groupService = groupService;
            _pageService = pageService;
            _reportingService = reportingService;
            _friendshipService = friendshipService;
            _notificationService = notificationService;
            _storyService = storyService;
            _interactionService = interactionService;
            _unitOfWork = unitOfWork;
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

            var todayUtc = DateTime.UtcNow.Date;
            viewModel.NewUsersToday = await _unitOfWork.Users.GetNewUsersCountSinceAsync(todayUtc);
            viewModel.NewPostsToday = await _unitOfWork.Posts.GetNewPostsCountSinceAsync(todayUtc);

            var registrationsByDay = await _unitOfWork.Users.GetUserRegistrationsByDayAsync(7);
            var labels = new List<string>();
            var counts = new List<int>();

            for (int i = 6; i >= 0; i--)
            {
                var targetDate = todayUtc.AddDays(-i);
                labels.Add(targetDate.ToString("MMM dd"));
                counts.Add(registrationsByDay.TryGetValue(targetDate, out var count) ? count : 0);
            }

            viewModel.Last7DaysLabels = labels;
            viewModel.UsersLast7Days = counts;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserActivity(int days = 7)
        {
            if (days <= 0) days = 7;
            var todayUtc = DateTime.UtcNow.Date;
            var registrationsByDay = await _unitOfWork.Users.GetUserRegistrationsByDayAsync(days);
            var labels = new List<string>();
            var counts = new List<int>();

            for (int i = days - 1; i >= 0; i--)
            {
                var targetDate = todayUtc.AddDays(-i);
                labels.Add(targetDate.ToString("MMM dd"));
                counts.Add(registrationsByDay.TryGetValue(targetDate, out var count) ? count : 0);
            }

            return Json(new { labels, data = counts });
        }



        // ==================== Users Management ====================

        [HttpGet]
        public async Task<IActionResult> Users(string search = "", string status = "all", int page = 1)
        {
            var pagedResult = await _userService.GetUsersAdminPagedAsync(search, status, page, 20);

            var currentAdminId = GetCurrentUserId();

            var userManager = HttpContext.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();

            var currentAdmin = await userManager.FindByIdAsync(currentAdminId.ToString());

            var isCurrentAdminOwner =
                currentAdmin != null &&
                await userManager.IsInRoleAsync(currentAdmin, "Owner");

            var userList = pagedResult.Value?.Items?.ToList()
                ?? new List<UserResponseDto>();

            // Populate role hierarchy display data
            foreach (var u in userList)
            {
                var entityUser = await userManager.FindByIdAsync(u.Id.ToString());

                if (entityUser == null)
                {
                    continue;
                }

                if (await userManager.IsInRoleAsync(entityUser, "Owner"))
                {
                    u.Role = UserRole.Owner;
                    u.CanCurrentAdminManageRole = false;
                }
                else if (await userManager.IsInRoleAsync(entityUser, "Admin"))
                {
                    u.Role = UserRole.Admin;
                    u.PromotedByAdminUserId = entityUser.PromotedByAdminUserId;

                    if (entityUser.PromotedByAdminUserId.HasValue)
                    {
                        var promoter = await userManager.FindByIdAsync(
                            entityUser.PromotedByAdminUserId.Value.ToString());

                        u.PromotedByAdminName = promoter?.Name ?? "Administrator";
                    }

                    // Admin cannot manage themselves or the Admin who promoted them.
                    // Owner can override the hierarchy.
                    var isPromoter =
                        currentAdmin != null &&
                        currentAdmin.PromotedByAdminUserId == entityUser.Id;

                    u.CanCurrentAdminManageRole =
                        u.Id != currentAdminId &&
                        (isCurrentAdminOwner || !isPromoter);
                }
                else
                {
                    u.Role = UserRole.User;
                    u.CanCurrentAdminManageRole = true;
                }
            }

            var viewModel = new DashboardUsersViewModel
            {
                SearchTerm = search,
                StatusFilter = status,
                CurrentPage = page,
                PageSize = 20,
                TotalCount = pagedResult.Value?.TotalCount ?? 0,
                Users = userList
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> BlockUser([FromBody] IdWrapperUserId model)
        {
            if (model == null || model.userId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid user ID." });
            var result = await _userService.BlockUserAccountAsync(model.userId);

            await LogAdminActionAsync("BlockUser", "User", model.userId, "User account blocked");
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUser([FromBody] IdWrapperUserId model)
        {
            if (model == null || model.userId == Guid.Empty)
                         return Json(new { success = false, error = "Invalid user ID." });

            var result = await _userService.UnblockUserAccountAsync(model.userId);
            await LogAdminActionAsync("UnblockUser", "User", model.userId, "User account unblocked");
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] IdWrapperUserId model)
        {
            if (model == null || model.userId == Guid.Empty)
                        return Json(new { success = false, error = "Invalid user ID." });
            var result = await _userService.DeleteUserAsync(model.userId);
            await LogAdminActionAsync("DeleteUser", "User", model.userId, "User account deleted");

            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        // ==================== Posts Management ====================

        [HttpGet]
        public async Task<IActionResult> Posts(string search = "", string source = "all", int page = 1)
        {
            var pagedResult = await _postService.GetPostsAdminPagedAsync(search, source, page, 20);

            var viewModel = new DashboardPostsViewModel
            {
                SearchTerm = search,
                SourceFilter = source,
                CurrentPage = page,
                PageSize = 20,
                TotalCount = pagedResult.Value?.TotalCount ?? 0,
                Posts = pagedResult.Value?.Items?.ToList() ?? new List<PostResponseDto>()
            };

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
            var postResult = await _postService.GetPostByIdAsync(model.postId, GetCurrentUserId(), isAdmin: true);
            if (postResult.IsFailure)
                return Json(new { success = false, error = postResult.Error });

            var postOwnerId = postResult.Value.UserId;
            var postTitle = postResult.Value.Title;

            var result = await _postService.DeletePostAsync(model.postId, GetCurrentUserId(), isAdmin: true, reason: model.reason);

            if (result.IsSuccess && postOwnerId != GetCurrentUserId())
            {
                await _notificationService.CreateNotificationAsync(
                receiverId: postOwnerId,
                message: $"Your post \"{postTitle}\" was removed by an administrator. Reason: {model.reason.Trim()}",
                type: NotificationType.SystemAlert,
                senderId: GetCurrentUserId());
            }
            await LogAdminActionAsync("DeletePost", "Post", model.postId, $"Reason: {model.reason}");

            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> HidePost([FromBody] IdWrapperPostId model)
        {
            if (model == null || model.postId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid post ID." });            
            var result = await _postService.HidePostAsync(model.postId, GetCurrentUserId());
            await LogAdminActionAsync("HidePost", "Post", model.postId, "Post hidden from feed");

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
            var pagedResult = await _reportingService.GetReportsPagedAsync(status, page, 20);

            var viewModel = new DashboardReportsViewModel
            {
                StatusFilter = status,
                CurrentPage = page,
                PageSize = 20,
                TotalCount = pagedResult.Value?.TotalCount ?? 0,
                Reports = pagedResult.Value?.Items?.ToList() ?? new List<PostReportResponseDto>()
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> ResolveReport([FromBody] IdWrapperReportId model)
        {
            if (model == null || model.reportId == Guid.Empty)
                    return Json(new { success = false, error = "Invalid report ID." });
            var result = await _reportingService.ResolveReportAsync(model.reportId);
            if (result.IsSuccess)
            {
                await LogAdminActionAsync("ResolveReport", "PostReport", model.reportId, "Report resolved and closed");
            }
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> DismissReport([FromBody] IdWrapperReportId model)
        {
            if (model == null || model.reportId == Guid.Empty)
                return Json(new { success = false, error = "Invalid report ID." });
            var result = await _reportingService.DismissReportAsync(model.reportId);
            if (result.IsSuccess)
            {
                await LogAdminActionAsync("DismissReport", "PostReport", model.reportId, "Report dismissed without action");
            }
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
            if (result.IsSuccess && result.Value != null)
            {
                var friendIds = await _friendshipService.GetFriendsListAsync(userId);
                var userPosts = await _postService.GetUserPostsAsync(userId, GetCurrentUserId());

                result.Value.FriendsCount = friendIds.Value?.Count() ?? 0;
                result.Value.PostsCount = userPosts.Value?.Count() ?? 0;

                return PartialView("Partials/_UserDetails", result.Value);
            }
            return Content("User not found");
        }

        [HttpGet]
        public async Task<IActionResult> GetPostDetails(Guid postId)
        {
            var result = await _postService.GetPostByIdAsync(postId, GetCurrentUserId(), isAdmin: true);
            if (result.IsSuccess)
            {
                return PartialView("Partials/_PostDetails", result.Value);
            }
            return Content("Post not found");
        }

        [HttpGet]
        public async Task<IActionResult> StoryMedia(Guid id)
        {
            var path = await _storyService.GetStoryStoragePathAsync(id);
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                // Fallback: check if story has external or direct WebRoot path
                var storyResult = await _storyService.GetStoryByIdAsync(id, GetCurrentUserId());
                if (storyResult.IsSuccess && !string.IsNullOrEmpty(storyResult.Value?.MediaUrl))
                {
                    var webRootPath = Path.Combine(HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath,
                        storyResult.Value.MediaUrl.TrimStart('/'));
                    if (System.IO.File.Exists(webRootPath)) path = webRootPath;
                }
            }

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return NotFound();

            var ext = Path.GetExtension(path).ToLowerInvariant();
            var contentType = ext switch
            {
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".webp" => "image/webp",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            return PhysicalFile(path, contentType);
        }

        [HttpGet]
        public async Task<IActionResult> DeletedPages(string search = "", int page = 1)
        {
            var viewModel = new DashboardDeletedPagesViewModel
            {
                SearchTerm = search,
                CurrentPage = page,
                PageSize = 20
            };

            var deletedPagesResult = await _pageService.GetDeletedPagesAsync();
            if (deletedPagesResult.IsSuccess)
            {
                var query = deletedPagesResult.Value.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p =>
                        p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        p.DeletionReason.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        p.OwnerName.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                viewModel.TotalCount = query.Count();
                viewModel.DeletedPages = query
                    .Skip((page - 1) * viewModel.PageSize)
                    .Take(viewModel.PageSize)
                    .ToList();
            }

            return View(viewModel);
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

        [HttpGet]
        public async Task<IActionResult> Groups(string search = "", int page = 1)
        {
            var pagedResult = await _groupService.GetGroupsPagedAsync(search, page, 20, GetCurrentUserId());

            var viewModel = new DashboardGroupsViewModel
            {
                SearchTerm = search,
                CurrentPage = page,
                PageSize = 20,
                TotalCount = pagedResult.Value?.TotalCount ?? 0,
                Groups = pagedResult.Value?.Items?.ToList() ?? new List<GroupResponseDto>()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup([FromBody] AdminDeleteGroupModel model)
        {
            if (model == null || model.groupId == Guid.Empty)
                return Json(new { success = false, error = "Invalid group ID." });

            if (string.IsNullOrWhiteSpace(model.reason))
                return Json(new { success = false, error = "A deletion reason is required." });

            var result = await _groupService.DeleteGroupAsync(model.groupId, GetCurrentUserId(), model.reason, isAdmin: true);
            await LogAdminActionAsync("DeleteGroup", "Group", model.groupId, $"Reason: {model.reason}");

            return Json(new { success = result.IsSuccess, error = result.Error });
        }
        [HttpGet]
        public async Task<IActionResult> Pages(string search = "", int page = 1)
        {
            var pagesResult = await _pageService.GetAllPagesAsync();
            var query = (pagesResult.Value ?? new List<PageResponseDto>()).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                         (p.Description != null && p.Description.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            var totalCount = query.Count();
            var items = query.Skip((page - 1) * 20).Take(20).ToList();

            var viewModel = new DashboardPagesViewModel
            {
                SearchTerm = search,
                CurrentPage = page,
                PageSize = 20,
                TotalCount = totalCount,
                Pages = items
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePage([FromBody] AdminDeletePageModel model)
        {
            if (model == null || model.pageId == Guid.Empty)
                return Json(new { success = false, error = "Invalid page ID." });

            if (string.IsNullOrWhiteSpace(model.reason))
                return Json(new { success = false, error = "A deletion reason is required." });

            var result = await _pageService.DeletePageAsync(GetCurrentUserId(), model.pageId, model.reason);
            await LogAdminActionAsync("DeletePage", "Page", model.pageId, $"Reason: {model.reason}");

            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        [HttpGet]
        public async Task<IActionResult> Stories(int page = 1)
        {
            var (stories, totalCount) = await _unitOfWork.Stories.GetStoriesAdminPagedAsync(page, 20);

            var dtos = stories.Select(s =>
            {
                string? resolvedMediaUrl = s.MediaUrl;
                if (!string.IsNullOrEmpty(s.MediaUrl))
                {
                    if (s.MediaUrl.Contains("ProtectedUploads", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedMediaUrl = Url.Action("StoryMedia", "Dashboard", new { id = s.Id });
                    }
                    else if (!s.MediaUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !s.MediaUrl.StartsWith("/"))
                    {
                        resolvedMediaUrl = "/" + s.MediaUrl;
                    }
                }

                return new StoryResponseDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserName = s.User?.Name ?? "User",
                    UserProfilePicture = s.User?.ProfilePictureUrl,
                    MediaUrl = resolvedMediaUrl,
                    MediaType = s.MediaType,
                    Content = s.Content,
                    CreatedAt = s.CreatedAt,
                    ExpiresAt = s.ExpiresAt,
                    Privacy = s.Privacy.ToString()
                };
            }).ToList();

            var viewModel = new DashboardStoriesViewModel
            {
                CurrentPage = page,
                PageSize = 20,
                TotalCount = totalCount,
                Stories = dtos
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStory([FromBody] IdWrapperStoryId model)
        {
            if (model == null || model.storyId == Guid.Empty)
                return Json(new { success = false, error = "Invalid story ID." });

            var result = await _storyService.DeleteStoryAsync(model.storyId, GetCurrentUserId(), isAdmin: true);
            if (result.IsSuccess)
            {
                await LogAdminActionAsync("DeleteStory", "Story", model.storyId, "Story permanently removed by administrator");
            }
            return Json(new { success = result.IsSuccess, error = result.Error });
        }


        [HttpGet]
        public async Task<IActionResult> AuditLogs(string actionFilter = "all", int page = 1)
        {
            var dbContext = HttpContext.RequestServices.GetRequiredService<Sohba.Infrastructure.Data.AppDbContext>();
            var query = dbContext.AdminAuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(actionFilter) && !actionFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.Action == actionFilter);
            }

            var totalCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(query);
            var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                query.OrderByDescending(l => l.Timestamp).Skip((page - 1) * 25).Take(25));

            var viewModel = new DashboardAuditLogsViewModel
            {
                ActionFilter = actionFilter,
                CurrentPage = page,
                PageSize = 25,
                TotalCount = totalCount,
                Logs = items
            };

            return View(viewModel);
        }

        private async Task LogAdminActionAsync(string action, string targetEntity, Guid targetId, string? details)
        {
            try
            {
                var dbContext = HttpContext.RequestServices.GetRequiredService<Sohba.Infrastructure.Data.AppDbContext>();
                dbContext.AdminAuditLogs.Add(new AdminAuditLog
                {
                    AdminId = GetCurrentUserId(),
                    AdminEmail = User.Identity?.Name ?? "Admin",
                    Action = action,
                    TargetEntity = targetEntity,
                    TargetId = targetId,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to record audit log for action {Action} on {TargetEntity} {TargetId}", action, targetEntity, targetId);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Comments(string search = "", int page = 1)
        {
            var (comments, totalCount) = await _unitOfWork.Interactions.GetCommentsAdminPagedAsync(search, page, 25);

            var dtos = comments.Select(c => new CommentResponseDto
            {
                Id = c.Id,
                PostId = c.PostId,
                UserId = c.UserId,
                UserName = c.User?.Name ?? "User",
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                Depth = c.Depth
            }).ToList();

            var viewModel = new DashboardCommentsViewModel
            {
                SearchTerm = search,
                CurrentPage = page,
                PageSize = 25,
                TotalCount = totalCount,
                Comments = dtos
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment([FromBody] IdWrapperCommentId model)
        {
            if (model == null || model.commentId == Guid.Empty)
                return Json(new { success = false, error = "Invalid comment ID." });

            var result = await _interactionService.DeleteCommentAsync(GetCurrentUserId(), model.commentId, isAdmin: true);
            if (result.IsSuccess)
            {
                await LogAdminActionAsync("DeleteComment", "Comment", model.commentId, "Comment removed by admin");
            }
            return Json(new { success = result.IsSuccess, error = result.Error });
        }

        

        [HttpPost]
        public async Task<IActionResult> ToggleUserAdminRole([FromBody] AdminRoleToggleModel model)
        {
            if (model == null || model.userId == Guid.Empty)
                return Json(new { success = false, error = "Invalid user ID." });

            var currentAdminId = GetCurrentUserId();
            var userManager = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();
            var targetUser = await userManager.FindByIdAsync(model.userId.ToString());
            if (targetUser == null)
                return Json(new { success = false, error = "Target user not found." });

            var isCurrentlyAdmin = await userManager.IsInRoleAsync(targetUser, "Admin");

            if (isCurrentlyAdmin)
            {
                var demoteResult = await _userService.DemoteAdminToUserAsync(model.userId, currentAdminId);
                if (!demoteResult.IsSuccess)
                    return Json(new { success = false, error = demoteResult.Error });

                await LogAdminActionAsync("RevokeAdminRole", "User", targetUser.Id, $"Revoked Admin role from {targetUser.Name}");
                return Json(new { success = true, newRole = "User", message = $"{targetUser.Name} is now a standard User." });
            }
            else
            {
                var promoteResult = await _userService.PromoteUserToAdminAsync(model.userId, currentAdminId);
                if (!promoteResult.IsSuccess)
                    return Json(new { success = false, error = promoteResult.Error });

                await LogAdminActionAsync("AssignAdminRole", "User", targetUser.Id, $"Promoted {targetUser.Name} to Admin");
                return Json(new { success = true, newRole = "Admin", message = $"{targetUser.Name} promoted to Administrator." });
            }
        }

        public class AdminRoleToggleModel { public Guid userId { get; set; } }
        public class IdWrapperCommentId { public Guid commentId { get; set; } }


        public class IdWrapperStoryId { public Guid storyId { get; set; } }

        public class AdminDeletePageModel { public Guid pageId { get; set; } public string reason { get; set; } }

        public class AdminDeleteGroupModel { public Guid groupId { get; set; } public string reason { get; set; } }


        public class IdWrapperUserId { public Guid userId { get; set; } }
        public class IdWrapperPostId { public Guid postId { get; set; } }
        public class AdminDeletePostModel { public Guid postId { get; set; } public string reason { get; set; } }
        public class IdWrapperReportId { public Guid reportId { get; set; } }
        public class DeleteReportedPostModel { public Guid postId { get; set; } public Guid reportId { get; set; } }
    }
}