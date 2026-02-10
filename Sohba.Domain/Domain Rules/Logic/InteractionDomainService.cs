using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class InteractionDomainService : IInteractionDomainService
    {
        public Result CanAddComment(Guid userId, string text, bool isContentDeleted, bool isBlockedByOwner)
        {
            // Cannot interact with deleted content
            if (isContentDeleted)
                return Result.Failure("Content is deleted.");

            // Cannot comment if blocked by the post owner
            if (isBlockedByOwner)
                return Result.Failure("You cannot comment on this post.");

            // Comment text validation
            if (string.IsNullOrWhiteSpace(text))
                return Result.Failure("Comment cannot be empty.");

            return Result.Success();
        }

        public Result CanAddReaction(Guid userId, bool isContentDeleted, bool isUserBlocked)
        {
            if (isContentDeleted)
                return Result.Failure("Cannot react to deleted content.");

            if (isUserBlocked)
                return Result.Failure("You are blocked from interacting with this user.");

            return Result.Success();
        }

        public Result CanDeleteComment(Guid userId, Guid commentOwnerId, Guid postOwnerId, bool isAdmin)
        {
            // 1. The comment owner can delete it
            if (userId == commentOwnerId)
                return Result.Success();

            // 2. The post owner (where the comment is) can delete it
            if (userId == postOwnerId)
                return Result.Success();

            // 3. System Admin or Moderator can delete it
            if (isAdmin)
                return Result.Success();

            return Result.Failure("You do not have permission to delete this comment.");
        }

        public Result CanEditComment(Guid userId, Guid commentOwnerId, DateTime createdAt, int editLimitMinutes)
        {
            // Only the owner can edit
            if (userId != commentOwnerId)
                return Result.Failure("You can only edit your own comments.");

            // Time limit check (e.g., 15 minutes)
            if (DateTime.UtcNow > createdAt.AddMinutes(editLimitMinutes))
                return Result.Failure($"You cannot edit comments older than {editLimitMinutes} minutes.");

            return Result.Success();
        }

        public Result CanReplyToComment(Guid userId, bool isCommentDeleted, bool isThreadLocked)
        {
            if (isCommentDeleted)
                return Result.Failure("Cannot reply to a deleted comment.");

            if (isThreadLocked)
                return Result.Failure("This discussion thread is locked.");

            return Result.Success();
        }

        public Result CanUpdateReaction(Guid userId, bool reactionExists)
        {
            // Logic: You can only update/remove a reaction if it exists
            if (!reactionExists)
                return Result.Failure("No reaction found to update.");

            return Result.Success();
        }
    }
}
