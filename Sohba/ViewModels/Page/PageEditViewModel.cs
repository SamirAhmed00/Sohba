namespace Sohba.ViewModels.Page
{
    public class PageEditViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
        public IFormFile? BackgroundImageFile { get; set; }
    }
}
