using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sohba.Application.Services
{
    public class PageService : IPageService
    {
        private const int MaxAdminsPerPage = 10;

        private readonly IPageDomainService _pageDomainService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly ILogger<PageService> _logger;
        private readonly IFileStorageService _fileStorage;
        public PageService(
            IPageDomainService pageDomainService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IUserService userService,
            INotificationService notificationService,
            ILogger<PageService> logger,
            IFileStorageService fileStorage)
        {
            _pageDomainService = pageDomainService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userService = userService;
            _notificationService = notificationService;
            _logger = logger;
            _fileStorage = fileStorage;
        }

        public async Task<Result<PageResponseDto>> CreatePageAsync(Guid adminId, PageCreateDto dto)
        {
            if (dto == null)
                return Result<PageResponseDto>.Failure("Page data is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result<PageResponseDto>.Failure("Page name is required.");

            var domainDecision = _pageDomainService.CanCreatePage(dto.Name);
            if (domainDecision.IsFailure)
                return Result<PageResponseDto>.Failure(domainDecision.Error);

            if (await _unitOfWork.Pages.ExistsByNameAsync(dto.Name))
                return Result<PageResponseDto>.Failure("A page with this name already exists.");

            var page = new Page
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                BackgroundImageUrl = dto.BackgroundImageUrl,
                Rules = dto.Rules,
                IsPrivate = dto.IsPrivate,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Pages.Add(page);

            var follower = new PageFollower
            {
                UserId = adminId,
                PageId = page.Id,
                FollowedAt = DateTime.UtcNow,
                Role = PageRole.PageOwner
            };
            _unitOfWork.Pages.AddFollower(follower);
            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<PageResponseDto>(page);
            return Result<PageResponseDto>.Success(response);
        }

        public async Task<Result> FollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null)
                return Result.Failure("Page not found.");

            if (page.IsPrivate)
                return Result.Failure("This page is private. You must submit a follow request.");

            if (await _unitOfWork.Pages.IsFollowingAsync(userId, pageId))
                return Result.Failure("You are already following this page.");

            var domainDecision = _pageDomainService.CanFollowPage(userId, page, false);
            if (domainDecision.IsFailure)
                return domainDecision;

            var follower = new PageFollower
            {
                UserId = userId,
                PageId = pageId,
                FollowedAt = DateTime.UtcNow,
                Role = PageRole.Member
            };

            _unitOfWork.Pages.AddFollower(follower);

            try
            {
                await _unitOfWork.CompleteAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Concurrent insert from another request — treat as already following.
                return Result.Failure("You are already following this page.");
            }

            if (page.AdminId != userId)
            {
                var user = await _userService.GetProfileAsync(userId);
                var userName = user.Value?.Name ?? "Someone";

                await _notificationService.CreateNotificationAsync(
                    receiverId: page.AdminId,
                    message: $"{userName} followed your page '{page.Name}'",
                    type: NotificationType.PageFollow,
                    senderId: userId,
                    targetId: pageId
                );
            }

            return Result.Success();
        }

        public async Task<Result> UnfollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null)
                return Result.Failure("Page not found.");

            if (!await _unitOfWork.Pages.IsFollowingAsync(userId, pageId))
                return Result.Failure("You cannot unfollow a page you don't follow.");

            var domainDecision = _pageDomainService.CanUnfollowPage(true);
            if (domainDecision.IsFailure)
                return domainDecision;

            _unitOfWork.Pages.RemoveFollower(userId, pageId);
            await _unitOfWork.CompleteAsync();

            if (page.AdminId != userId)
            {
                var user = await _userService.GetProfileAsync(userId);
                await _notificationService.CreateNotificationAsync(
                    receiverId: page.AdminId,
                    message: $"{user.Value?.Name} unfollowed your page '{page.Name}'",
                    type: NotificationType.PageFollow,
                    senderId: userId,
                    targetId: pageId
                );
            }

            return Result.Success();
        }

        public async Task<Result<PageResponseDto>> GetPageByIdAsync(Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null)
                return Result<PageResponseDto>.Failure("Page not found");

            var dto = _mapper.Map<PageResponseDto>(page);
            return Result<PageResponseDto>.Success(dto);
        }

        public async Task<Result<IEnumerable<PageResponseDto>>> GetUserFollowedPagesAsync(Guid userId)
        {
            var pages = await _unitOfWork.Pages.GetPagesByFollowerIdAsync(userId);
            var dtos = _mapper.Map<IEnumerable<PageResponseDto>>(pages);
            return Result<IEnumerable<PageResponseDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<PageResponseDto>>> GetAllPagesAsync()
        {
            var pages = await _unitOfWork.Pages.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<PageResponseDto>>(pages);
            return Result<IEnumerable<PageResponseDto>>.Success(dtos);
        }

        public async Task<Result> DeletePageAsync(Guid adminId, Guid pageId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure("A deletion reason is required.");

            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null) return Result.Failure("Page not found");

            var actorRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(adminId, pageId);
            var validation = _pageDomainService.CanDeletePage(adminId, actorRole);
            if (!validation.IsSuccess) return validation;

            _logger.LogInformation("Page {PageId} ('{Name}') deleted by PageOwner {UserId}. Reason: {Reason}",
                pageId, page.Name, adminId, reason);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var pagePosts = await _unitOfWork.Posts.GetPagePostsAsync(pageId);
                foreach (var post in pagePosts)
                {
                    post.IsDeleted = true;
                    post.PageId = null;
                    _unitOfWork.Posts.Update(post);
                }
                await _unitOfWork.CompleteAsync();

                var oldImage = page.ImageUrl;
                var oldBg = page.BackgroundImageUrl;

                _unitOfWork.Pages.Delete(page);
                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();

                // Safe cleanup after transaction commits
                if (!string.IsNullOrEmpty(oldImage))
                    await _fileStorage.DeleteFileAsync(oldImage);
                if (!string.IsNullOrEmpty(oldBg))
                    await _fileStorage.DeleteFileAsync(oldBg);

                return Result.Success();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to delete page {PageId}", pageId);
                return Result.Failure("An error occurred while deleting the page.");
            }
        }

        public async Task<Result<bool>> ToggleFollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null)
                return Result<bool>.Failure("Page not found");

            if (page.AdminId == userId)
                return Result<bool>.Failure("As the page owner, you cannot toggle follow status.");

            var role = await _unitOfWork.Pages.GetUserRoleInPageAsync(userId, pageId);
            if (role == PageRole.CoAdmin || role == PageRole.Admin || role == PageRole.PageOwner)
            {
                return Result<bool>.Failure("Page managers and administrators cannot unfollow via quick toggle. Please use Leave Page.");
            }

            var isFollowing = await IsFollowingAsync(userId, pageId);

            if (isFollowing.Value)
            {
                var unfollowResult = await UnfollowPageAsync(userId, pageId);
                return unfollowResult.IsSuccess
                    ? Result<bool>.Success(false)
                    : Result<bool>.Failure(unfollowResult.Error);
            }
            else
            {
                var followResult = await FollowPageAsync(userId, pageId);
                return followResult.IsSuccess
                    ? Result<bool>.Success(true)
                    : Result<bool>.Failure(followResult.Error);
            }
        }

        public async Task<Result<bool>> IsFollowingAsync(Guid userId, Guid pageId)
        {
            var isFollowing = await _unitOfWork.Pages.IsFollowingAsync(userId, pageId);
            return Result<bool>.Success(isFollowing);
        }

        public async Task<Result<int>> GetFollowersCountAsync(Guid pageId)
        {
            var count = await _unitOfWork.Pages.GetFollowersCountAsync(pageId);
            return Result<int>.Success(count);
        }

        public async Task<Result<IEnumerable<PageFollowerDto>>> GetFollowersAsync(Guid pageId, int page = 1, int pageSize = 20)
        {
            var followers = await _unitOfWork.Pages.GetFollowersAsync(pageId, page, pageSize);
            var dtos = _mapper.Map<IEnumerable<PageFollowerDto>>(followers);
            return Result<IEnumerable<PageFollowerDto>>.Success(dtos);
        }

        public async Task<Result<bool>> IsPageAdminAsync(Guid userId, Guid pageId)
        {
            var role = await _unitOfWork.Pages.GetUserRoleInPageAsync(userId, pageId);
            var isAdmin = role.HasValue && role.Value >= PageRole.Admin;
            return Result<bool>.Success(isAdmin);
        }

        public async Task<PageRole?> GetUserRoleInPageAsync(Guid userId, Guid pageId)
        {
            return await _unitOfWork.Pages.GetUserRoleInPageAsync(userId, pageId);
        }

        public async Task<Result<PageResponseDto>> UpdatePageAsync(PageUpdateDto updateDto, Guid userId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(updateDto.Id);
            if (page == null)
                return Result<PageResponseDto>.Failure("Page not found.");

            var actorRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(userId, updateDto.Id);
            var editDecision = _pageDomainService.CanEditPage(actorRole);
            if (editDecision.IsFailure)
                return Result<PageResponseDto>.Failure(editDecision.Error);

            // Enforce length caps server-side.
            if (string.IsNullOrWhiteSpace(updateDto.Name) || updateDto.Name.Length < 3 || updateDto.Name.Length > 100)
                return Result<PageResponseDto>.Failure("Page name must be between 3 and 100 characters.");

            if (updateDto.Description != null && updateDto.Description.Length > 2000)
                return Result<PageResponseDto>.Failure("Description must be 2000 characters or fewer.");

            // Name uniqueness (case-insensitive) — skip if unchanged.
            if (!string.Equals(page.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase) &&
                await _unitOfWork.Pages.ExistsByNameAsync(updateDto.Name))
            {
                return Result<PageResponseDto>.Failure("A page with this name already exists.");
            }

            page.Name = updateDto.Name;
            page.Description = updateDto.Description;
            page.Rules = updateDto.Rules;
            page.IsPrivate = updateDto.IsPrivate;
            if (!string.IsNullOrEmpty(updateDto.ImageUrl))
                page.ImageUrl = updateDto.ImageUrl;
            if (!string.IsNullOrEmpty(updateDto.BackgroundImageUrl))
                page.BackgroundImageUrl = updateDto.BackgroundImageUrl;

            _unitOfWork.Pages.Update(page);
            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<PageResponseDto>(page);
            return Result<PageResponseDto>.Success(response);
        }

        public async Task<Result<int>> GetPagesCountAsync()
        {
            var count = await _unitOfWork.Pages.CountAsync();
            return Result<int>.Success(count);
        }

        public async Task<Result<bool>> KickPageMemberAsync(Guid pageId, Guid targetUserId, Guid adminId)
        {
            var actorRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(adminId, pageId);
            var targetRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(targetUserId, pageId);

            var validation = _pageDomainService.CanKickPageMember(adminId, actorRole, targetUserId, targetRole);
            if (!validation.IsSuccess) return Result<bool>.Failure(validation.Error);

            _unitOfWork.Pages.RemoveFollower(targetUserId, pageId);
            var affectedRows = await _unitOfWork.CompleteAsync();

            if (affectedRows > 0)
            {
                var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
                var pageName = page?.Name ?? "the page";
                await _notificationService.CreateNotificationAsync(
                    receiverId: targetUserId,
                    message: $"You have been removed from page '{pageName}'.",
                    type: NotificationType.PageFollow,
                    senderId: adminId,
                    targetId: pageId
                );
            }

            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<bool>> PromotePageMemberAsync(Guid pageId, Guid targetUserId, Guid adminId, PageRole newRole)
        {
            if (newRole <= PageRole.Member || newRole > PageRole.Admin)
                return Result<bool>.Failure("Invalid target role for promotion.");

            var actorRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(adminId, pageId);
            var targetRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(targetUserId, pageId);

            var validation = _pageDomainService.CanPromotePageMember(adminId, targetUserId, actorRole, targetRole, newRole);
            if (!validation.IsSuccess) return Result<bool>.Failure(validation.Error);

            // Cap the number of Admins (and PageOwner — but PageOwner is created once).
            if (newRole == PageRole.Admin)
            {
                var adminCount = await _unitOfWork.Pages.GetAdminCountAsync(pageId);
                if (adminCount >= MaxAdminsPerPage)
                    return Result<bool>.Failure($"This page already has the maximum number of admins ({MaxAdminsPerPage}).");
            }

            var follower = await _unitOfWork.Pages.GetFollowerAsync(targetUserId, pageId);
            if (follower == null) return Result<bool>.Failure("Member not found.");

            follower.Role = newRole;
            var affectedRows = await _unitOfWork.CompleteAsync();
            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<bool>> DemotePageMemberAsync(Guid pageId, Guid targetUserId, Guid adminId, PageRole newRole)
        {
            if (newRole < PageRole.Member || newRole >= PageRole.Admin)
                return Result<bool>.Failure("Invalid target role for demotion.");

            var actorRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(adminId, pageId);
            var targetRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(targetUserId, pageId);

            var validation = _pageDomainService.CanDemotePageMember(adminId, targetUserId, actorRole, targetRole, newRole);
            if (!validation.IsSuccess) return Result<bool>.Failure(validation.Error);

            var follower = await _unitOfWork.Pages.GetFollowerAsync(targetUserId, pageId);
            if (follower == null) return Result<bool>.Failure("Member not found.");

            follower.Role = newRole;
            var affectedRows = await _unitOfWork.CompleteAsync();
            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<bool>> TransferOwnershipAsync(Guid pageId, Guid targetUserId, Guid adminId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null) return Result<bool>.Failure("Page not found.");

            var actorRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(adminId, pageId);
            var targetRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(targetUserId, pageId);

            var validation = _pageDomainService.CanTransferOwnership(adminId, actorRole, targetRole);
            if (!validation.IsSuccess) return Result<bool>.Failure(validation.Error);

            // Find the current owner row, demote them to Admin, promote the target to PageOwner, update Page.AdminId.
            var currentOwner = await _unitOfWork.Pages.GetFollowerAsync(adminId, pageId);
            if (currentOwner == null) return Result<bool>.Failure("Current owner is not a follower of this page.");

            var target = await _unitOfWork.Pages.GetFollowerAsync(targetUserId, pageId);
            if (target == null) return Result<bool>.Failure("Target user is not a member of this page.");

            currentOwner.Role = PageRole.Admin;
            target.Role = PageRole.PageOwner;
            page.AdminId = targetUserId;

            _unitOfWork.Pages.Update(page);
            var affectedRows = await _unitOfWork.CompleteAsync();
            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<string>> LeavePageAsync(Guid pageId, Guid userId, string? reason = null)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null) return Result<string>.Failure("Page not found.");

            var follower = await _unitOfWork.Pages.GetFollowerAsync(userId, pageId);
            if (follower == null) return Result<string>.Failure("You are not a member of this page.");

            // Succession: if the leaving user is the PageOwner, promote the earliest Admin
            // (by FollowedAt) to PageOwner before removing the founder.
            if (follower.Role == PageRole.PageOwner)
            {
                var successor = await _unitOfWork.Pages.GetEarliestAdminAsync(pageId);
                if (successor == null)
                {
                    // No Admin to take over. If the founder is the only follower, hard-delete the page.
                    var followersCount = await _unitOfWork.Pages.GetFollowersCountAsync(pageId);
                    if (followersCount <= 1)
                    {
                        await _unitOfWork.BeginTransactionAsync();
                        try
                        {
                            var pagePosts = await _unitOfWork.Posts.GetPagePostsAsync(pageId);
                            foreach (var post in pagePosts)
                            {
                                post.IsDeleted = true;
                                post.PageId = null;
                                _unitOfWork.Posts.Update(post);
                            }
                            await _unitOfWork.CompleteAsync();

                            _unitOfWork.Pages.RemoveFollower(userId, pageId);
                            _unitOfWork.Pages.Delete(page);
                            await _unitOfWork.CompleteAsync();

                            await _unitOfWork.CommitTransactionAsync();
                            return Result<string>.Success("deleted");
                        }
                        catch (Exception ex)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            _logger.LogError(ex, "Failed to delete page {PageId} upon founder leave", pageId);
                            return Result<string>.Failure("An error occurred while leaving and deleting the page.");
                        }
                    }
                }

                successor.Role = PageRole.PageOwner;
                page.AdminId = successor.UserId;
                _unitOfWork.Pages.Update(page);
                _unitOfWork.Pages.RemoveFollower(userId, pageId);
                await _unitOfWork.CompleteAsync();

                var previousOwner = await _userService.GetProfileAsync(userId);
                var previousOwnerName = previousOwner.Value?.Name ?? "The previous owner";

                var reasonText = string.IsNullOrWhiteSpace(reason)
                    ? "No reason was provided."
                    : $"Reason: {reason.Trim()}";

                await _notificationService.CreateNotificationAsync(
                    receiverId: successor.UserId,
                    message: $"{previousOwnerName} transferred ownership of page '{page.Name}' to you. {reasonText}",
                    type: NotificationType.PageFollow,
                    senderId: userId,
                    targetId: pageId
                );

                return Result<string>.Success("ownership_transferred");
            }

            // Non-owner: if the only Admin (other than the PageOwner) is leaving, force them to promote first.
            if (follower.Role == PageRole.Admin)
            {
                var regularAdminCount = await _unitOfWork.Pages.GetRoleCountAsync(pageId, PageRole.Admin);
                if (regularAdminCount <= 1)
                {
                    return Result<string>.Failure("You are the only Admin. Promote another member before leaving.");
                }
            }

            _unitOfWork.Pages.RemoveFollower(userId, pageId);
            await _unitOfWork.CompleteAsync();
            var user = await _userService.GetProfileAsync(userId);
            var userName = user.Value?.Name ?? "A user";

            var leaveReasonText = string.IsNullOrWhiteSpace(reason)
                ? "No reason provided."
                : $"Reason: {reason.Trim()}";

            await _notificationService.CreateNotificationAsync(
                receiverId: page.AdminId,
                message: $"{userName} left your page '{page.Name}'. {leaveReasonText}",
                type: NotificationType.PageFollow,
                senderId: userId,
                targetId: pageId
            );
            return Result<string>.Success("left");
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            // SQL Server unique-constraint errors surface as 2627 (unique constraint)
            // or 2601 (duplicate key). We check via the type name + Number without
            // taking a direct dependency on Microsoft.Data.SqlClient in this layer.
            var inner = ex.InnerException;
            while (inner != null)
            {
                var typeName = inner.GetType().FullName ?? string.Empty;
                if (typeName.Contains("SqlException"))
                {
                    var numProp = inner.GetType().GetProperty("Number");
                    if (numProp != null)
                    {
                        var num = (int)(numProp.GetValue(inner) ?? 0);
                        if (num == 2627 || num == 2601) return true;
                    }
                }
                inner = inner.InnerException;
            }
            return false;
        }

        public async Task<Result> CanPostAsPageAsync(Guid userId, Guid pageId)
        {
            if (pageId == Guid.Empty)
                return Result.Failure("Invalid page identifier.");

            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null)
                return Result.Failure("Page not found.");

            var actorRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(userId, pageId);
            return _pageDomainService.CanPostAsPage(actorRole);
        }

        public async Task<Result<IEnumerable<PageResponseDto>>> GetPagesToDiscoverAsync(Guid userId, int count = 5)
        {
            var pages = await _unitOfWork.Pages.GetPagesToDiscoverAsync(userId, count);
            var dtos = _mapper.Map<IEnumerable<PageResponseDto>>(pages);
            return Result<IEnumerable<PageResponseDto>>.Success(dtos);
        }


        public async Task<Result<bool>> HasPendingRequestAsync(Guid pageId, Guid userId)
        {
            var hasPending = await _unitOfWork.Pages.HasPendingRequestAsync(pageId, userId);
            return Result<bool>.Success(hasPending);
        }

        public async Task<Result<bool>> SubmitFollowRequestAsync(Guid userId, SubmitPageFollowRequestDto dto)
        {
            if (dto == null || dto.PageId == Guid.Empty)
                return Result<bool>.Failure("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.Message))
                return Result<bool>.Failure("A request message is required.");

            if (dto.Message.Trim().Length > 500)
                return Result<bool>.Failure("Request message cannot exceed 500 characters.");

            var page = await _unitOfWork.Pages.GetByIdAsync(dto.PageId);
            if (page == null)
                return Result<bool>.Failure("Page not found.");

            if (!page.IsPrivate)
                return Result<bool>.Failure("This page is public. You can follow it directly.");

            if (page.AdminId == userId || await _unitOfWork.Pages.IsFollowingAsync(userId, dto.PageId))
                return Result<bool>.Failure("You are already a member or owner of this page.");

            if (await _unitOfWork.Pages.HasPendingRequestAsync(dto.PageId, userId))
                return Result<bool>.Failure("You already have a pending request for this page.");

            var request = new PageFollowRequest
            {
                Id = Guid.NewGuid(),
                PageId = dto.PageId,
                UserId = userId,
                Message = dto.Message.Trim(),
                Status = PageFollowRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Pages.AddFollowRequest(request);
            await _unitOfWork.CompleteAsync();

            var requester = await _userService.GetProfileAsync(userId);
            var requesterName = requester.Value?.Name ?? "A user";

            // Notify Page Owner
            await _notificationService.CreateNotificationAsync(
                receiverId: page.AdminId,
                message: $"{requesterName} requested to follow your private page '{page.Name}'.",
                type: NotificationType.PageFollowRequest,
                senderId: userId,
                targetId: page.Id
            );

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> ReviewFollowRequestAsync(Guid reviewerUserId, ReviewPageFollowRequestDto dto)
        {
            if (dto == null || dto.RequestId == Guid.Empty)
                return Result<bool>.Failure("Invalid request.");

            var request = await _unitOfWork.Pages.GetFollowRequestByIdAsync(dto.RequestId);
            if (request == null)
                return Result<bool>.Failure("Follow request not found.");

            if (request.Status != PageFollowRequestStatus.Pending)
                return Result<bool>.Failure("This request has already been reviewed.");

            var reviewerRole = await _unitOfWork.Pages.GetUserRoleInPageAsync(reviewerUserId, request.PageId);
            if (reviewerRole == null || reviewerRole < PageRole.Admin)
                return Result<bool>.Failure("You do not have permission to review requests for this page.");

            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserId = reviewerUserId;

            if (dto.Approve)
            {
                request.Status = PageFollowRequestStatus.Accepted;

                if (!await _unitOfWork.Pages.IsFollowingAsync(request.UserId, request.PageId))
                {
                    var follower = new PageFollower
                    {
                        UserId = request.UserId,
                        PageId = request.PageId,
                        FollowedAt = DateTime.UtcNow,
                        Role = PageRole.Member
                    };
                    _unitOfWork.Pages.AddFollower(follower);
                }

                await _unitOfWork.CompleteAsync();

                await _notificationService.CreateNotificationAsync(
                    receiverId: request.UserId,
                    message: $"Your request to follow '{request.Page.Name}' has been accepted.",
                    type: NotificationType.PageRequestAccepted,
                    senderId: reviewerUserId,
                    targetId: request.PageId
                );
            }
            else
            {
                request.Status = PageFollowRequestStatus.Rejected;
                await _unitOfWork.CompleteAsync();

                await _notificationService.CreateNotificationAsync(
                    receiverId: request.UserId,
                    message: $"Your request to follow '{request.Page.Name}' was declined.",
                    type: NotificationType.PageRequestRejected,
                    senderId: reviewerUserId,
                    targetId: request.PageId
                );
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<IEnumerable<PageFollowRequestDto>>> GetPendingRequestsAsync(Guid userId, Guid? pageId = null)
        {
            IEnumerable<PageFollowRequest> requests;

            if (pageId.HasValue && pageId.Value != Guid.Empty)
            {
                var role = await _unitOfWork.Pages.GetUserRoleInPageAsync(userId, pageId.Value);
                if (role == null || role < PageRole.Admin)
                    return Result<IEnumerable<PageFollowRequestDto>>.Failure("Unauthorized.");

                requests = await _unitOfWork.Pages.GetPendingFollowRequestsAsync(pageId.Value);
            }
            else
            {
                requests = await _unitOfWork.Pages.GetPendingFollowRequestsForUserPagesAsync(userId);
            }

            var dtos = requests.Select(r => new PageFollowRequestDto
            {
                Id = r.Id,
                PageId = r.PageId,
                PageName = r.Page?.Name ?? "Page",
                UserId = r.UserId,
                UserName = r.User?.Name ?? "User",
                UserAvatarUrl = r.User?.ProfilePictureUrl,
                Message = r.Message,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToList();

            return Result<IEnumerable<PageFollowRequestDto>>.Success(dtos);
        }

    }
}
