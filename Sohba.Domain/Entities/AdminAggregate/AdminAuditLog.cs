using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.AdminAggregate
{
    public class AdminAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AdminId { get; set; }
        public string AdminEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public Guid TargetId { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
