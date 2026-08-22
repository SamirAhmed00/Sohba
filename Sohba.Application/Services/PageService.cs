using AutoMapper;
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
using System.Text;

namespace Sohba.Application.Services
{
    public class PageService : IPageService
    {
        private readonly IPageDomainService _pageDomainService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly ILogger<PageService> _logger;

        public PageService(
            IPageDomainService pageDomainService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IUserService userService,
            INotificationService notificationService,
            ILogger<PageService> logger)
        {
            _pageDomainService = pageDomainService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userService = userService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Result<PageResponseDto>> CreatePageAsync(Guid adminId, PageCreateDto dto)
        {
            var domainDecision = _pageDomainService.CanCreatePage(dto?.Name);
            if (domainDecision.IsFailure)
                return Result<PageResponseDto>.Failure(domainDecision.Error);

            var page = new Page
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Pages.Add(page);

            var follower = new PageFollower
            {
                UserId = adminId,
                PageId = page.Id,
                FollowedAt = DateTime.UtcNow,
                Role = PageRole.Admin
            };
            _unitOfWork.Pages.AddFollower(follower);
            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<PageResponseDto>(page);
            return Result<PageResponseDto>.Success(response);
        }

        public async Task<Result> FollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);            
            var followedPages = await _unitOfWork.Pages.GetPagesByFollowerIdAsync(userId);
            var alreadyFollowing = followedPages.Any(p => p.Id == pageId);

            
            var domainDecision = _pageDomainService.CanFollowPage(userId, page, alreadyFollowing);
            if (domainDecision.IsFailure)
                return domainDecision;

            var follower = new PageFollower
            {
                UserId = userId,
                PageId = pageId,
                FollowedAt = DateTime.UtcNow
            };

            
            _unitOfWork.Pages.AddFollower(follower);
            await _unitOfWork.CompleteAsync();

            // Send notification to page admin
            if (page.AdminId != userId)
            {
                var user = await _userService.GetProfileAsync(userId);
                var userName = user.Value?.Name ?? "Someone";

                await _notificationService.CreateNotificationAsync(
                    receiverId: page.AdminId,
                    message: $"{userName} followed your page '{page.Name}'",
                    type: NotificationType.SystemAlert,
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
                return Result.Failure("Page not found");

            
            var followedPages = await _unitOfWork.Pages.GetPagesByFollowerIdAsync(userId);
            var alreadyFollowing = followedPages.Any(p => p.Id == pageId);
            
            var domainDecision = _pageDomainService.CanUnfollowPage(alreadyFollowing);
            if (domainDecision.IsFailure)
                return domainDecision;

            _unitOfWork.Pages.RemoveFollower(userId, pageId);
            await _unitOfWork.CompleteAsync();


            // Notification When User Unfollows the Page (if the user is not the admin of the page)
            if (page.AdminId != userId)
            {
                var user = await _userService.GetProfileAsync(userId);
                await _notificationService.CreateNotificationAsync(
                    receiverId: page.AdminId,
                    message: $"{user.Value?.Name} unfollowed your page '{page.Name}'",
                    type: NotificationType.SystemAlert,
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

            var role = _unitOfWork.Pages.GetUserRoleInPage(adminId, pageId);
            var validation = _pageDomainService.CanDeletePage(adminId, role);
            if (!validation.IsSuccess) return validation;

            _logger.LogInformation("Page {PageId} ('{Name}') deleted by admin {UserId}. Reason: {Reason}",
                pageId, page.Name, adminId, reason);

            _unitOfWork.Pages.Delete(page);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<Result<bool>> ToggleFollowPageAsync(Guid userId, Guid pageId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null)
                return Result<bool>.Failure("Page not found");

            // Check if already following
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
            var followedPages = await _unitOfWork.Pages.GetPagesByFollowerIdAsync(userId);
            var isFollowing = followedPages.Any(p => p.Id == pageId);
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

        public Task<Result<bool>> IsPageAdminAsync(Guid userId, Guid pageId)
        {
            var role = _unitOfWork.Pages.GetUserRoleInPage(userId, pageId);
            return Task.FromResult(Result<bool>.Success(role == "Admin"));
        }


        public async Task<Result<PageResponseDto>> UpdatePageAsync(PageUpdateDto updateDto, Guid userId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(updateDto.Id);
            if (page == null)
                return Result<PageResponseDto>.Failure("Page not found.");

            if (page.AdminId != userId)
                return Result<PageResponseDto>.Failure("You are not authorized to edit this page.");

            // Update properties
            page.Name = updateDto.Name;
            page.Description = updateDto.Description;
            page.ImageUrl = updateDto.ImageUrl ?? page.ImageUrl;
            page.BackgroundImageUrl = updateDto.BackgroundImageUrl ?? page.BackgroundImageUrl;

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
            var actionRole = _unitOfWork.Pages.GetUserRoleInPage(adminId, pageId);
            var targetRole = _unitOfWork.Pages.GetUserRoleInPage(targetUserId, pageId);

            var validation = _pageDomainService.CanKickPageMember(adminId, actionRole, targetUserId, targetRole);
            if (!validation.IsSuccess) return Result<bool>.Failure(validation.Error);

            _unitOfWork.Pages.RemoveFollower(targetUserId, pageId);
            var affectedRows = await _unitOfWork.CompleteAsync();
            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<bool>> PromotePageMemberAsync(Guid pageId, Guid targetUserId, Guid adminId)
        {
            var actionRole = _unitOfWork.Pages.GetUserRoleInPage(adminId, pageId);
            var targetRole = _unitOfWork.Pages.GetUserRoleInPage(targetUserId, pageId);

            var validation = _pageDomainService.CanPromotePageMember(adminId, actionRole, targetRole);
            if (!validation.IsSuccess) return Result<bool>.Failure(validation.Error);

            var follower = await _unitOfWork.Pages.GetFollowerAsync(targetUserId, pageId);
            if (follower == null) return Result<bool>.Failure("Member not found.");

            follower.Role = PageRole.Admin;
            var affectedRows = await _unitOfWork.CompleteAsync();
            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<string>> LeavePageAsync(Guid pageId, Guid userId)
        {
            var page = await _unitOfWork.Pages.GetByIdAsync(pageId);
            if (page == null) return Result<string>.Failure("Page not found.");

            var follower = await _unitOfWork.Pages.GetFollowerAsync(userId, pageId);
            if (follower == null) return Result<string>.Failure("You are not following this page.");

            var followersCount = await _unitOfWork.Pages.GetFollowersCountAsync(pageId);

            // Page with zero members after leaving -> follow the intended deletion
            // lifecycle rule (Page has no members = Page is deleted).
            if (followersCount <= 1)
            {
                _unitOfWork.Pages.RemoveFollower(userId, pageId);
                _unitOfWork.Pages.Delete(page);
                await _unitOfWork.CompleteAsync();
                return Result<string>.Success("deleted");
            }

            if (follower.Role == PageRole.Admin)
            {
                var adminCount = await _unitOfWork.Pages.GetAdminCountAsync(pageId);
                if (adminCount <= 1)
                {
                    // Same terminology as the Group lifecycle rule for consistency.
                    return Result<string>.Failure("You are the only admin. Please promote another member before leaving.");
                }
            }

            _unitOfWork.Pages.RemoveFollower(userId, pageId);
            await _unitOfWork.CompleteAsync();
            return Result<string>.Success("left");
        }

    }

}
