using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Domain.Common;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IPageService
    {
        Task<Result<PageResponseDto>> CreatePageAsync(Guid adminId, PageCreateDto dto);
        Task<Result> FollowPageAsync(Guid userId, Guid pageId);
        Task<Result> UnfollowPageAsync(Guid userId, Guid pageId);
        Task<Result<PageResponseDto>> GetPageByIdAsync(Guid pageId);
        Task<Result<IEnumerable<PageResponseDto>>> GetUserFollowedPagesAsync(Guid userId);
        Task<Result<IEnumerable<PageResponseDto>>> GetAllPagesAsync();
        Task<Result> DeletePageAsync(Guid adminId, Guid pageId, string reason);
        Task<Result<bool>> KickPageMemberAsync(Guid pageId, Guid targetUserId, Guid adminId);
        Task<Result<bool>> PromotePageMemberAsync(Guid pageId, Guid targetUserId, Guid adminId, PageRole newRole);
        Task<Result<bool>> DemotePageMemberAsync(Guid pageId, Guid targetUserId, Guid adminId, PageRole newRole);
        Task<Result<bool>> TransferOwnershipAsync(Guid pageId, Guid targetUserId, Guid adminId);
        Task<Result<string>> LeavePageAsync(Guid pageId, Guid userId, string? reason = null);
        Task<Result<bool>> IsPageAdminAsync(Guid userId, Guid pageId);
        Task<Result<bool>> ToggleFollowPageAsync(Guid userId, Guid pageId);
        Task<Result<bool>> IsFollowingAsync(Guid userId, Guid pageId);
        Task<Result<int>> GetFollowersCountAsync(Guid pageId);
        Task<Result<IEnumerable<PageFollowerDto>>> GetFollowersAsync(Guid pageId, int page = 1, int pageSize = 20);
        Task<Result<PageResponseDto>> UpdatePageAsync(PageUpdateDto updateDto, Guid userId);
        Task<Result<int>> GetPagesCountAsync();
        Task<PageRole?> GetUserRoleInPageAsync(Guid userId, Guid pageId);

        Task<Result> CanPostAsPageAsync(Guid userId, Guid pageId);
        Task<Result<IEnumerable<PageResponseDto>>> GetPagesToDiscoverAsync(Guid userId, int count = 5);
        Task<Result<bool>> SubmitFollowRequestAsync(Guid userId, SubmitPageFollowRequestDto dto);
        Task<Result<bool>> ReviewFollowRequestAsync(Guid reviewerUserId, ReviewPageFollowRequestDto dto);
        Task<Result<IEnumerable<PageFollowRequestDto>>> GetPendingRequestsAsync(Guid userId, Guid? pageId = null);
        Task<Result<bool>> HasPendingRequestAsync(Guid pageId, Guid userId);
    }
}
