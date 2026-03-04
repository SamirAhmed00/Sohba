using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Domain.Common;
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
        Task<Result> DeletePageAsync(Guid adminId, Guid pageId);
        Task<Result<bool>> ToggleFollowPageAsync(Guid userId, Guid pageId);        
        Task<Result<bool>> IsFollowingAsync(Guid userId, Guid pageId);
        Task<Result<int>> GetFollowersCountAsync(Guid pageId);
        Task<Result<IEnumerable<PageFollowerDto>>> GetFollowersAsync(Guid pageId, int page = 1, int pageSize = 20);

    }
}
