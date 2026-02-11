using Sohba.Application.DTOs.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;
using Sohba.Domain.Common;

namespace Sohba.Application.Interfaces
{
    public interface IPostService
    {
        Task<Result<PostResponseDto>> CreatePostAsync(PostCreateDto postDto, Guid userId);
        Task<Result<IEnumerable<PostResponseDto>>> GetFeedAsync(Guid userId);
        Task<Result<PostResponseDto>> GetPostByIdAsync(Guid postId);
        Task<Result> DeletePostAsync(Guid postId, Guid userId);
        Task<Result> UpdatePostAsync(Guid postId, PostCreateDto postDto, Guid userId);

    }
}
