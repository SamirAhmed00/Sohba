namespace Sohba.ViewModels.Post
{
    public class PostCreateViewModel
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPrivate { get; set; }
    }
}
