using Sohba.Domain.Domain_Rules.Logic;
using Sohba.Domain.Enums;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for story creation limits, visibility, replies and the
    /// 24-hour expiration rule.
    /// </summary>
    public class StoryDomainServiceTests
    {
        private readonly StoryDomainService _sut = new();

        private static readonly Guid Creator = Guid.NewGuid();
        private static readonly Guid Viewer = Guid.NewGuid();

        // ---- CanCreateStory ----

        /// <summary>Verifies that a story with media can be created below the daily limit.</summary>
        [Fact]
        public void CanCreateStory_WhenHasMediaAndBelowLimit_ReturnsSuccess()
        {
            var result = _sut.CanCreateStory(Creator, hasMedia: true, dailyStoryLimit: 5, currentStoryCount: 4);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a story without media is rejected.</summary>
        [Fact]
        public void CanCreateStory_WhenNoMedia_ReturnsFailure()
        {
            var result = _sut.CanCreateStory(Creator, hasMedia: false, dailyStoryLimit: 5, currentStoryCount: 0);

            Assert.True(result.IsFailure);
            Assert.Contains("media", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the daily-limit boundary: exactly at the limit is rejected.</summary>
        [Fact]
        public void CanCreateStory_WhenAtDailyLimit_ReturnsFailure()
        {
            var result = _sut.CanCreateStory(Creator, hasMedia: true, dailyStoryLimit: 5, currentStoryCount: 5);

            Assert.True(result.IsFailure);
            Assert.Contains("daily limit", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the daily-limit boundary: one above the limit is also rejected.</summary>
        [Fact]
        public void CanCreateStory_WhenAboveDailyLimit_ReturnsFailure()
        {
            Assert.True(_sut.CanCreateStory(Creator, hasMedia: true, dailyStoryLimit: 5, currentStoryCount: 6).IsFailure);
        }

        /// <summary>Verifies that a zero limit blocks the very first story.</summary>
        [Fact]
        public void CanCreateStory_WhenZeroLimit_ReturnsFailure()
        {
            Assert.True(_sut.CanCreateStory(Creator, hasMedia: true, dailyStoryLimit: 0, currentStoryCount: 0).IsFailure);
        }

        /// <summary>Verifies that the missing-media rule takes precedence over the daily limit.</summary>
        [Fact]
        public void CanCreateStory_WhenNoMediaAndAtLimit_ReportsMissingMedia()
        {
            var result = _sut.CanCreateStory(Creator, hasMedia: false, dailyStoryLimit: 5, currentStoryCount: 5);

            Assert.True(result.IsFailure);
            Assert.Contains("media", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanViewStory ----

        /// <summary>Verifies that the creator can always view their own active story.</summary>
        [Fact]
        public void CanViewStory_WhenViewerIsCreator_ReturnsSuccess()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-5);

            var result = _sut.CanViewStory(Creator, Creator, StoryPrivacy.Public, isCreatorAccountPrivate: false, isFriend: false, createdAt);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that an expired story cannot be viewed, even by its creator.</summary>
        [Fact]
        public void CanViewStory_WhenStoryIsExpired_ReturnsFailure_EvenForCreator()
        {
            var createdAt = DateTime.UtcNow.AddHours(-25);

            var result = _sut.CanViewStory(Creator, Creator, StoryPrivacy.Public, isCreatorAccountPrivate: false, isFriend: false, createdAt);

            Assert.True(result.IsFailure);
            Assert.Contains("expired", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a friend can view a story of a private account.</summary>
        [Fact]
        public void CanViewStory_WhenPrivateAccountAndViewerIsFriend_ReturnsSuccess()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-5);

            var result = _sut.CanViewStory(Viewer, Creator, StoryPrivacy.Public, isCreatorAccountPrivate: true, isFriend: true, createdAt);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a non-friend cannot view a story of a private account.</summary>
        [Fact]
        public void CanViewStory_WhenPrivateAccountAndViewerIsNotFriend_ReturnsFailure()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-5);

            var result = _sut.CanViewStory(Viewer, Creator, StoryPrivacy.Public, isCreatorAccountPrivate: true, isFriend: false, createdAt);

            Assert.True(result.IsFailure);
            Assert.Contains("private", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the private-account rule takes precedence over story privacy settings.</summary>
        [Fact]
        public void CanViewStory_WhenPrivateAccountAndStoryIsPublic_NonFriendStillDenied()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-5);

            var result = _sut.CanViewStory(Viewer, Creator, StoryPrivacy.Public, isCreatorAccountPrivate: true, isFriend: false, createdAt);

            Assert.True(result.IsFailure);
        }

        /// <summary>Verifies that a public story is visible to anyone while the creator account is public.</summary>
        [Fact]
        public void CanViewStory_WhenPublicStoryAndPublicAccount_ReturnsSuccess()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-5);

            var result = _sut.CanViewStory(Viewer, Creator, StoryPrivacy.Public, isCreatorAccountPrivate: false, isFriend: false, createdAt);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a friends-only story is visible to a friend.</summary>
        [Fact]
        public void CanViewStory_WhenFriendsOnlyAndViewerIsFriend_ReturnsSuccess()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-5);

            var result = _sut.CanViewStory(Viewer, Creator, StoryPrivacy.FriendsOnly, isCreatorAccountPrivate: false, isFriend: true, createdAt);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a friends-only story is hidden from a non-friend.</summary>
        [Fact]
        public void CanViewStory_WhenFriendsOnlyAndViewerIsNotFriend_ReturnsFailure()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-5);

            var result = _sut.CanViewStory(Viewer, Creator, StoryPrivacy.FriendsOnly, isCreatorAccountPrivate: false, isFriend: false, createdAt);

            Assert.True(result.IsFailure);
        }

        // ---- 24-hour expiration boundary (implementation: createdAt + 24h < UtcNow) ----

        /// <summary>Verifies that a story remains viewable one second before the 24-hour expiration boundary.</summary>
        [Fact]
        public void IsStoryExpired_OneSecondBefore24Hours_ReturnsFalse()
        {
            var createdAt = DateTime.UtcNow.AddHours(-24).AddSeconds(1);

            Assert.False(_sut.IsStoryExpired(createdAt));
        }

        /// <summary>Verifies that a story is expired one second after the 24-hour lifetime boundary.</summary>
        [Fact]
        public void IsStoryExpired_OneSecondAfter24Hours_ReturnsTrue()
        {
            var createdAt = DateTime.UtcNow.AddHours(-24).AddSeconds(-1);

            Assert.True(_sut.IsStoryExpired(createdAt));
        }

        /// <summary>Verifies that a fresh story created seconds ago is not expired.</summary>
        [Fact]
        public void IsStoryExpired_StoryJustCreated_ReturnsFalse()
        {
            Assert.False(_sut.IsStoryExpired(DateTime.UtcNow.AddSeconds(-2)));
        }

        /// <summary>Verifies that the expiration rule dominates all privacy rules in CanViewStory.</summary>
        [Fact]
        public void CanViewStory_WhenExpiredAndPublic_ReturnsExpiredFailure()
        {
            var createdAt = DateTime.UtcNow.AddHours(-25);

            var result = _sut.CanViewStory(Viewer, Creator, StoryPrivacy.Public, isCreatorAccountPrivate: false, isFriend: false, createdAt);

            Assert.True(result.IsFailure);
            Assert.Contains("expired", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a story exactly inside the visible window but friends-only remains hidden from strangers.</summary>
        [Fact]
        public void CanViewStory_WhenFriendsOnlyAndNonFriendInsideWindow_ReturnsFailure()
        {
            var createdAt = DateTime.UtcNow.AddHours(-23);

            var result = _sut.CanViewStory(Viewer, Creator, StoryPrivacy.FriendsOnly, isCreatorAccountPrivate: false, isFriend: false, createdAt);

            Assert.True(result.IsFailure);
        }

        // ---- CanReplyToStory ----

        /// <summary>Verifies that replies are allowed on an active story that accepts replies.</summary>
        [Fact]
        public void CanReplyToStory_WhenActiveAndAcceptingReplies_ReturnsSuccess()
        {
            Assert.True(_sut.CanReplyToStory(Viewer, isCreatorAcceptingReplies: true, isExpired: false).IsSuccess);
        }

        /// <summary>Verifies that replies to an expired story are rejected.</summary>
        [Fact]
        public void CanReplyToStory_WhenStoryIsExpired_ReturnsFailure()
        {
            var result = _sut.CanReplyToStory(Viewer, isCreatorAcceptingReplies: true, isExpired: true);

            Assert.True(result.IsFailure);
            Assert.Contains("expired", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that replies are rejected when the creator disabled replies.</summary>
        [Fact]
        public void CanReplyToStory_WhenRepliesDisabled_ReturnsFailure()
        {
            var result = _sut.CanReplyToStory(Viewer, isCreatorAcceptingReplies: false, isExpired: false);

            Assert.True(result.IsFailure);
            Assert.Contains("turned off replies", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that expiration takes precedence over the replies-disabled setting.</summary>
        [Fact]
        public void CanReplyToStory_WhenExpiredAndRepliesDisabled_ReportsExpired()
        {
            var result = _sut.CanReplyToStory(Viewer, isCreatorAcceptingReplies: false, isExpired: true);

            Assert.True(result.IsFailure);
            Assert.Contains("expired", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }
}
