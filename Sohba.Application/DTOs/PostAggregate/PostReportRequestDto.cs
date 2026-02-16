using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class PostReportRequestDto
    {
        public Guid PostId { get; set; }
        public Guid? UserId { get; set; }
        public string Reason { get; set; } // Enum mapping
        public string? AdditionalInfo { get; set; } 
    }
}
