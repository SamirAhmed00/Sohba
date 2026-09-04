using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Enums
{
    public enum NotificationType
    {
        PostLike = 1,
        PostComment = 2,
        FriendRequest = 3,
        GroupInvitation = 4,
        SystemAlert = 5,
        PageFollow = 6,
        StoryLike = 7,
        PageFollowRequest = 8,
        PageRequestAccepted = 9,
        PageRequestRejected = 10
    }
}
