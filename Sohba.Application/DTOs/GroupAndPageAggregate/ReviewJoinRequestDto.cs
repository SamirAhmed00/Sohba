using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class ReviewJoinRequestDto
    {
        public Guid RequestId { get; set; }
        public bool Approve { get; set; }
    }
}
