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
                .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");

            RuleFor(x => x.PostId)
                .NotEmpty().WithMessage("Post ID is required.");
        }
    }
}
