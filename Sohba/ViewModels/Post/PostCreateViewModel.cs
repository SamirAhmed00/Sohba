using Sohba.Domain.Enums;
namespace Sohba.ViewModels.Post
{
    public class PostCreateViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public IFormFile? ImageFile { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? VideoFile { get; set; }
        public string? VideoUrl { get; set; }
        public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;
    }
}
