using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Enums;
using Sohba.ViewModels.Group;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sohba.Controllers
{
    [Authorize]
    [EnableRateLimiting("Api")]
    public class GroupsController : BaseController
    {
        private readonly IGroupService _groupService;
        private readonly IPostService _postService;
        private readonly IFileStorageService _fileStorage;

    public GroupsController(
        IGroupService groupService,
        IPostService postService,
        IFileStorageService fileStorage)
        {
            _groupService = groupService;
            _postService = postService;
            _fileStorage = fileStorage;
        }

        [HttpGet]
        public async Task<IActionResult> Discover()
        {
            var userId = GetCurrentUserId();

            var result = await _groupService.GetGroupsPagedAsync(
                null,
                1,
                5,
                userId != Guid.Empty ? userId : null);

            if (!result.IsSuccess || result.Value == null)
                return Json(Array.Empty<object>());

            var groupsToJoin = result.Value.Items
                .Where(g => !g.IsCurrentUserMember)
                .OrderByDescending(g => g.MembersCount)
                .Take(5)
                .ToList();

            return Json(groupsToJoin);
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string search = "",
            int page = 1,
            int pageSize = 12)
        {
            var userId = GetCurrentUserId();

            var result = await _groupService.GetGroupsPagedAsync(
                search,
                page,
                pageSize,
                userId != Guid.Empty ? userId : null);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Error ?? "Unable to load groups.");

                return View(new GroupIndexViewModel
                {
                    Groups = new PagedResult<GroupResponseDto>(),
                    SearchTerm = search ?? string.Empty
                });
            }

            var viewModel = new GroupIndexViewModel
            {
                Groups = result.Value ?? new PagedResult<GroupResponseDto>(),
                SearchTerm = search ?? string.Empty
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            GroupCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            string? imageUrl = null;

            if (model.ImageFile != null)
            {
                var uploadResult =
                    await _fileStorage.SaveFileAsync(
                        model.ImageFile,
                        "groups");

                if (!uploadResult.IsSuccess)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        uploadResult.Error);

                    return View(model);
                }

                imageUrl = uploadResult.Value;
            }

            string? backgroundImageUrl = null;

            if (model.BackgroundImageFile != null)
            {
                var bgUploadResult =
                    await _fileStorage.SaveFileAsync(
                        model.BackgroundImageFile,
                        "groups");

                if (!bgUploadResult.IsSuccess)
                {
                    ModelState.AddModelError(
                        "BackgroundImageFile",
                        bgUploadResult.Error);

                    return View(model);
                }

                backgroundImageUrl = bgUploadResult.Value;
            }

            var dto = new GroupCreateDto
            {
                Name = model.Name,
                Description = model.Description,
                Rules = model.Rules,
                ImageUrl = imageUrl,
                BackgroundImageUrl = backgroundImageUrl,
                IsPrivate = model.IsPrivate
            };

            var result =
                await _groupService.CreateGroupAsync(
                    dto,
                    userId);

            if (result.IsSuccess && result.Value != null)
            {
                return RedirectToAction(
                    "Details",
                    new { id = result.Value.Id });
            }

            ModelState.AddModelError(
                string.Empty,
                result.Error ?? "Unable to create group.");

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUserId = GetCurrentUserId();

            var groupResult =
                await _groupService.GetGroupByIdAsync(
                    id,
                    currentUserId);

            if (groupResult.IsFailure ||
                groupResult.Value == null)
            {
                return NotFound();
            }

            var userRole =
                await _groupService.GetUserRoleInGroupAsync(
                    id,
                    currentUserId);

            var isOwner =
                groupResult.Value.AdminId == currentUserId;

            var canManage =
                isOwner ||
                userRole == GroupRole.Admin ||
                User.IsInRole("Admin");

            var pendingCount = 0;

            if (canManage &&
                groupResult.Value.IsPrivate)
            {
                var countResult =
                    await _groupService
                        .GetPendingJoinRequestsCountAsync(
                            id,
                            currentUserId);

                if (countResult.IsSuccess)
                    pendingCount = countResult.Value;
            }

            var viewModel = new GroupDetailsViewModel
            {
                Group = groupResult.Value,
                UserJoinRequestStatus =
                    groupResult.Value.UserJoinRequestStatus,
                CanManageRequests = canManage,
                PendingRequestsCount = pendingCount
            };

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.GroupAdminId =
                groupResult.Value.AdminId;
            ViewBag.CurrentUserRole = userRole;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetCurrentUserId();

            var groupResult =
                await _groupService.GetGroupByIdAsync(
                    id,
                    userId);

            if (groupResult.IsFailure ||
                groupResult.Value == null)
            {
                return NotFound();
            }

            if (groupResult.Value.AdminId != userId &&
                !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var viewModel = new GroupEditViewModel
            {
                Id = groupResult.Value.Id,
                Name = groupResult.Value.Name,
                Description = groupResult.Value.Description,
                Rules = groupResult.Value.Rules,
                ImageUrl = groupResult.Value.ImageUrl,
                BackgroundImageUrl =
                    groupResult.Value.BackgroundImageUrl,
                IsPrivate = groupResult.Value.IsPrivate
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            GroupEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            string? imageUrl = model.ImageUrl;

            if (model.ImageFile != null)
            {
                var uploadResult =
                    await _fileStorage.SaveFileAsync(
                        model.ImageFile,
                        "groups");

                if (!uploadResult.IsSuccess)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        uploadResult.Error);

                    return View(model);
                }

                imageUrl = uploadResult.Value;
            }

            string? backgroundImageUrl =
                model.BackgroundImageUrl;

            if (model.BackgroundImageFile != null)
            {
                var bgUploadResult =
                    await _fileStorage.SaveFileAsync(
                        model.BackgroundImageFile,
                        "groups");

                if (!bgUploadResult.IsSuccess)
                {
                    ModelState.AddModelError(
                        "BackgroundImageFile",
                        bgUploadResult.Error);

                    return View(model);
                }

                backgroundImageUrl =
                    bgUploadResult.Value;
            }

            var updateDto = new GroupUpdateDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Rules = model.Rules,
                ImageUrl = imageUrl,
                BackgroundImageUrl = backgroundImageUrl,
                IsPrivate = model.IsPrivate
            };

            var result =
                await _groupService.UpdateGroupAsync(
                    updateDto,
                    userId);

            if (result.IsSuccess)
            {
                return RedirectToAction(
                    "Details",
                    new { id = model.Id });
            }

            ModelState.AddModelError(
                string.Empty,
                result.Error ?? "Unable to update group.");

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupPosts(
            Guid groupId)
        {
            var userId = GetCurrentUserId();

            var groupCheck =
                await _groupService.GetGroupByIdAsync(
                    groupId,
                    userId);

            if (groupCheck.IsFailure ||
                groupCheck.Value == null)
            {
                return NotFound();
            }

            // Privacy barrier:
            // non-members cannot view private group posts.
            if (groupCheck.Value.IsPrivate &&
                !groupCheck.Value.IsCurrentUserMember)
            {
                return Content(@"
                <div class='bg-white rounded-2xl p-8 sm:p-12 text-center border border-slate-100 shadow-sm'>
                    <div class='w-16 h-16 bg-amber-50 text-amber-600 rounded-2xl flex items-center justify-center mx-auto mb-4 border border-amber-100'>
                        <svg class='w-8 h-8' fill='none' viewBox='0 0 24 24' stroke='currentColor'>
                            <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2'
                                  d='M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z' />
                        </svg>
                    </div>
                    <h3 class='text-xl font-bold text-gray-900 mb-2'>This Group is Private</h3>
                    <p class='text-gray-500 text-sm max-w-md mx-auto mb-6'>
                        Join this community to see posts, discussions, and participate in conversations.
                    </p>
                </div>",
                    "text/html");
            }

            var postsResult =
                await _postService.GetGroupPostsAsync(
                    groupId,
                    userId);

            if (postsResult.IsSuccess &&
                postsResult.Value != null &&
                postsResult.Value.Any())
            {
                return PartialView(
                    "Partials/_PostCard",
                    postsResult.Value);
            }

            return Content(@"
            <div class='bg-white rounded-2xl p-10 text-center border border-slate-100 shadow-sm'>
                <div class='w-14 h-14 bg-slate-100 text-slate-400 rounded-2xl flex items-center justify-center mx-auto mb-3'>
                    <svg class='w-7 h-7' fill='none' viewBox='0 0 24 24' stroke='currentColor'>
                        <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2'
                              d='M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10' />
                    </svg>
                </div>
                <h3 class='font-bold text-gray-800 text-base'>No posts yet</h3>
                <p class='text-xs text-gray-400 mt-1'>
                    Be the first to share something with this group!
                </p>
            </div>",
                "text/html");
        }

        [HttpGet]
        public async Task<IActionResult> GetMembersPaged(
            Guid groupId,
            string search = "",
            int page = 1,
            int pageSize = 12)
        {
            var userId = GetCurrentUserId();

            var groupCheck =
                await _groupService.GetGroupByIdAsync(
                    groupId,
                    userId);

            if (groupCheck.IsFailure ||
                groupCheck.Value == null)
            {
                return Content(
                    "<div class='text-center py-10 text-red-500'>Group not found</div>",
                    "text/html");
            }

            // Privacy barrier for private groups.
            if (groupCheck.Value.IsPrivate &&
                !groupCheck.Value.IsCurrentUserMember)
            {
                return Content(@"
                <div class='bg-white rounded-2xl p-8 text-center border border-slate-100 shadow-sm'>
                    <div class='w-14 h-14 bg-slate-100 text-slate-500 rounded-2xl flex items-center justify-center mx-auto mb-3'>
                        <svg class='w-7 h-7' fill='none' viewBox='0 0 24 24' stroke='currentColor'>
                            <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2'
                                  d='M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z' />
                        </svg>
                    </div>
                    <h3 class='text-lg font-bold text-gray-900 mb-1'>Members List is Private</h3>
                    <p class='text-gray-500 text-sm'>
                        Join this group to see the full list of members.
                    </p>
                </div>",
                    "text/html");
            }

            var pagedResult =
                await _groupService.GetMembersPagedAsync(
                    groupId,
                    search,
                    page,
                    pageSize,
                    groupCheck.Value.AdminId);

            if (pagedResult.IsFailure ||
                pagedResult.Value == null)
            {
                return Content(
                    $"<div class='text-center py-10 text-red-500'>{pagedResult.Error ?? "Unable to load members."}</div>",
                    "text/html");
            }

            ViewBag.GroupId = groupId;
            ViewBag.CurrentUserId = userId;
            ViewBag.GroupAdminId =
                groupCheck.Value.AdminId;

            ViewBag.CurrentUserRole =
                await _groupService.GetUserRoleInGroupAsync(
                    groupId,
                    userId);

            ViewBag.SearchTerm =
                search ?? string.Empty;

            return PartialView(
                "_MembersTab",
                pagedResult.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetAboutTab(
            Guid groupId)
        {
            var userId = GetCurrentUserId();

            var groupResult =
                await _groupService.GetGroupByIdAsync(
                    groupId,
                    userId);

            if (groupResult.IsFailure ||
                groupResult.Value == null)
            {
                return Content(
                    "<div class='text-center py-10 text-red-500'>Group not found</div>",
                    "text/html");
            }

            var isMember =
                groupResult.Value.IsCurrentUserMember;

            IEnumerable<GroupMemberDto> membersResult =
                Array.Empty<GroupMemberDto>();

            if (!groupResult.Value.IsPrivate || isMember)
            {
                var membersResponse =
                    await _groupService
                        .GetGroupMembersAsync(groupId);

                if (membersResponse.IsSuccess &&
                    membersResponse.Value != null)
                {
                    membersResult =
                        membersResponse.Value;
                }
            }

            var postsCount = 0;

            if (!groupResult.Value.IsPrivate || isMember)
            {
                var postsResult =
                    await _postService.GetGroupPostsAsync(
                        groupId,
                        userId);

                if (postsResult.IsSuccess &&
                    postsResult.Value != null)
                {
                    postsCount =
                        postsResult.Value.Count();
                }
            }

            ViewBag.GroupId = groupId;

            return PartialView(
                "_AboutTab",
                new
                {
                    Group = groupResult.Value,
                    Members = membersResult,
                    PostsCount = postsCount
                });
        }

        // ==================== Membership & Roles ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(
            [FromBody] IdRequestDto request)
        {
            var userId = GetCurrentUserId();

            if (request == null ||
                request.Id == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    error = "Invalid group ID."
                });
            }

            var result =
                await _groupService.JoinGroupAsync(
                    request.Id,
                    userId);

            return Json(new
            {
                success = result.IsSuccess,
                error = result.Error
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(
            [FromBody] LeaveGroupRequest request)
        {
            var userId = GetCurrentUserId();

            if (userId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    error = "User not authenticated."
                });
            }

            if (request == null ||
                request.GroupId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    error = "Invalid group ID."
                });
            }

            var result =
                await _groupService.LeaveGroupAsync(
                    request.GroupId,
                    userId);

            return Json(new
            {
                success = result.IsSuccess,
                error = result.Error
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteMember(
            [FromBody] GroupMemberActionRequest request)
        {
            var adminId = GetCurrentUserId();

            if (request == null ||
                request.GroupId == Guid.Empty ||
                request.TargetUserId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    error = "Invalid request."
                });
            }

            var result =
                await _groupService.PromoteMemberAsync(
                    request.GroupId,
                    request.TargetUserId,
                    adminId);

            return Json(new
            {
                success = result.IsSuccess,
                error = result.Error
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteMember(
            [FromBody] GroupMemberActionRequest request)
        {
            var adminId = GetCurrentUserId();

            if (request == null ||
                request.GroupId == Guid.Empty ||
                request.TargetUserId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    error = "Invalid request."
                });
            }

            var result =
                await _groupService.DemoteMemberAsync(
                    request.GroupId,
                    request.TargetUserId,
                    adminId);

            return Json(new
            {
                success = result.IsSuccess,
                error = result.Error
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KickMember(
            [FromBody] GroupMemberActionRequest request)
        {
            var adminId = GetCurrentUserId();

            if (request == null ||
                request.GroupId == Guid.Empty ||
                request.TargetUserId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    error = "Invalid request."
                });
            }

            var result =
                await _groupService.KickMemberAsync(
                    request.GroupId,
                    request.TargetUserId,
                    adminId);

            return Json(new
            {
                success = result.IsSuccess,
                error = result.Error
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            [FromBody] DeleteGroupRequest request)
        {
            var userId = GetCurrentUserId();

            if (request == null ||
                request.GroupId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    error = "Invalid group ID."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Json(new
                {
                    success = false,
                    error = "A deletion reason is required."
                });
            }

            var isAdmin =
                User.IsInRole("Admin");

            var result =
                await _groupService.DeleteGroupAsync(
                    request.GroupId,
                    userId,
                    request.Reason,
                    isAdmin);

            return Json(new
            {
                success = result.IsSuccess,
                error = result.Error
            });
        }

        // ==================== Join Requests ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitJoinRequest(
            [FromBody] SubmitJoinRequestDto dto)
        {
            var userId = GetCurrentUserId();

            if (dto == null ||
                dto.GroupId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    error = "Invalid request parameters."
                });
            }

            var result =
                await _groupService
                    .SubmitJoinRequestAsync(
                        userId,
                        dto);

            return Json(new
            {
                success = result.IsSuccess,
                error = result.Error
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetJoinRequests(
            Guid groupId,
            int page = 1,
            int pageSize = 10)
        {
            var userId = GetCurrentUserId();

            var result =
                await _groupService
                    .GetPendingJoinRequestsAsync(
                        groupId,
                        userId,
                        page,
                        pageSize);

            if (!result.IsSuccess ||
                result.Value == null)
            {
                return Content(
                    $"<div class='text-center py-10 text-red-500'>{result.Error ?? "Unable to load join requests."}</div>",
                    "text/html");
            }

            ViewBag.GroupId = groupId;

            return PartialView(
                "_JoinRequestsTab",
                result.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewJoinRequest(
            [FromBody] ReviewJoinRequestDto dto)
        {
            var userId = GetCurrentUserId();

            if (dto == null ||
                dto.RequestId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    error = "Invalid request."
                });
            }

            var result =
                await _groupService
                    .ReviewJoinRequestAsync(
                        userId,
                        dto);

            return Json(new
            {
                success = result.IsSuccess,
                error = result.Error
            });
        }

        public class DeleteGroupRequest
        {
            public Guid GroupId { get; set; }

            public string Reason { get; set; } =
                string.Empty;
        }

        public class GroupMemberActionRequest
        {
            public Guid GroupId { get; set; }

            public Guid TargetUserId { get; set; }
        }

        public class LeaveGroupRequest
        {
            public Guid GroupId { get; set; }
        }
    }

}
