using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class InteractionService : IInteractionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IInteractionDomainService _interactionDomainService;

        public InteractionService(IUnitOfWork unitOfWork, IInteractionDomainService interactionDomainService)
        {
            _unitOfWork = unitOfWork;
            _interactionDomainService = interactionDomainService;
        }

        // --- Reaction Logic ---

        public async Task<Result> ToggleReactionAsync(Guid userId, Guid postId, ReactionType type)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null) return Result.Failure("Post not found.");

            // Apply domain rules (e.g., check if post is soft-deleted)
            var canReact = _interactionDomainService.CanAddReaction(userId, post.IsDeleted, isUserBlocked: false);
            if (!canReact.IsSuccess) return canReact;

            // Using our custom repository method to find an existing reaction
            var existingReaction = await _unitOfWork.Interactions.GetReactionAsync(userId, postId);

            if (existingReaction != null)
            {
                // If it exists, we remove it (Toggle Off)
                _unitOfWork.Interactions.RemoveReaction(existingReaction);
            }
            else
            {
                // If not, we create a new one (Toggle On)
                var reaction = new Reaction
                {
                    UserId = userId,
                    PostId = postId,
                    Type = type
                };
                _unitOfWork.Interactions.AddReaction(reaction);
            }

            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        // --- Comment Logic ---

        public async Task<Result> AddCommentAsync(Guid userId, Guid postId, string content)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null) return Result.Failure("Post not found.");

            // Validate against domain rules (Length, blocked status, etc.)
            var canComment = _interactionDomainService.CanAddComment(userId, content, post.IsDeleted, isBlockedByOwner: false);
            if (!canComment.IsSuccess) return canComment;

            var comment = new Comment
            {
                UserId = userId,
                PostId = postId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Interactions.AddComment(comment);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> DeleteCommentAsync(Guid userId, Guid commentId, bool isAdmin)
        {
            var comment = await _unitOfWork.Interactions.GetCommentByIdAsync(commentId);
            if (comment == null) return Result.Failure("Comment not found.");

            // Fetch post to identify the owner (The owner can delete comments on their post)
            var post = await _unitOfWork.Posts.GetByIdAsync(comment.PostId);

            // Ask the domain service if deletion is permitted
            var canDelete = _interactionDomainService.CanDeleteComment(userId, comment.UserId, post.UserId, isAdmin);
            if (!canDelete.IsSuccess) return canDelete;

            _unitOfWork.Interactions.RemoveComment(comment);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        // --- Reply Logic ---

        public async Task<Result> AddReplyAsync(Guid userId, Guid commentId, string content)
        {
            var parentComment = await _unitOfWork.Interactions.GetCommentByIdAsync(commentId);
            if (parentComment == null) return Result.Failure("Parent comment not found.");

            var canReply = _interactionDomainService.CanReplyToComment(userId, isCommentDeleted: false, isThreadLocked: false);
            if (!canReply.IsSuccess) return canReply;

            // In your Domain, if Reply is a separate Entity or a self-referencing Comment:
            // This logic will depend on your specific entity mapping.

            return Result.Success();
        }
    }
}
