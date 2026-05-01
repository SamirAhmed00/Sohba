using FluentValidation;
using Sohba.ViewModels.Post;

namespace Sohba.Validators
{
    public class PostCreateViewModelValidator : AbstractValidator<PostCreateViewModel>
    {
        public PostCreateViewModelValidator()
        {
            RuleFor(x => x.Title)
                .MaximumLength(150).WithMessage("Title cannot exceed 150 characters.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Post content cannot be empty.")
                .MaximumLength(5000).WithMessage("Post content cannot exceed 5000 characters.");
            
            RuleFor(x => x.Privacy)
                .IsInEnum().WithMessage("Invalid privacy setting.");
        }
    }
}
