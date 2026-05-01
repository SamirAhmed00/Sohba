using FluentValidation;
using Sohba.Application.DTOs.PostAggregate;

namespace Sohba.Application.Validators
{
    public class CommentRequestDtoValidator : AbstractValidator<CommentRequestDto>
    {
        public CommentRequestDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment cannot be empty.")
                .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.");
            
            RuleFor(x => x.PostId)
                .NotEmpty().WithMessage("Post ID is required.");
        }
    }
}
