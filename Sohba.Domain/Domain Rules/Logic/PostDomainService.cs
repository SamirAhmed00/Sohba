using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class PostDomainService : IPostDomainService
    {
        public Result CanCreatePost(Guid userId, string content, bool hasAttachments)
        {
            // Rule: Post must have content OR attachments
            if (string.IsNullOrWhiteSpace(content) && !hasAttachments)
                return Result.Failure("Post cannot be empty. You must add text or attachments.");

            return Result.Success();
        }

        public Result CanUpdatePost(Guid userId, Guid postId, Guid postOwnerId, bool isPostDeleted)
        {
            if (isPostDeleted)
                return Result.Failure("Cannot update a deleted post.");

            if (userId != postOwnerId)
                return Result.Failure("You are not authorized to edit this post.");

            return Result.Success();
        }

        public Result CanDeletePost(Guid userId, Guid postId, Guid postOwnerId, bool isAdmin)
        {
            // Admin can delete anything
            if (isAdmin) return Result.Success();

            // Owner can delete their own post
            if (userId == postOwnerId) return Result.Success();

            return Result.Failure("You are not authorized to delete this post.");
        }

        public Result CanViewPost(Guid userId, Guid postOwnerId, PostPrivacy privacy, bool isFriend)
        {
            // Rule 1: Owner always sees their post
            if (userId == postOwnerId) return Result.Success();

            // Rule 2: Public posts are visible to everyone
            if (privacy == PostPrivacy.Public) return Result.Success();

            // Rule 3: Friends-only posts require friendship
            if (privacy == PostPrivacy.Friends && isFriend) return Result.Success();

            // Rule 4: Private ("Only Me") posts are visible only to the owner (Rule 1)
            return Result.Failure("This post is private.");
        }

        public Result CanCommentOnPost(Guid userId, Guid postOwnerId, bool isDeleted, bool isBlocked)
        {
            if (isDeleted) return Result.Failure("Post is deleted.");
            if (isBlocked) return Result.Failure("You are blocked by the post owner.");

            return Result.Success();
        }

        public Result CanReactToPost(Guid userId, Guid postOwnerId, bool isDeleted)
        {
            if (isDeleted) return Result.Failure("Cannot react to a deleted post.");

            return Result.Success();
        }

        public Result CanSharePost(Guid userId, Guid postId, bool isPrivate)
        {
            // Rule: Private posts cannot be shared publicly
            if (isPrivate)
                return Result.Failure("Cannot share a private post.");

            return Result.Success();
        }

        public Result CanPostInGroup(Guid userId, Guid groupId, bool isMember, bool isBannedFromGroup)
        {
            if (isBannedFromGroup) return Result.Failure("You are banned from this group.");
            if (!isMember) return Result.Failure("You must be a member to post in this group.");

            return Result.Success();
        }

        public Result CanReportPost(Guid userId, Guid postId, bool alreadyReported)
        {
            if (alreadyReported)
                return Result.Failure("You have already reported this post.");

            return Result.Success();
        }
    }
}
