namespace Sohba.ViewModels.Page
{
    public class PageCreateViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IFormFile? ImageFile { get; set; }
        public IFormFile? BackgroundImageFile { get; set; }
        public bool IsPrivate { get; set; }
        public string? Rules { get; set; }
    }
}
