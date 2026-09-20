using FluentValidation.TestHelper;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Validators;

namespace Sohba.Application.Tests.Validators
{
    /// <summary>
    /// Tests for the comment request validator: required content,
    /// maximum length and required post identifier.
    /// </summary>
    public class CommentRequestDtoValidatorTests
    {
        private readonly CommentRequestDtoValidator _sut = new();

        /// <summary>Verifies that a valid comment request passes validation.</summary>
        [Fact]
        public void Validate_WithValidRequest_HasNoErrors()
        {
            var dto = new CommentRequestDto { PostId = Guid.NewGuid(), Content = "Valid comment" };

            var result = _sut.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }

        /// <summary>Verifies that empty comment content is rejected.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WithEmptyContent_IsRejected(string? content)
        {
            var dto = new CommentRequestDto { PostId = Guid.NewGuid(), Content = content! };

            var result = _sut.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Content);
        }

        /// <summary>Verifies the content boundary: exactly 1000 characters is accepted.</summary>
        [Fact]
        public void Validate_With1000Characters_IsAccepted()
        {
            var dto = new CommentRequestDto { PostId = Guid.NewGuid(), Content = new string('a', 1000) };

            var result = _sut.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Content);
        }

        /// <summary>Verifies the content boundary: 1001 characters is rejected.</summary>
        [Fact]
        public void Validate_With1001Characters_IsRejected()
        {
            var dto = new CommentRequestDto { PostId = Guid.NewGuid(), Content = new string('a', 1001) };

            var result = _sut.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Content);
        }

        /// <summary>Verifies that a missing post identifier is rejected.</summary>
        [Fact]
        public void Validate_WithEmptyPostId_IsRejected()
        {
            var dto = new CommentRequestDto { PostId = Guid.Empty, Content = "Valid comment" };

            var result = _sut.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.PostId);
        }
    }
}
