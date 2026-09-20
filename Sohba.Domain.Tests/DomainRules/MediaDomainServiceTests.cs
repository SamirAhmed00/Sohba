using Sohba.Domain.Domain_Rules.Logic;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for media upload validation (5 MB image / 50 MB video),
    /// profile-picture limits (2 MB) and private-media access rules.
    /// </summary>
    public class MediaDomainServiceTests
    {
        private readonly MediaDomainService _sut = new();

        private const long ImageLimit = 5 * 1024 * 1024;
        private const long VideoLimit = 50 * 1024 * 1024;
        private const long ProfilePicLimit = 2 * 1024 * 1024;

        private static readonly Guid User = Guid.NewGuid();
        private static readonly Guid Owner = Guid.NewGuid();

        // ---- CanUploadMedia: extension validation ----

        /// <summary>Verifies that every allowed image extension is accepted.</summary>
        [Theory]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".png")]
        [InlineData(".gif")]
        [InlineData(".webp")]
        public void CanUploadMedia_WithAllowedImageExtension_ReturnsSuccess(string extension)
        {
            Assert.True(_sut.CanUploadMedia(extension, 1024, "image").IsSuccess);
        }

        /// <summary>Verifies that every allowed video extension is accepted.</summary>
        [Theory]
        [InlineData(".mp4")]
        [InlineData(".mov")]
        public void CanUploadMedia_WithAllowedVideoExtension_ReturnsSuccess(string extension)
        {
            Assert.True(_sut.CanUploadMedia(extension, 1024, "video").IsSuccess);
        }

        /// <summary>Verifies that uppercase extensions are normalized before validation.</summary>
        [Fact]
        public void CanUploadMedia_WithUppercaseExtension_ReturnsSuccess()
        {
            Assert.True(_sut.CanUploadMedia(".JPG", 1024, "image").IsSuccess);
        }

        /// <summary>Verifies that a disallowed extension is rejected.</summary>
        [Fact]
        public void CanUploadMedia_WithDisallowedExtension_ReturnsFailure()
        {
            var result = _sut.CanUploadMedia(".exe", 1024, "binary");

            Assert.True(result.IsFailure);
            Assert.Contains("not allowed", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a null extension is rejected without throwing.</summary>
        [Fact]
        public void CanUploadMedia_WithNullExtension_ReturnsFailure()
        {
            Assert.True(_sut.CanUploadMedia(null, 1024, "image").IsFailure);
        }

        /// <summary>Verifies that an empty extension is rejected.</summary>
        [Fact]
        public void CanUploadMedia_WithEmptyExtension_ReturnsFailure()
        {
            Assert.True(_sut.CanUploadMedia("", 1024, "image").IsFailure);
        }

        /// <summary>Verifies that a partially-correct extension is rejected.</summary>
        [Fact]
        public void CanUploadMedia_WithMissingDot_ReturnsFailure()
        {
            Assert.True(_sut.CanUploadMedia("jpg", 1024, "image").IsFailure);
        }

        // ---- CanUploadMedia: size boundaries ----

        /// <summary>Verifies the image boundary: exactly 5 MB is accepted.</summary>
        [Fact]
        public void CanUploadMedia_ImageExactlyAt5MBLimit_ReturnsSuccess()
        {
            Assert.True(_sut.CanUploadMedia(".jpg", ImageLimit, "image").IsSuccess);
        }

        /// <summary>Verifies the image boundary: one byte above 5 MB is rejected.</summary>
        [Fact]
        public void CanUploadMedia_ImageOneByteAbove5MBLimit_ReturnsFailure()
        {
            var result = _sut.CanUploadMedia(".jpg", ImageLimit + 1, "image");

            Assert.True(result.IsFailure);
            Assert.Contains("5 MB", result.Error);
        }

        /// <summary>Verifies the video boundary: exactly 50 MB is accepted.</summary>
        [Fact]
        public void CanUploadMedia_VideoExactlyAt50MBLimit_ReturnsSuccess()
        {
            Assert.True(_sut.CanUploadMedia(".mp4", VideoLimit, "video").IsSuccess);
        }

        /// <summary>Verifies the video boundary: one byte above 50 MB is rejected.</summary>
        [Fact]
        public void CanUploadMedia_VideoOneByteAbove50MBLimit_ReturnsFailure()
        {
            var result = _sut.CanUploadMedia(".mp4", VideoLimit + 1, "video");

            Assert.True(result.IsFailure);
            Assert.Contains("50 MB", result.Error);
        }

        /// <summary>Verifies that a huge video far above the limit is rejected.</summary>
        [Fact]
        public void CanUploadMedia_VideoFarAboveLimit_ReturnsFailure()
        {
            Assert.True(_sut.CanUploadMedia(".mp4", VideoLimit * 10, "video").IsFailure);
        }

        /// <summary>Verifies that a valid image is not accidentally measured against the video limit.</summary>
        [Fact]
        public void CanUploadMedia_ImageAboveVideoLimit_ReportsImageLimitNotVideo()
        {
            var result = _sut.CanUploadMedia(".png", VideoLimit + 1, "image");

            Assert.True(result.IsFailure);
            Assert.Contains("Image", result.Error);
        }

        /// <summary>Verifies that a zero-byte file with a valid extension is accepted.</summary>
        [Fact]
        public void CanUploadMedia_ZeroByteFile_ReturnsSuccess()
        {
            Assert.True(_sut.CanUploadMedia(".jpg", 0, "image").IsSuccess);
        }

        /// <summary>Verifies that the owner can access their own private media.</summary>
        [Fact]
        public void CanAccessMedia_WhenViewerIsOwner_ReturnsSuccess_EvenForPrivateMedia()
        {
            Assert.True(_sut.CanAccessMedia(User, User, isPrivate: true, isFriend: false).IsSuccess);
        }

        /// <summary>Verifies that a friend can access another user's private media.</summary>
        [Fact]
        public void CanAccessMedia_WhenPrivateAndViewerIsFriend_ReturnsSuccess()
        {
            Assert.True(_sut.CanAccessMedia(User, Owner, isPrivate: true, isFriend: true).IsSuccess);
        }

        /// <summary>Verifies that a non-friend cannot access another user's private media.</summary>
        [Fact]
        public void CanAccessMedia_WhenPrivateAndViewerIsNotFriend_ReturnsFailure()
        {
            var result = _sut.CanAccessMedia(User, Owner, isPrivate: true, isFriend: false);

            Assert.True(result.IsFailure);
            Assert.Contains("private", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that non-private media is accessible to any user.</summary>
        [Fact]
        public void CanAccessMedia_WhenNotPrivate_ReturnsSuccess()
        {
            Assert.True(_sut.CanAccessMedia(User, Owner, isPrivate: false, isFriend: false).IsSuccess);
        }

        // ---- CanSetProfilePicture ----

        /// <summary>Verifies the profile-picture boundary: exactly 2 MB with a valid extension is accepted.</summary>
        [Fact]
        public void CanSetProfilePicture_ExactlyAt2MBLimit_ReturnsSuccess()
        {
            Assert.True(_sut.CanSetProfilePicture(ProfilePicLimit, ".png").IsSuccess);
        }

        /// <summary>Verifies the profile-picture boundary: one byte above 2 MB is rejected.</summary>
        [Fact]
        public void CanSetProfilePicture_OneByteAbove2MBLimit_ReturnsFailure()
        {
            var result = _sut.CanSetProfilePicture(ProfilePicLimit + 1, ".png");

            Assert.True(result.IsFailure);
            Assert.Contains("2MB", result.Error);
        }

        /// <summary>Verifies that every allowed profile-picture extension is accepted.</summary>
        [Theory]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".png")]
        public void CanSetProfilePicture_WithAllowedExtension_ReturnsSuccess(string extension)
        {
            Assert.True(_sut.CanSetProfilePicture(1024, extension).IsSuccess);
        }

        /// <summary>Verifies that a GIF, allowed for posts, is not allowed for profile pictures.</summary>
        [Fact]
        public void CanSetProfilePicture_WithGifExtension_ReturnsFailure()
        {
            var result = _sut.CanSetProfilePicture(1024, ".gif");

            Assert.True(result.IsFailure);
            Assert.Contains("Invalid file format", result.Error);
        }

        /// <summary>Verifies that the size check takes precedence over the extension check.</summary>
        [Fact]
        public void CanSetProfilePicture_TooLargeAndInvalidExtension_ReportsSize()
        {
            var result = _sut.CanSetProfilePicture(ProfilePicLimit * 10, ".exe");

            Assert.True(result.IsFailure);
            Assert.Contains("too large", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a zero-byte profile picture with a valid extension is accepted.</summary>
        [Fact]
        public void CanSetProfilePicture_ZeroBytes_ReturnsSuccess()
        {
            Assert.True(_sut.CanSetProfilePicture(0, ".jpg").IsSuccess);
        }
    }
}
