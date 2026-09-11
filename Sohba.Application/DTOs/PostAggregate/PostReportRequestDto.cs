using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class PostReportRequestDto
    {
        public Guid PostId { get; set; }
        public Guid? UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? AdditionalInfo { get; set; }
        public string? OtherText
        {
            get => AdditionalInfo;
            set => AdditionalInfo = value;
        }
    }
}
