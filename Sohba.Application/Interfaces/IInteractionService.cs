using Sohba.Application.DTOs.PostAggregate;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IInteractionService
    {
        // Reactions
        Task<Result> AddReactionAsync(Guid userId, Guid postId, ReactionType type);
        Task<Result> RemoveReactionAsync(Guid userId, Guid postId);
        Task<Reaction?> GetUserReactionAsync(Guid userId, Guid postId);
        Task<int> GetReactionCountAsync(Guid postId);

        // Comments
        Task<Result> AddCommentAsync(Guid userId, Guid postId, string content);
        Task<IEnumerable<CommentResponseDto>> GetCommentsByPostIdAsync(Guid postId);
        Task<Result> DeleteCommentAsync(Guid userId, Guid commentId, bool isAdmin);

        Task<Result> AddReplyAsync(Guid userId, Guid commentId, string content);

        // Saved Posts
        Task<Result<IEnumerable<PostResponseDto>>> GetSavedPostsAsync(Guid userId);
        Task<Result<IEnumerable<PostResponseDto>>> GetFavoritePostsAsync(Guid userId);
        Task<Result<IEnumerable<PostResponseDto>>> GetSavedPostsByTagAsync(Guid userId, SavedTag tag);
        Task<Result<SavedPostDto>> SavePostAsync(Guid userId, Guid postId, SavedTag tag = SavedTag.General, string? userTag = null);
        Task<Result> RemoveSavedPostAsync(Guid userId, Guid postId);
    }
}
