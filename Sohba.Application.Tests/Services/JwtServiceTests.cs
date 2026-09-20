using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sohba.Application.Services;
using Sohba.Application.Settings;
using Sohba.Domain.Entities.UserAggregate;

namespace Sohba.Application.Tests.Services
{
    /// <summary>
    /// Tests for JWT generation: subject/email/name claims, role claims,
    /// issuer/audience, configured lifetime and signature validity.
    /// </summary>
    public class JwtServiceTests
    {
        private readonly JwtSettings _settings = new()
        {
            Key = "unit-test-signing-key-that-is-long-enough!!",
            Issuer = "sohba-test-issuer",
            Audience = "sohba-test-audience",
            AccessTokenLifetimeMinutes = 30
        };

        private JwtService CreateSut() => new(Options.Create(_settings));

        private static User CreateUser()
        {
            return new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserName = "testuser",
                Email = "user@test.local",
                Name = "Test User"
            };
        }

        private static JwtSecurityToken ReadToken(string token) =>
            new JwtSecurityTokenHandler().ReadJwtToken(token);

        /// <summary>Verifies that a generated token is a well-formed JWT readable by the JWT handler.</summary>
        [Fact]
        public void GenerateToken_ProducesParsableJwt()
        {
            var token = CreateSut().GenerateToken(CreateUser(), new List<string>());

            Assert.True(new JwtSecurityTokenHandler().CanReadToken(token));
        }

        /// <summary>Verifies that the subject claim carries the user's identifier.</summary>
        [Fact]
        public void GenerateToken_ContainsSubjectClaimWithUserId()
        {
            var user = CreateUser();

            var jwt = ReadToken(CreateSut().GenerateToken(user, new List<string>()));

            Assert.Equal(user.Id.ToString(), jwt.Payload.Sub as string);
        }

        /// <summary>Verifies that the email claim carries the user's email address.</summary>
        [Fact]
        public void GenerateToken_ContainsEmailClaim()
        {
            var jwt = ReadToken(CreateSut().GenerateToken(CreateUser(), new List<string>()));

            Assert.Equal("user@test.local", jwt.Payload.TryGetValue("email", out var value) ? value?.ToString() : null);
        }

        /// <summary>Verifies that the name claim carries the user's display name.</summary>
        [Fact]
        public void GenerateToken_ContainsNameClaim()
        {
            var jwt = ReadToken(CreateSut().GenerateToken(CreateUser(), new List<string>()));

            Assert.Equal("Test User", jwt.Payload.TryGetValue("name", out var value) ? value?.ToString() : null);
        }

        /// <summary>Verifies that a unique jti claim is present.</summary>
        [Fact]
        public void GenerateToken_ContainsJtiClaim()
        {
            var jwt = ReadToken(CreateSut().GenerateToken(CreateUser(), new List<string>()));

            Assert.False(string.IsNullOrEmpty(jwt.Payload.Jti));
        }

        /// <summary>Verifies that each role is emitted as a distinct role claim (serialized under ClaimTypes.Role).</summary>
        [Fact]
        public void GenerateToken_ContainsAllRoleClaims()
        {
            var roles = new List<string> { "Admin", "Member" };

            var jwt = ReadToken(CreateSut().GenerateToken(CreateUser(), roles));

            var roleClaims = jwt.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .OrderBy(v => v)
                .ToArray();

            Assert.Equal(new[] { "Admin", "Member" }, roleClaims);
        }

        /// <summary>Verifies that a token without roles contains no role claim at all.</summary>
        [Fact]
        public void GenerateToken_WithNoRoles_ContainsNoRoleClaim()
        {
            var jwt = ReadToken(CreateSut().GenerateToken(CreateUser(), new List<string>()));

            Assert.DoesNotContain(jwt.Claims, c => c.Type == ClaimTypes.Role);
        }

        /// <summary>Verifies that the token issuer and audience match the configured settings.</summary>
        [Fact]
        public void GenerateToken_UsesConfiguredIssuerAndAudience()
        {
            var jwt = ReadToken(CreateSut().GenerateToken(CreateUser(), new List<string>()));

            Assert.Equal(_settings.Issuer, jwt.Issuer);
            Assert.Equal(_settings.Audience, jwt.Audiences.Single());
        }

        /// <summary>Verifies that the token lifetime matches the configured access-token lifetime (±1 minute tolerance).</summary>
        [Fact]
        public void GenerateToken_ExpiresAfterConfiguredLifetime()
        {
            var before = DateTime.UtcNow;

            var jwt = ReadToken(CreateSut().GenerateToken(CreateUser(), new List<string>()));

            var expected = before.AddMinutes(_settings.AccessTokenLifetimeMinutes);
            Assert.InRange(jwt.ValidTo, expected.AddMinutes(-1), expected.AddMinutes(1));
        }

        /// <summary>Verifies that the token is signed with HMAC-SHA256.</summary>
        [Fact]
        public void GenerateToken_UsesHmacSha256Algorithm()
        {
            var jwt = ReadToken(CreateSut().GenerateToken(CreateUser(), new List<string>()));

            Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
        }

        /// <summary>Verifies that the generated token passes full signature validation with the configured key.</summary>
        [Fact]
        public void GenerateToken_ValidatesSignatureWithConfiguredKey()
        {
            var token = CreateSut().GenerateToken(CreateUser(), new List<string>());

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, parameters, out _);

            Assert.Equal("11111111-1111-1111-1111-111111111111", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        /// <summary>Verifies that the token fails validation when signed with a different key.</summary>
        [Fact]
        public void GenerateToken_RejectsValidationWithWrongKey()
        {
            var token = CreateSut().GenerateToken(CreateUser(), new List<string>());

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-signing-key!!!")),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };

            Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(token, parameters, out _));
        }

        /// <summary>Verifies that constructing the service with a too-short signing key throws.</summary>
        [Fact]
        public void Constructor_WithShortKey_Throws()
        {
            _settings.Key = "too-short";

            Assert.Throws<InvalidOperationException>(() => CreateSut());
        }
    }
}
