using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class SubmitJoinRequestDto
    {
        public Guid GroupId { get; set; }
        public string? Reason { get; set; }
    }
}
