using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IStoryDomainService
    {
        Result CanCreateStory(Guid userId, bool hasMedia, int dailyStoryLimit, int currentStoryCount);
        Result CanViewStory(Guid viewerId, Guid creatorId, bool isFriend, DateTime createdAt);
        Result CanReplyToStory(Guid userId, bool isCreatorAcceptingReplies, bool isExpired);

        // Logic check for expiration
        bool IsStoryExpired(DateTime createdAt);

        Result CanHighlightStory(Guid userId, Guid creatorId, bool isExpired);
    }
}

