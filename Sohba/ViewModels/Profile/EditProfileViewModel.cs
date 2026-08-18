namespace Sohba.ViewModels.Profile
{
    public class EditProfileViewModel
    {
        public string Name { get; set; }
        public string Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }

        public IFormFile? ProfileImageFile { get; set; }
        public IFormFile? BackgroundImageFile { get; set; }

    }
}
