using System.Globalization;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    [TestFixture]
    public class OAuthStateTokenIssuerTest
    {
        private const string ValidSecret = "uH2VbF5hOW0/huLOH1Q2L0g+P3J9dG43cknQK7t9R5M=";

        private static OAuthStateTokenIssuer CreateSubject(
            string secret = ValidSecret,
            TimeProvider? timeProvider = null)
        {
            var serviceConfig = new Mock<IServiceConfig>();
            serviceConfig.SetupGet(c => c.OAuthStateSecret).Returns(secret);
            serviceConfig.SetupGet(c => c.BaseUrl).Returns(string.Empty);

            return new OAuthStateTokenIssuer(serviceConfig.Object, timeProvider ?? TimeProvider.System);
        }

        [Test]
        public void Verify_RoundTrip_ReturnsSameClaims()
        {
            var subject = CreateSubject();

            var token = subject.Issue(connectionId: 42, providerKey: "jira.oauth");
            var claims = subject.Verify(token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(claims.ConnectionId, Is.EqualTo(42));
                Assert.That(claims.ProviderKey, Is.EqualTo("jira.oauth"));
            }
        }

        [Test]
        public void Verify_TamperedPayload_ThrowsInvalidTokenException()
        {
            var subject = CreateSubject();

            var token = subject.Issue(connectionId: 42, providerKey: "jira.oauth");
            var tampered = FlipMiddleCharacter(token);

            Assert.Throws<OAuthStateTokenInvalidException>(() => subject.Verify(tampered));
        }

        [Test]
        public void Verify_ExpiredToken_ThrowsExpiredTokenException()
        {
            var startInstant = DateTimeOffset.Parse("2026-05-14T12:00:00Z", CultureInfo.InvariantCulture);
            var fakeTime = new FakeTimeProvider(startInstant);
            var subject = CreateSubject(timeProvider: fakeTime);

            var token = subject.Issue(connectionId: 7, providerKey: "ado.oauth");
            fakeTime.Advance(TimeSpan.FromMinutes(16));

            Assert.Throws<OAuthStateTokenExpiredException>(() => subject.Verify(token));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void Constructor_MissingSecret_ThrowsInvalidOperationException(string? secret)
        {
            var serviceConfig = new Mock<IServiceConfig>();
            serviceConfig.SetupGet(c => c.OAuthStateSecret).Returns(secret!);

            Assert.Throws<InvalidOperationException>(() =>
                new OAuthStateTokenIssuer(serviceConfig.Object, TimeProvider.System));
        }

        private static string FlipMiddleCharacter(string token)
        {
            var middleIndex = token.Length / 2;
            var middleChar = token[middleIndex];
            var replacement = middleChar == 'A' ? 'B' : 'A';
            return string.Concat(token.AsSpan(0, middleIndex), replacement.ToString(), token.AsSpan(middleIndex + 1));
        }
    }
}
