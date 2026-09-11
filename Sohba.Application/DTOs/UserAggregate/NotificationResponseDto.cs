using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.UserAggregate
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public string NotificationType { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? TargetId { get; set; }
        public Guid? SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderProfilePicture { get; set; }

        public string TimeAgo => GetTimeAgo(CreatedAt);

        public string TargetUrl => GetTargetUrl();

        private string GetTargetUrl()
        {
            if (string.IsNullOrEmpty(NotificationType))
                return "/Notifications/Index";

            return NotificationType switch
            {
                "PostLike" or "PostComment" when TargetId.HasValue => $"/Posts/Details/{TargetId.Value}",
                "GroupInvitation" when TargetId.HasValue => $"/Groups/Details/{TargetId.Value}",
                "FriendRequest" => "/Friends/Requests",
                "PageFollow" when TargetId.HasValue => $"/Pages/Details/{TargetId.Value}",
                "PageFollowRequest" when TargetId.HasValue => $"/Pages/PageRequests?pageId={TargetId.Value}",
                "PageRequestAccepted" when TargetId.HasValue => $"/Pages/Details/{TargetId.Value}",
                "PageRequestRejected" when TargetId.HasValue => $"/Pages/Details/{TargetId.Value}",
                "StoryLike" => "/Stories",
                _ => "/Notifications/Index"
            };
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)}w ago";
            if (timeSpan.TotalDays < 365) return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
            return $"{(int)(timeSpan.TotalDays / 365)}y ago";
        }
    }
}
