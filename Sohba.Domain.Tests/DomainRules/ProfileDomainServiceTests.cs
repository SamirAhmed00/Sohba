using Sohba.Domain.Domain_Rules.Logic;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for profile rules: ownership, profile visibility, friends-list
    /// privacy settings, contact info and the username change cooldown.
    /// </summary>
    public class ProfileDomainServiceTests
    {
        private readonly ProfileDomainService _sut = new();

        private static readonly Guid User = Guid.NewGuid();
        private static readonly Guid Owner = Guid.NewGuid();

        // ---- CanUpdateProfile ----

        /// <summary>Verifies that users can update their own profile.</summary>
        [Fact]
        public void CanUpdateProfile_WhenUserIsOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanUpdateProfile(Owner, Owner).IsSuccess);
        }

        /// <summary>Verifies that a user cannot update another user's profile.</summary>
        [Fact]
        public void CanUpdateProfile_WhenUserIsNotOwner_ReturnsFailure()
        {
            var result = _sut.CanUpdateProfile(User, Owner);

            Assert.True(result.IsFailure);
            Assert.Contains("your own profile", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanViewProfile ----

        /// <summary>Verifies that blocking prevents profile viewing even when the viewer is the owner.</summary>
        [Fact]
        public void CanViewProfile_WhenBlocked_ViewingIsDeniedEvenForOwner()
        {
            var result = _sut.CanViewProfile(Owner, Owner, isPrivate: false, isFriend: false, isBlocked: true);

            Assert.True(result.IsFailure);
            Assert.Contains("blocking", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the owner can always view their own unblocked profile.</summary>
        [Fact]
        public void CanViewProfile_WhenViewerIsOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewProfile(Owner, Owner, isPrivate: true, isFriend: false, isBlocked: false).IsSuccess);
        }

        /// <summary>Verifies that a friend can view a private profile.</summary>
        [Fact]
        public void CanViewProfile_WhenPrivateAndViewerIsFriend_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewProfile(User, Owner, isPrivate: true, isFriend: true, isBlocked: false).IsSuccess);
        }

        /// <summary>Verifies that a non-friend cannot view a private profile.</summary>
        [Fact]
        public void CanViewProfile_WhenPrivateAndViewerIsNotFriend_ReturnsFailure()
        {
            var result = _sut.CanViewProfile(User, Owner, isPrivate: true, isFriend: false, isBlocked: false);

            Assert.True(result.IsFailure);
            Assert.Contains("private", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that anyone can view a public profile.</summary>
        [Fact]
        public void CanViewProfile_WhenPublicAccount_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewProfile(User, Owner, isPrivate: false, isFriend: false, isBlocked: false).IsSuccess);
        }

        // ---- CanViewFriendsList ----

        /// <summary>Verifies that the owner can always view their own friends list.</summary>
        [Fact]
        public void CanViewFriendsList_WhenViewerIsOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewFriendsList(Owner, Owner, "Private", isFriend: false).IsSuccess);
        }

        /// <summary>Verifies that a Private friends list is hidden from everyone except the owner.</summary>
        [Fact]
        public void CanViewFriendsList_WhenSettingIsPrivate_ReturnsFailure()
        {
            var result = _sut.CanViewFriendsList(User, Owner, "Private", isFriend: true);

            Assert.True(result.IsFailure);
            Assert.Contains("private", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a FriendsOnly list is visible to a friend.</summary>
        [Fact]
        public void CanViewFriendsList_WhenFriendsOnlyAndViewerIsFriend_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewFriendsList(User, Owner, "FriendsOnly", isFriend: true).IsSuccess);
        }

        /// <summary>Verifies that a FriendsOnly list is hidden from a non-friend.</summary>
        [Fact]
        public void CanViewFriendsList_WhenFriendsOnlyAndViewerIsNotFriend_ReturnsFailure()
        {
            var result = _sut.CanViewFriendsList(User, Owner, "FriendsOnly", isFriend: false);

            Assert.True(result.IsFailure);
            Assert.Contains("friends", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a Public friends list is visible to anyone.</summary>
        [Fact]
        public void CanViewFriendsList_WhenSettingIsPublic_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewFriendsList(User, Owner, "Public", isFriend: false).IsSuccess);
        }

        /// <summary>Verifies that an unrecognized privacy setting falls through to public visibility.</summary>
        [Fact]
        public void CanViewFriendsList_WithUnknownSetting_BehavesAsPublic()
        {
            Assert.True(_sut.CanViewFriendsList(User, Owner, "SomeUnknownSetting", isFriend: false).IsSuccess);
        }

        // ---- CanViewContactInfo ----

        /// <summary>Verifies that the owner can view their own contact info.</summary>
        [Fact]
        public void CanViewContactInfo_WhenViewerIsOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewContactInfo(Owner, Owner, isFriend: false).IsSuccess);
        }

        /// <summary>Verifies that only friends can view contact information.</summary>
        [Fact]
        public void CanViewContactInfo_WhenViewerIsNotFriend_ReturnsFailure()
        {
            var result = _sut.CanViewContactInfo(User, Owner, isFriend: false);

            Assert.True(result.IsFailure);
            Assert.Contains("friends", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a friend can view contact information.</summary>
        [Fact]
        public void CanViewContactInfo_WhenViewerIsFriend_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewContactInfo(User, Owner, isFriend: true).IsSuccess);
        }

        // ---- CanChangeUsername ----

        /// <summary>Verifies that a first-time username change is allowed when never changed before.</summary>
        [Fact]
        public void CanChangeUsername_WhenNeverChanged_ReturnsSuccess()
        {
            Assert.True(_sut.CanChangeUsername(lastChangedDate: null, daysLimit: 30).IsSuccess);
        }

        /// <summary>Verifies the cooldown boundary: one second inside the cooldown window is rejected.</summary>
        [Fact]
        public void CanChangeUsername_WithinCooldownWindow_ReturnsFailure()
        {
            var lastChanged = DateTime.UtcNow.AddDays(-30).AddSeconds(1);

            var result = _sut.CanChangeUsername(lastChanged, daysLimit: 30);

            Assert.True(result.IsFailure);
            Assert.Contains("days", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the cooldown boundary: one second past the cooldown window is allowed.</summary>
        [Fact]
        public void CanChangeUsername_JustPastCooldownWindow_ReturnsSuccess()
        {
            var lastChanged = DateTime.UtcNow.AddDays(-30).AddSeconds(-1);

            Assert.True(_sut.CanChangeUsername(lastChanged, daysLimit: 30).IsSuccess);
        }

        /// <summary>Verifies that a change well inside the cooldown window is rejected.</summary>
        [Fact]
        public void CanChangeUsername_EarlyInsideCooldown_ReturnsFailure()
        {
            var lastChanged = DateTime.UtcNow.AddDays(-25);

            Assert.True(_sut.CanChangeUsername(lastChanged, daysLimit: 30).IsFailure);
        }

        /// <summary>Verifies that a zero-day cooldown allows immediate re-change.</summary>
        [Fact]
        public void CanChangeUsername_WithZeroDayLimit_ReturnsSuccess()
        {
            var lastChanged = DateTime.UtcNow.AddHours(-1);

            Assert.True(_sut.CanChangeUsername(lastChanged, daysLimit: 0).IsSuccess);
        }
    }
}
