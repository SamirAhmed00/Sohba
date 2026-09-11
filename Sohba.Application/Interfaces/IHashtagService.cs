using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IHashtagService
    {
        Task<Result<IEnumerable<HashtagDto>>> GetTrendingHashtagsAsync(int count = 10);
        Task<Result<IEnumerable<PostResponseDto>>> GetPostsByHashtagAsync(string tag, Guid currentUserId);
        Task<Result<PagedResult<HashtagDto>>> GetTrendingHashtagsPagedAsync(int page = 1, int pageSize = 5);

    }

}
