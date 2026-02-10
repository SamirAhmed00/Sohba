using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
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

        public Result CanViewStory(Guid viewerId, Guid creatorId, bool isFriend, DateTime createdAt)
        {
            // Rule 1: Check expiration
            if (IsStoryExpired(createdAt))
                return Result.Failure("This story has expired.");

            // Rule 2: Privacy (assuming stories are for friends only or owner)
            if (viewerId == creatorId) return Result.Success();

            if (!isFriend)
                return Result.Failure("You must be friends to view this story.");

            return Result.Success();
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
