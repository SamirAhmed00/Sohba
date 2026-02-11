using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class PostReportResponseDto
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public string ReporterName { get; set; }
        public string Reason { get; set; }
        public bool IsResolved { get; set; }
        public DateTime ReportedAt { get; set; }
    }
}
