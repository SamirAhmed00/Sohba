using Sohba.Application.DTOs.PostAggregate;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IPostService
    {
        // Basic CRUD
        Task<Result<PostResponseDto>> CreatePostAsync(PostCreateDto postDto, Guid userId);
        Task<Result<IEnumerable<PostResponseDto>>> GetFeedAsync(Guid userId);
        Task<Result<PostResponseDto>> GetPostByIdAsync(Guid postId, Guid currentUserId);
        Task<Result> DeletePostAsync(Guid postId, Guid userId);
        Task<Result> UpdatePostAsync(Guid postId, PostCreateDto postDto, Guid userId);

        // Filtered PostsS
        Task<Result<IEnumerable<PostResponseDto>>> GetGroupPostsAsync(Guid groupId, Guid currentUserId);
        Task<Result<IEnumerable<PostResponseDto>>> GetPagePostsAsync(Guid pageId, Guid currentUserId);
        Task<Result<IEnumerable<PostResponseDto>>> GetUserPostsAsync(Guid userId, Guid currentUserId);

        // Admin
        Task<Result<IEnumerable<PostResponseDto>>> GetAllPostsAsync();
        Task<Result> HidePostAsync(Guid postId, Guid userId);
        
        Task<Result<IEnumerable<PostResponseDto>>> MapPostsWithInteractions(IEnumerable<Post> posts, Guid currentUserId);

    }
}
