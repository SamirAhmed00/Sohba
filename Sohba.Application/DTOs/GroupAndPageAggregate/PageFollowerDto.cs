using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class PageFollowerDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime FollowedAt { get; set; }
    }
}
