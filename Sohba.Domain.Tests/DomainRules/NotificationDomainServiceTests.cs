using Sohba.Domain.Domain_Rules.Logic;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for notification self-echo suppression, 15-minute bundling
    /// window and per-owner notification management.
    /// </summary>
    public class NotificationDomainServiceTests
    {
        private readonly NotificationDomainService _sut = new();

        private static readonly Guid Actor = Guid.NewGuid();
        private static readonly Guid Owner = Guid.NewGuid();

        // ---- ShouldSendNotification ----

        /// <summary>Verifies that a notification is sent when the actor differs from the owner.</summary>
        [Fact]
        public void ShouldSendNotification_WhenActorDiffersFromOwner_ReturnsTrue()
        {
            Assert.True(_sut.ShouldSendNotification(Actor, Owner));
        }

        /// <summary>Verifies that no notification is sent when a user triggers their own notification.</summary>
        [Fact]
        public void ShouldSendNotification_WhenActorIsOwner_ReturnsFalse()
        {
            Assert.False(_sut.ShouldSendNotification(Owner, Owner));
        }

        // ---- ShouldBundleNotifications ----

        /// <summary>Verifies that a notification is bundled while inside the 15-minute window.</summary>
        [Fact]
        public void ShouldBundleNotifications_Within15MinuteWindow_ReturnsTrue()
        {
            var lastSentAt = DateTime.UtcNow.AddMinutes(-10);

            Assert.True(_sut.ShouldBundleNotifications(Guid.NewGuid(), "like", lastSentAt));
        }

        /// <summary>Verifies that a notification is not bundled one second beyond the 15-minute window.</summary>
        [Fact]
        public void ShouldBundleNotifications_JustBeyond15MinuteWindow_ReturnsFalse()
        {
            var lastSentAt = DateTime.UtcNow.AddMinutes(-15).AddSeconds(-1);

            Assert.False(_sut.ShouldBundleNotifications(Guid.NewGuid(), "like", lastSentAt));
        }

        /// <summary>Verifies that no bundling occurs when no similar notification was sent before.</summary>
        [Fact]
        public void ShouldBundleNotifications_WhenNeverSent_ReturnsFalse()
        {
            var lastSentAt = DateTime.MinValue;

            Assert.False(_sut.ShouldBundleNotifications(Guid.NewGuid(), "like", lastSentAt));
        }

        /// <summary>Verifies that a future lastSentAt timestamp still bundles (clock-order tolerance).</summary>
        [Fact]
        public void ShouldBundleNotifications_WhenLastSentInFuture_ReturnsTrue()
        {
            var lastSentAt = DateTime.UtcNow.AddMinutes(5);

            Assert.True(_sut.ShouldBundleNotifications(Guid.NewGuid(), "like", lastSentAt));
        }

        // ---- CanMarkAsRead ----

        /// <summary>Verifies that a user can mark their own notification as read.</summary>
        [Fact]
        public void CanMarkAsRead_WhenUserOwnsNotification_ReturnsSuccess()
        {
            Assert.True(_sut.CanMarkAsRead(Owner, Owner).IsSuccess);
        }

        /// <summary>Verifies that a user cannot manage another user's notifications.</summary>
        [Fact]
        public void CanMarkAsRead_WhenUserIsNotOwner_ReturnsFailure()
        {
            var result = _sut.CanMarkAsRead(Actor, Owner);

            Assert.True(result.IsFailure);
            Assert.Contains("only manage your own", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }
}
