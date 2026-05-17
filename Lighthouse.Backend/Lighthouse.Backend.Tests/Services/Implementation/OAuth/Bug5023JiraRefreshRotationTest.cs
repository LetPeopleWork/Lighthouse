using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    // Regression test for ADO Bug #5023 — "OAuth Connection for Jira break after some time".
    //
    // Root cause: PerformRefreshAsync used to pre-encrypt the rotated tokens before save, while
    // LighthouseAppContext.EncryptSecrets ALSO encrypts Modified OAuthCredential tokens on save.
    // The result was double-encryption: stored value `Encrypt(Encrypt(plaintext))`. On the next
    // refresh one Decrypt peeled off only the outer layer and the still-encrypted inner string
    // was sent to Atlassian, which rejected it with HTTP 403 `unauthorized_client`. CompleteAsync
    // never had this bug because UpsertValidCredential assigns plaintext and lets EncryptSecrets
    // encrypt once — manual reconnect therefore always restored the connection.
    //
    // Two tests guard the contract:
    //   1. `…_EachUsesRotatedRefreshTokenAndStaysValid` — rotation chain integrity with identity
    //      crypto. The unit-level contract test. Was already GREEN against the buggy code (the
    //      bug was invisible because identity-encrypt is idempotent).
    //   2. `…_WithRoundTripCryptoBoundary_ProviderReceivesPlaintextEveryCycle` — exercises the
    //      REAL `EncryptSecrets` path against a non-identity crypto stub. This is the test that
    //      catches the double-encrypt bug: it is RED against pre-fix code and GREEN after.
    [TestFixture]
    public class Bug5023JiraRefreshRotationTest
    {
        private const int ConnectionId = 503;
        private const string ProviderKey = AuthenticationMethodKeys.JiraOAuth;
        private const string PlaintextClientId = "atlassian-client-id";
        private const string PlaintextClientSecret = "atlassian-client-secret";
        private const string EncryptedClientId = "enc-client-id";
        private const string EncryptedClientSecret = "enc-client-secret";
        private const int RotationCycles = 5;

        // Tokens issued by Atlassian are seeded with a 1-hour expiry. `EnsureFreshTokenAsync`
        // triggers a refresh once the token has less than 5 minutes (`OAuthService.RefreshWindow`)
        // until expiry — advancing 56 minutes lands inside that window and forces a refresh.
        private static readonly TimeSpan AdvanceIntoRefreshWindow = TimeSpan.FromMinutes(56);

        [Test]
        public async Task EnsureFreshTokenAsync_FiveConsecutiveRefreshes_EachUsesRotatedRefreshTokenAndStaysValid()
        {
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 17, 9, 0, 0, TimeSpan.Zero));

            var cryptoService = BuildIdentityCryptoService();

            var contextOptions = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LighthouseAppContext(
                contextOptions,
                cryptoService.Object,
                NullLogger<LighthouseAppContext>.Instance);

            SeedJiraOAuthConnectionAndCredential(context, timeProvider);

            var connectionRepository = new WorkTrackingSystemConnectionRepository(
                context, NullLogger<WorkTrackingSystemConnectionRepository>.Instance);
            var credentialRepository = new OAuthCredentialRepository(
                context, NullLogger<OAuthCredentialRepository>.Instance);

            var refreshTokensReceivedByProvider = new List<string>();
            var providerRegistry = BuildRotatingProviderRegistry(timeProvider, refreshTokensReceivedByProvider);

            var sut = new OAuthService(
                providerRegistry,
                connectionRepository,
                credentialRepository,
                cryptoService.Object,
                Mock.Of<IOAuthStateTokenIssuer>(),
                BuildServiceConfig(),
                timeProvider,
                NullLogger<OAuthService>.Instance,
                Mock.Of<IHttpContextAccessor>(),
                OAuthRefreshOptions.Default);

            for (var cycle = 1; cycle <= RotationCycles; cycle++)
            {
                timeProvider.Advance(AdvanceIntoRefreshWindow);

                var token = await sut.EnsureFreshTokenAsync(ConnectionId, CancellationToken.None);

                Assert.That(token, Is.EqualTo($"plain-at-{cycle}"),
                    $"Cycle {cycle}: caller should receive the freshly-rotated access token from Atlassian.");

                var persisted = credentialRepository.GetByPredicate(c => c.WorkTrackingSystemConnectionId == ConnectionId)!;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(persisted.RefreshToken, Is.EqualTo($"plain-rt-{cycle}"),
                        $"Cycle {cycle}: the persisted refresh token must equal the one Atlassian just issued.");
                    Assert.That(persisted.AccessToken, Is.EqualTo($"plain-at-{cycle}"),
                        $"Cycle {cycle}: the persisted access token must equal the one Atlassian just issued.");
                    Assert.That(persisted.Status, Is.EqualTo(OAuthCredentialStatus.Valid),
                        $"Cycle {cycle}: credential must remain Valid across the rotation chain — flipping to RefreshFailed is the bug.");
                }
            }

            var expectedRefreshTokensSentToAtlassian = new[]
            {
                "plain-rt-seed",
                "plain-rt-1",
                "plain-rt-2",
                "plain-rt-3",
                "plain-rt-4",
            };

            Assert.That(refreshTokensReceivedByProvider, Is.EqualTo(expectedRefreshTokensSentToAtlassian),
                "Each refresh call MUST send Atlassian the refresh token Atlassian issued in the PREVIOUS response. " +
                "Sending a stale refresh token is exactly the bug ADO #5023 reports: Atlassian invalidates the old " +
                "token on rotation and returns HTTP 403 unauthorized_client when it sees it again.");
        }

        [Test]
        public async Task EnsureFreshTokenAsync_FiveConsecutiveRefreshes_WithRoundTripCryptoBoundary_ProviderReceivesPlaintextEveryCycle()
        {
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 17, 9, 0, 0, TimeSpan.Zero));

            var cryptoService = BuildRoundTripCryptoService();

            var contextOptions = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LighthouseAppContext(
                contextOptions,
                cryptoService.Object,
                NullLogger<LighthouseAppContext>.Instance);

            SeedJiraConnectionWithPlaintextOptions(context, timeProvider);

            var connectionRepository = new WorkTrackingSystemConnectionRepository(
                context, NullLogger<WorkTrackingSystemConnectionRepository>.Instance);
            var credentialRepository = new OAuthCredentialRepository(
                context, NullLogger<OAuthCredentialRepository>.Instance);

            var refreshTokensReceivedByProvider = new List<string>();
            var providerRegistry = BuildRotatingProviderRegistry(timeProvider, refreshTokensReceivedByProvider);

            var sut = new OAuthService(
                providerRegistry,
                connectionRepository,
                credentialRepository,
                cryptoService.Object,
                Mock.Of<IOAuthStateTokenIssuer>(),
                BuildServiceConfig(),
                timeProvider,
                NullLogger<OAuthService>.Instance,
                Mock.Of<IHttpContextAccessor>(),
                OAuthRefreshOptions.Default);

            for (var cycle = 1; cycle <= RotationCycles; cycle++)
            {
                timeProvider.Advance(AdvanceIntoRefreshWindow);

                await sut.EnsureFreshTokenAsync(ConnectionId, CancellationToken.None);

                var lastTokenSentToAtlassian = refreshTokensReceivedByProvider[^1];

                Assert.That(lastTokenSentToAtlassian, Does.Not.StartWith(EncryptWrapPrefix),
                    $"Cycle {cycle}: the refresh token posted to Atlassian must be plaintext. " +
                    $"Receiving an `{EncryptWrapPrefix}…)` value means PerformRefreshAsync pre-encrypted " +
                    "the token before save and LighthouseAppContext.EncryptSecrets then encrypted it " +
                    "again — exactly the double-encryption bug behind ADO #5023.");
            }

            var expectedRefreshTokensSentToAtlassian = new[]
            {
                "plain-rt-seed",
                "plain-rt-1",
                "plain-rt-2",
                "plain-rt-3",
                "plain-rt-4",
            };

            Assert.That(refreshTokensReceivedByProvider, Is.EqualTo(expectedRefreshTokensSentToAtlassian),
                "Across the rotation chain, the exact plaintext refresh token Atlassian issued in " +
                "response N must be the value sent on the request for response N+1.");

            var persisted = credentialRepository.GetByPredicate(c => c.WorkTrackingSystemConnectionId == ConnectionId)!;
            Assert.That(cryptoService.Object.Decrypt(persisted.RefreshToken), Is.EqualTo("plain-rt-5"),
                "After the final rotation the at-rest refresh token must decrypt back to the most " +
                "recently issued plaintext — i.e. it was encrypted exactly once.");
        }

        private const string EncryptWrapPrefix = "ENC(";
        private const string EncryptWrapSuffix = ")";

        private static Mock<ICryptoService> BuildRoundTripCryptoService()
        {
            var cryptoService = new Mock<ICryptoService>();
            cryptoService
                .Setup(c => c.Encrypt(It.IsAny<string>()))
                .Returns<string>(value => $"{EncryptWrapPrefix}{value}{EncryptWrapSuffix}");
            cryptoService
                .Setup(c => c.Decrypt(It.IsAny<string>()))
                .Returns<string>(value =>
                    value.StartsWith(EncryptWrapPrefix, StringComparison.Ordinal)
                    && value.EndsWith(EncryptWrapSuffix, StringComparison.Ordinal)
                        ? value[EncryptWrapPrefix.Length..^EncryptWrapSuffix.Length]
                        : value);
            return cryptoService;
        }

        private static void SeedJiraConnectionWithPlaintextOptions(LighthouseAppContext context, FakeTimeProvider timeProvider)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = ConnectionId,
                Name = "Jira OAuth — bug 5023 round-trip regression",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = ProviderKey,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = OAuthWorkTrackingOptionNames.ClientId,
                Value = PlaintextClientId,
                IsSecret = false,
            });
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = OAuthWorkTrackingOptionNames.ClientSecret,
                Value = PlaintextClientSecret,
                IsSecret = true,
            });
            context.WorkTrackingSystemConnections.Add(connection);

            context.OAuthCredentials.Add(new OAuthCredential
            {
                Id = 701,
                WorkTrackingSystemConnectionId = ConnectionId,
                AccessToken = "plain-at-seed",
                RefreshToken = "plain-rt-seed",
                ExpiresAt = timeProvider.GetUtcNow().AddHours(1),
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = timeProvider.GetUtcNow(),
            });

            context.SaveChanges();
        }

        private static Mock<ICryptoService> BuildIdentityCryptoService()
        {
            var cryptoService = new Mock<ICryptoService>();
            cryptoService.Setup(c => c.Encrypt(It.IsAny<string>())).Returns<string>(value => value);
            cryptoService.Setup(c => c.Decrypt(It.IsAny<string>())).Returns<string>(value => value);
            cryptoService.Setup(c => c.Decrypt(EncryptedClientId)).Returns(PlaintextClientId);
            cryptoService.Setup(c => c.Decrypt(EncryptedClientSecret)).Returns(PlaintextClientSecret);
            return cryptoService;
        }

        private static void SeedJiraOAuthConnectionAndCredential(LighthouseAppContext context, FakeTimeProvider timeProvider)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = ConnectionId,
                Name = "Jira OAuth — bug 5023 regression",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = ProviderKey,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = OAuthWorkTrackingOptionNames.ClientId,
                Value = EncryptedClientId,
                IsSecret = false,
            });
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = OAuthWorkTrackingOptionNames.ClientSecret,
                Value = EncryptedClientSecret,
                IsSecret = true,
            });
            context.WorkTrackingSystemConnections.Add(connection);

            context.OAuthCredentials.Add(new OAuthCredential
            {
                Id = 700,
                WorkTrackingSystemConnectionId = ConnectionId,
                AccessToken = "plain-at-seed",
                RefreshToken = "plain-rt-seed",
                ExpiresAt = timeProvider.GetUtcNow().AddHours(1),
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = timeProvider.GetUtcNow(),
            });

            context.SaveChanges();
        }

        private static IOAuthProviderRegistry BuildRotatingProviderRegistry(
            FakeTimeProvider timeProvider,
            List<string> refreshTokensReceivedByProvider)
        {
            var providerMock = new Mock<IOAuthProvider>();
            providerMock.SetupGet(p => p.ProviderKey).Returns(ProviderKey);

            var cycle = 0;
            providerMock
                .Setup(p => p.RefreshTokenAsync(It.IsAny<OAuthRefreshContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OAuthRefreshContext ctx, CancellationToken _) =>
                {
                    refreshTokensReceivedByProvider.Add(ctx.RefreshToken);
                    cycle++;
                    return new OAuthTokens(
                        $"plain-at-{cycle}",
                        $"plain-rt-{cycle}",
                        timeProvider.GetUtcNow().AddHours(1));
                });

            var registryMock = new Mock<IOAuthProviderRegistry>();
            registryMock.Setup(r => r.GetByKey(ProviderKey)).Returns(providerMock.Object);
            return registryMock.Object;
        }

        private static IServiceConfig BuildServiceConfig()
        {
            var serviceConfig = new Mock<IServiceConfig>();
            serviceConfig.SetupGet(c => c.BaseUrl).Returns("https://lighthouse.example.com");
            return serviceConfig.Object;
        }
    }
}
