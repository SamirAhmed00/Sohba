namespace Sohba.ViewModels.Group
{
    public class GroupCreateViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
