using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IPostDomainService
    {
        // Creation & Basic Rules
        Result CanCreatePost(Guid userId, string content, bool hasAttachments);
        Result CanUpdatePost(Guid userId, Guid postId, bool isPostDeleted);
        Result CanDeletePost(Guid userId, Guid postId, Guid postOwnerId, bool isAdmin);

        // Privacy & Audience -- It will internally depend on the FriendshipService.
        Result CanViewPost(Guid userId, Guid postOwnerId, bool isPrivate, bool isFriend);

        // Interactions
        Result CanCommentOnPost(Guid userId, Guid postOwnerId, bool isDeleted, bool isBlocked);
        Result CanReactToPost(Guid userId, Guid postOwnerId, bool isDeleted);
        Result CanSharePost(Guid userId, Guid postId, bool isPrivate);

        // Group Context Rules
        Result CanPostInGroup(Guid userId, Guid groupId, bool isMember, bool isBannedFromGroup);

        // Safety & Moderation
        Result CanReportPost(Guid userId, Guid postId, bool alreadyReported);
    }
}
