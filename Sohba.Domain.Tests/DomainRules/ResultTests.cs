using Sohba.Domain.Common;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for the Result / Result&lt;T&gt; contract used across the domain layer.
    /// </summary>
    public class ResultTests
    {
        /// <summary>Verifies that a success result is successful with no error message.</summary>
        [Fact]
        public void Success_IsSuccessful_HasNoError()
        {
            var result = Result.Success();

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(string.Empty, result.Error);
        }

        /// <summary>Verifies that a failure result carries the provided error message.</summary>
        [Fact]
        public void Failure_IsFailure_CarriesErrorMessage()
        {
            var result = Result.Failure("Something went wrong.");

            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Equal("Something went wrong.", result.Error);
        }

        /// <summary>Verifies that creating a success result with an error message is rejected.</summary>
        [Fact]
        public void Failure_CalledWithEmptyError_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Result.Failure(string.Empty));
            Assert.Throws<InvalidOperationException>(() => Result.Failure(null!));
        }

        /// <summary>Verifies that a typed success result exposes the wrapped value.</summary>
        [Fact]
        public void GenericSuccess_ReturnsWrappedValue()
        {
            var result = Result<string>.Success("payload");

            Assert.True(result.IsSuccess);
            Assert.Equal("payload", result.Value);
        }

        /// <summary>Verifies that the value of a typed failure result cannot be accessed.</summary>
        [Fact]
        public void GenericFailure_ValueAccess_Throws()
        {
            var result = Result<string>.Failure("no value");

            Assert.True(result.IsFailure);
            Assert.Throws<InvalidOperationException>(() => result.Value);
        }

        /// <summary>Verifies that creating a typed success result with an error message is rejected.</summary>
        [Fact]
        public void GenericSuccess_WithErrorMessage_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Result<string>.Failure(""));
        }
    }
}
