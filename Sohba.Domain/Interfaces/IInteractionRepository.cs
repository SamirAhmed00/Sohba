using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IInteractionRepository
    {
        // Reaction Methods
        Task<Reaction?> GetReactionAsync(Guid userId, Guid postId);
        bool HasUserReacted(Guid userId, Guid entityId);
        Task<int> GetReactionCountAsync(Guid entityId);
        void AddReaction(Reaction reaction);
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
    }
}
