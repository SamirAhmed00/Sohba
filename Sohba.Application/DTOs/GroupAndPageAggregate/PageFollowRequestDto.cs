using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class PageFollowRequestDto
    {
        public Guid Id { get; set; }
        public Guid PageId { get; set; }
        public string PageName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public string Message { get; set; } = string.Empty;
        public PageFollowRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubmitPageFollowRequestDto
    {
        public Guid PageId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ReviewPageFollowRequestDto
    {
        public Guid RequestId { get; set; }
        public bool Approve { get; set; }
    }
}
