using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class StoryDomainService : IStoryDomainService
    {
        public Result CanCreateStory(Guid userId, bool hasMedia, int dailyStoryLimit, int currentStoryCount)
        {
            // Rule: Story must have media (image/video)
            if (!hasMedia)
                return Result.Failure("Story must contain media.");

            // Rule: Check daily limit
            if (currentStoryCount >= dailyStoryLimit)
                return Result.Failure($"You have reached your daily limit of {dailyStoryLimit} stories.");

            return Result.Success();
        }

        public Result CanViewStory(Guid viewerId, Guid creatorId, StoryPrivacy privacy, bool isCreatorAccountPrivate, bool isFriend, DateTime createdAt)
        {
            if (IsStoryExpired(createdAt))
                return Result.Failure("This story has expired.");

            if (viewerId == creatorId) return Result.Success();

            if (isCreatorAccountPrivate)
            {
                return isFriend
                    ? Result.Success()
                    : Result.Failure("This account is private. You must be friends to view this story.");
            }

            if (privacy == StoryPrivacy.Public) return Result.Success();

            if (privacy == StoryPrivacy.FriendsOnly && isFriend) return Result.Success();

            return Result.Failure("You must be friends to view this story.");
        }

        public Result CanReplyToStory(Guid userId, bool isCreatorAcceptingReplies, bool isExpired)
        {
            if (isExpired) return Result.Failure("Cannot reply to an expired story.");

            if (!isCreatorAcceptingReplies)
                return Result.Failure("The creator has turned off replies for this story.");

            return Result.Success();
        }

        public bool IsStoryExpired(DateTime createdAt)
        {
            // Rule: Story expires after 24 hours
            return createdAt.AddHours(24) < DateTime.UtcNow;
        }

        public Result CanHighlightStory(Guid userId, Guid creatorId, bool isExpired)
        {
            if (userId != creatorId)
                return Result.Failure("Only the owner can highlight their story.");

            // Even expired stories can be highlighted (archived), so we generally allow it
            // unless there is a specific business rule against it.
            return Result.Success();
        }
    }
}
