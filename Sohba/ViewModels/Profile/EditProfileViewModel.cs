using System.ComponentModel.DataAnnotations;

namespace Sohba.ViewModels.Profile
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        public string? ProfilePictureUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }

        public IFormFile? ProfileImageFile { get; set; }
        public IFormFile? BackgroundImageFile { get; set; }
    }
}
