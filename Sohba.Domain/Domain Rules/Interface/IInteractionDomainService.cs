using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IInteractionDomainService
    {
        // Reactions
        Result CanAddReaction(Guid userId, bool isContentDeleted, bool isUserBlocked);
        Result CanUpdateReaction(Guid userId, bool reactionExists);

        // Comments
        Result CanAddComment(Guid userId, string text, bool isContentDeleted, bool isBlockedByOwner);
        Result CanEditComment(Guid userId, Guid commentOwnerId, DateTime createdAt, int editLimitMinutes);
        Result CanDeleteComment(Guid userId, Guid commentOwnerId, Guid postOwnerId, bool isAdmin);

        // Replies
        Result CanReplyToComment(Guid userId, bool isCommentDeleted, bool isThreadLocked);
    }
}
