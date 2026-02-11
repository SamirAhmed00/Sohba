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
        public string NotificationType { get; set; } // Enum as string
        public DateTime CreatedAt { get; set; }
        public Guid? TargetId { get; set; }
    }
}
