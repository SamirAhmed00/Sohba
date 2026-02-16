using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IInteractionRepository
    {
        // Reaction Methods
        Task<Reaction?> GetReactionAsync(Guid userId, Guid postId);
        Task<IEnumerable<Reaction>> GetUserReactionsForPostsAsync(Guid userId, IEnumerable<Guid> postIds);
        bool HasUserReacted(Guid userId, Guid entityId);
        Task<int> GetReactionCountAsync(Guid entityId);
        void AddReaction(Reaction reaction);
        void UpdateReaction(Reaction reaction);
        void RemoveReaction(Reaction reaction);

        // Comment Methods
        Task<Comment?> GetCommentByIdAsync(Guid commentId);
        Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(Guid postId);
        void AddComment(Comment comment);
        void RemoveComment(Comment comment);

        // SavedPost Methods
        Task<SavedPost?> GetSavedPostAsync(Guid userId, Guid postId);
        void AddSavedPost(SavedPost savedPost);
        void RemoveSavedPost(SavedPost savedPost);
        Task<IEnumerable<SavedPost>> GetSavedPostsByUserAsync(Guid userId);
        Task<IEnumerable<SavedPost>> GetSavedPostsByUserAndTagAsync(Guid userId, SavedTag tag);
        void UpdateSavedPost(SavedPost savedPost); 
    }
}
