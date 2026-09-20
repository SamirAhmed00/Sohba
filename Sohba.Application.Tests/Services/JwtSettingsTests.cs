using Sohba.Application.Settings;

namespace Sohba.Application.Tests.Services
{
    /// <summary>
    /// Tests for JWT settings validation: required key length, issuer,
    /// audience and strictly positive lifetime values.
    /// </summary>
    public class JwtSettingsTests
    {
        private static JwtSettings CreateValidSettings() => new()
        {
            Key = "0123456789abcdef0123456789abcdef", // exactly 32 chars
            Issuer = "issuer",
            Audience = "audience",
            ExpireDays = 7,
            AccessTokenLifetimeMinutes = 60,
            RefreshTokenLifetimeDays = 14
        };

        /// <summary>Verifies that a fully valid configuration passes validation.</summary>
        [Fact]
        public void Validate_WithValidSettings_DoesNotThrow()
        {
            var settings = CreateValidSettings();

            settings.Validate();
        }

        /// <summary>Verifies the key boundary: a 32-character key is the minimum accepted length.</summary>
        [Fact]
        public void Validate_WithExactly32CharKey_Passes()
        {
            var settings = CreateValidSettings();
            settings.Key = new string('k', 32);

            settings.Validate();
        }

        /// <summary>Verifies the key boundary: a 31-character key is rejected.</summary>
        [Fact]
        public void Validate_With31CharKey_Throws()
        {
            var settings = CreateValidSettings();
            settings.Key = new string('k', 31);

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            Assert.Contains("32 characters", ex.Message);
        }

        /// <summary>Verifies that a null key is rejected.</summary>
        [Fact]
        public void Validate_WithNullKey_Throws()
        {
            var settings = CreateValidSettings();
            settings.Key = null!;

            Assert.Throws<InvalidOperationException>(() => settings.Validate());
        }

        /// <summary>Verifies that a whitespace-only key is rejected.</summary>
        [Fact]
        public void Validate_WithWhitespaceKey_Throws()
        {
            var settings = CreateValidSettings();
            settings.Key = "                                ";

            Assert.Throws<InvalidOperationException>(() => settings.Validate());
        }

        /// <summary>Verifies that an empty issuer is rejected.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WithMissingIssuer_Throws(string? issuer)
        {
            var settings = CreateValidSettings();
            settings.Issuer = issuer!;

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            Assert.Contains("Issuer", ex.Message);
        }

        /// <summary>Verifies that an empty audience is rejected.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WithMissingAudience_Throws(string? audience)
        {
            var settings = CreateValidSettings();
            settings.Audience = audience!;

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            Assert.Contains("Audience", ex.Message);
        }

        /// <summary>Verifies that a non-positive ExpireDays value is rejected.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithNonPositiveExpireDays_Throws(double expireDays)
        {
            var settings = CreateValidSettings();
            settings.ExpireDays = expireDays;

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            Assert.Contains("ExpireDays", ex.Message);
        }

        /// <summary>Verifies that a non-positive access-token lifetime is rejected.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Validate_WithNonPositiveAccessTokenLifetime_Throws(double lifetimeMinutes)
        {
            var settings = CreateValidSettings();
            settings.AccessTokenLifetimeMinutes = lifetimeMinutes;

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            Assert.Contains("AccessTokenLifetimeMinutes", ex.Message);
        }

        /// <summary>Verifies that a non-positive refresh-token lifetime is rejected.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-0.5)]
        public void Validate_WithNonPositiveRefreshTokenLifetime_Throws(double lifetimeDays)
        {
            var settings = CreateValidSettings();
            settings.RefreshTokenLifetimeDays = lifetimeDays;

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            Assert.Contains("RefreshTokenLifetimeDays", ex.Message);
        }

        /// <summary>Verifies that a small positive lifetime is accepted (boundary above zero).</summary>
        [Fact]
        public void Validate_WithSmallPositiveLifetimes_Passes()
        {
            var settings = CreateValidSettings();
            settings.ExpireDays = 0.001;
            settings.AccessTokenLifetimeMinutes = 0.001;
            settings.RefreshTokenLifetimeDays = 0.001;

            settings.Validate();
        }
    }
}
