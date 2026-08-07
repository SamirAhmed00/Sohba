using Sohba.Application.DTOs.Common;
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
        // Get feed with pagination
        Task<Result<PagedResult<PostResponseDto>>> GetFeedAsync( 
            Guid userId,
            int page = 1,
            int pageSize = 10);

        // Basic CRUD
        // Keep old method for backward compatibility -- I will Remove It Later
       
        Task<Result<PostResponseDto>> CreatePostAsync(PostCreateDto postDto, Guid userId);
        Task<Result<PostResponseDto>> GetPostByIdAsync(Guid postId, Guid currentUserId);
        Task<Result> DeletePostAsync(Guid postId, Guid userId, bool isAdmin = false);
        Task<Result> UpdatePostAsync(Guid postId, PostUpdateDto postDto, Guid userId);
        

        // Filtered PostsS
        Task<Result<IEnumerable<PostResponseDto>>> GetGroupPostsAsync(Guid groupId, Guid currentUserId);
        Task<Result<IEnumerable<PostResponseDto>>> GetPagePostsAsync(Guid pageId, Guid currentUserId);
        Task<Result<IEnumerable<PostResponseDto>>> GetUserPostsAsync(Guid userId, Guid currentUserId);

        // Admin
        Task<Result<IEnumerable<PostResponseDto>>> GetAllPostsAsync();
        Task<Result> HidePostAsync(Guid postId, Guid userId);
        
        Task<Result<IEnumerable<PostResponseDto>>> MapPostsWithInteractions(IEnumerable<Post> posts, Guid currentUserId);

        Task<Result<int>> GetPostsCountAsync();        
        Task<Result<IEnumerable<PostResponseDto>>> GetRecentPostsAsync(int count);
    }
}
