namespace Sohba.ViewModels.Profile
{
    public class SettingsViewModel
    {
        public string Email { get; set; }
        public bool IsPrivateAccount { get; set; }
        public bool ShowActivityStatus { get; set; }
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
    }
}
