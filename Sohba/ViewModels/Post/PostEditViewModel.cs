namespace Sohba.ViewModels.Post
{
    public class PostEditViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public IFormFile? ImageFile { get; set; }
        public string? ImageUrl { get; set; }
        public Sohba.Domain.Enums.PostPrivacy Privacy { get; set; }
    }
}
