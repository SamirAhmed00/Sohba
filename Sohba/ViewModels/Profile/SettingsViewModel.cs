namespace Sohba.ViewModels.Profile
{
    public class SettingsViewModel
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

        // Password (for forms)
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
