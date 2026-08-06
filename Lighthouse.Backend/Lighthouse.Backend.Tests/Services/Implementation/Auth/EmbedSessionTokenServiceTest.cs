using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    /// <summary>
    /// Epic 5146 slice 01 (#5692) — ADR-137. The edges the journey and container tests cannot reach
    /// cheaply: the outcome window's closing instant, the malformed-token guard, a grant row carrying
    /// no digest, and the two lifetimes an unconfigured instance falls back to.
    /// </summary>
    [TestFixture]
    public class EmbedSessionTokenServiceTest
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

        private const string Nonce = "a-handshake-nonce";

        private Mock<IEmbedSessionTokenRepository> repository = null!;
        private EmbedConfiguration configuration = null!;
        private FakeTimeProvider timeProvider = null!;

        [SetUp]
        public void SetUp()
        {
            repository = new Mock<IEmbedSessionTokenRepository>();
            configuration = new EmbedConfiguration();
            timeProvider = new FakeTimeProvider(FixedNow);
        }

        [Test]
        [TestCase((string?)null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task ConsumeHandshake_WithoutANonce_IsUnresolvedWithoutReadingTheStore(string? blankNonce)
        {
            var outcome = await CreateSubject().ConsumeHandshakeAsync(blankNonce, TestContext.CurrentContext.CancellationToken);

            Assert.That(outcome, Is.EqualTo(EmbedHandshakeOutcome.Unresolved));
            repository.Verify(
                store => store.FindByHandshakeNonceHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task ConsumeHandshake_ForAnOutcomeWhoseWindowClosesAtThisInstant_IsUnresolved()
        {
            repository
                .Setup(store => store.FindByHandshakeNonceHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EmbedSessionToken
                {
                    TokenId = "token-id",
                    SecretHash = "a-digest",
                    Subject = "viewer",
                    CreatedAt = FixedNow.UtcDateTime.AddMinutes(-5),
                    ExpiresAt = FixedNow.UtcDateTime,
                });
            repository
                .Setup(store => store.TryConsumeHandshakeGrantAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var outcome = await CreateSubject().ConsumeHandshakeAsync(Nonce, TestContext.CurrentContext.CancellationToken);

            Assert.That(outcome, Is.EqualTo(EmbedHandshakeOutcome.Unresolved),
                "D45: the window is closed at the instant it expires, so an outcome reaching this poll one "
                + "tick late is indistinguishable from one that never existed");
        }

        [Test]
        [TestCase((string?)null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("no-separator-at-all")]
        [TestCase("three.parts.here")]
        [TestCase(".secret-only")]
        [TestCase("token-id-only.")]
        public async Task Redeem_WithAMalformedToken_IsRefusedWithoutReadingTheStore(string? malformedToken)
        {
            var redemption = await CreateSubject().RedeemAsync(malformedToken, TestContext.CurrentContext.CancellationToken);

            Assert.That(redemption.Succeeded, Is.False);
            repository.Verify(
                store => store.FindByTokenIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "a token that is not two non-empty halves names no row; looking one up anyway turns the "
                + "store into a timing oracle for token ids");
        }

        [Test]
        public async Task Redeem_ForARowCarryingNoDigest_IsRefused()
        {
            repository
                .Setup(store => store.FindByTokenIdAsync("token-id", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EmbedSessionToken
                {
                    TokenId = "token-id",
                    SecretHash = null,
                    Subject = "viewer",
                    CreatedAt = FixedNow.UtcDateTime,
                    ExpiresAt = FixedNow.UtcDateTime.AddMinutes(1),
                });
            repository
                .Setup(store => store.TryMarkRedeemedAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var redemption = await CreateSubject().RedeemAsync("token-id.any-secret", TestContext.CurrentContext.CancellationToken);

            Assert.That(redemption.Succeeded, Is.False,
                "a row with no digest matches no secret; treating the absent hash as a match signs in "
                + "whoever guesses the token id");
        }

        [Test]
        [TestCase(900, 900)]
        [TestCase(0, EmbedConfiguration.DefaultHandshakeOutcomeLifetimeSeconds)]
        public async Task RecordHandshakeGrant_ExpiresAfterTheResolvedOutcomeLifetime(
            int configuredSeconds, int expectedSeconds)
        {
            configuration.HandshakeOutcomeLifetimeSeconds = configuredSeconds;

            EmbedSessionToken? recorded = null;
            repository
                .Setup(store => store.AddAsync(It.IsAny<EmbedSessionToken>(), It.IsAny<CancellationToken>()))
                .Callback<EmbedSessionToken, CancellationToken>((token, _) => recorded = token)
                .Returns(Task.CompletedTask);

            await CreateSubject().RecordHandshakeGrantAsync("viewer", Nonce, TestContext.CurrentContext.CancellationToken);

            Assert.That(recorded?.ExpiresAt, Is.EqualTo(FixedNow.UtcDateTime.AddSeconds(expectedSeconds)),
                "DQ-2: the outcome window is the one a human finishing a login has to fit inside, and an "
                + "unconfigured instance falls back to it rather than to zero");
        }

        private EmbedSessionTokenService CreateSubject()
        {
            var options = new Mock<IOptionsMonitor<EmbedConfiguration>>();
            options.Setup(monitor => monitor.CurrentValue).Returns(configuration);

            return new EmbedSessionTokenService(
                repository.Object,
                options.Object,
                timeProvider,
                NullLogger<EmbedSessionTokenService>.Instance);
        }
    }
}
