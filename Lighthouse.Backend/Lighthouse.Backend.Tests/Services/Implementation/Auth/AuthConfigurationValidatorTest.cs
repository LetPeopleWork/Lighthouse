using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    [TestFixture]
    public class AuthConfigurationValidatorTest
    {
        [Test]
        public void Validate_AuthDisabled_ReturnsValid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: false);

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_AuthDisabled_WithEmptyAuthority_ReturnsValid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: false, authority: "");

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_AuthEnabled_WithValidConfig_ReturnsValid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true);

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_AuthEnabled_MissingAuthority_ReturnsInvalid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true, authority: "");

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorReason, Does.Contain("Authority"));
        }

        [Test]
        public void Validate_AuthEnabled_MissingClientId_ReturnsInvalid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true, clientId: "");

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorReason, Does.Contain("ClientId"));
        }

        [Test]
        public void Validate_AuthEnabled_WhitespaceAuthority_ReturnsInvalid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true, authority: "   ");

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorReason, Does.Contain("Authority"));
        }

        [Test]
        public void Validate_AuthEnabled_WhitespaceClientId_ReturnsInvalid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true, clientId: "   ");

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorReason, Does.Contain("ClientId"));
        }

        [Test]
        public void Validate_AuthEnabled_InvalidAuthorityUrl_ReturnsInvalid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true, authority: "not-a-url");

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorReason, Does.Contain("Authority"));
        }

        [Test]
        public void Validate_AuthEnabled_HttpAuthorityUrl_ReturnsInvalid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true, authority: "http://idp.example.com");

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorReason, Does.Contain("HTTPS"));
        }

        [Test]
        public void Validate_AuthEnabled_HttpsAuthorityUrl_ReturnsValid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true, authority: "https://idp.example.com");

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_AuthEnabled_EmptyScopes_ReturnsInvalid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true, scopes: []);

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorReason, Does.Contain("Scopes"));
        }

        [Test]
        public void Validate_AuthEnabled_MissingOpenIdScope_ReturnsInvalid()
        {
            var validator = CreateSubject();
            var config = CreateConfig(enabled: true, scopes: ["profile", "email"]);

            var result = validator.Validate(config);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorReason, Does.Contain("openid"));
        }

        private static AuthConfigurationValidator CreateSubject()
        {
            return new AuthConfigurationValidator();
        }

        private static AuthenticationConfiguration CreateConfig(
            bool enabled = true,
            string authority = "https://idp.example.com",
            string clientId = "lighthouse-client",
            string clientSecret = "secret",
            string? callbackPath = null,
            IReadOnlyList<string>? scopes = null)
        {
            return new AuthenticationConfiguration
            {
                Enabled = enabled,
                Authority = authority,
                ClientId = clientId,
                ClientSecret = clientSecret,
                CallbackPath = callbackPath ?? "/api/auth/callback",
                Scopes = scopes ?? ["openid", "profile", "email"],
            };
        }
    }
}
