using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.UserAggregate
{
    public class UserSettingsDto
    {
        // Account
        public string Email { get; set; }
        public string Name { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Privacy
        public bool IsPrivateAccount { get; set; }
        public bool ShowActivityStatus { get; set; }

        // Notifications
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool WeeklyDigest { get; set; }

        // Dates
        public DateTime? LastPasswordChanged { get; set; }
    }
}
