using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.UserAggregate
{
    public class FriendDto
    {
        public Guid FriendUserId { get; set; }
        public string FriendName { get; set; }
        public string Status { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
