using System.ComponentModel.DataAnnotations;

namespace Sohba.ViewModels.Group
{
    public class GroupCreateViewModel
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Group name must be between 3 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Group rules cannot exceed 2000 characters.")]
        public string? Rules { get; set; }

        public bool IsPrivate { get; set; }

        public IFormFile? ImageFile { get; set; }
        public IFormFile? BackgroundImageFile { get; set; }
    }
}
