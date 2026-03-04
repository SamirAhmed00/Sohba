using Sohba.Application.DTOs.SearchAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface ISearchService
    {
        Task<Result<SearchResultDto>> GlobalSearchAsync(string query, Guid currentUserId);
        Task<Result<List<PostSearchResultDto>>> SearchPostsAsync(string query);
        Task<Result<List<UserSearchResultDto>>> SearchUsersAsync(string query, Guid currentUserId);
        Task<Result<List<GroupSearchResultDto>>> SearchGroupsAsync(string query);
        Task<Result<List<PageSearchResultDto>>> SearchPagesAsync(string query);
    }
}
