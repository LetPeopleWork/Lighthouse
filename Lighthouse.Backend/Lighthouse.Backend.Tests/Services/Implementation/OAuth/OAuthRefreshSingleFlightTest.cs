using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    // Verifies the single-flight invariant on IOAuthService.EnsureFreshTokenAsync:
    // N concurrent callers observing an expired credential MUST trigger exactly one
    // refresh against the IOAuthProvider; all N callers receive the same fresh token.
    // Re-layered here from Playwright scenario #8 on 2026-05-14 because concurrency
    // under load is best asserted via direct service-level testing, not through an
    // E2E browser harness (where deterministically simulating concurrency is brittle).
    [TestFixture]
    public class OAuthRefreshSingleFlightTest
    {
        private const int ConnectionId = 42;
        private const string ProviderKey = "jira.oauth";
        private const string EncryptedClientId = "enc-client-id";
        private const string EncryptedClientSecret = "enc-client-secret";
        private const string PlaintextClientId = "client-id-123";
        private const string PlaintextClientSecret = "client-secret-xyz";
        private const string EncryptedAccessTokenOld = "enc-at-old";
        private const string EncryptedRefreshTokenOld = "enc-rt-old";
        private const string PlaintextRefreshTokenOld = "plain-rt-old";
        private const string PlaintextAccessTokenNew = "plain-at-new";
        private const string PlaintextRefreshTokenNew = "plain-rt-new";
        private const int ConcurrentCallers = 32;

        [Test]
        public async Task EnsureFreshTokenAsync_NConcurrentCallers_RefreshInvokedExactlyOnce()
        {
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero));

            var connection = CreateOAuthConnection(ConnectionId, ProviderKey);
            var connectionRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            connectionRepositoryMock.Setup(r => r.GetById(ConnectionId)).Returns(connection);

            var credential = new OAuthCredential
            {
                Id = 101,
                WorkTrackingSystemConnectionId = ConnectionId,
                AccessToken = EncryptedAccessTokenOld,
                RefreshToken = EncryptedRefreshTokenOld,
                ExpiresAt = timeProvider.GetUtcNow().AddMinutes(3),
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = timeProvider.GetUtcNow().AddMinutes(-10),
            };

            var credentialRepositoryMock = new Mock<IRepository<OAuthCredential>>();
            credentialRepositoryMock
                .Setup(r => r.GetByPredicate(It.IsAny<Func<OAuthCredential, bool>>()))
                .Returns<Func<OAuthCredential, bool>>(predicate => predicate(credential) ? credential : null);

            var cryptoServiceMock = new Mock<ICryptoService>();
            cryptoServiceMock.Setup(c => c.Decrypt(EncryptedClientId)).Returns(PlaintextClientId);
            cryptoServiceMock.Setup(c => c.Decrypt(EncryptedClientSecret)).Returns(PlaintextClientSecret);
            cryptoServiceMock.Setup(c => c.Decrypt(EncryptedRefreshTokenOld)).Returns(PlaintextRefreshTokenOld);
            cryptoServiceMock.Setup(c => c.Decrypt(PlaintextAccessTokenNew)).Returns(PlaintextAccessTokenNew);

            var serviceConfigMock = new Mock<IServiceConfig>();
            serviceConfigMock.SetupGet(c => c.BaseUrl).Returns("https://lighthouse.example.com");

            var refreshedTokens = new OAuthTokens(
                PlaintextAccessTokenNew,
                PlaintextRefreshTokenNew,
                timeProvider.GetUtcNow().AddHours(1));

            var countingProvider = new CountingStubOAuthProvider(
                serviceConfigMock.Object,
                timeProvider,
                refreshDelay: TimeSpan.FromMilliseconds(50),
                refreshedTokens: refreshedTokens);

            var providerRegistryMock = new Mock<IOAuthProviderRegistry>();
            providerRegistryMock.Setup(r => r.GetByKey(ProviderKey)).Returns(countingProvider);

            var stateTokenIssuerMock = new Mock<IOAuthStateTokenIssuer>();

            var sut = new OAuthService(
                providerRegistryMock.Object,
                connectionRepositoryMock.Object,
                credentialRepositoryMock.Object,
                cryptoServiceMock.Object,
                stateTokenIssuerMock.Object,
                serviceConfigMock.Object,
                timeProvider,
                NullLogger<OAuthService>.Instance,
                Mock.Of<IHttpContextAccessor>(),
                OAuthRefreshOptions.Default);

            var tasks = Enumerable.Range(0, ConcurrentCallers)
                .Select(_ => Task.Run(() => sut.EnsureFreshTokenAsync(ConnectionId, CancellationToken.None)))
                .ToArray();

            var tokens = await Task.WhenAll(tasks);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(countingProvider.RefreshInvocationCount, Is.EqualTo(1));
                Assert.That(tokens.Distinct().Count(), Is.EqualTo(1));
                Assert.That(tokens[0], Is.EqualTo(PlaintextAccessTokenNew));
            }
            credentialRepositoryMock.Verify(r => r.Update(credential), Times.Once);
            credentialRepositoryMock.Verify(r => r.Save(), Times.Once);
        }

        private static WorkTrackingSystemConnection CreateOAuthConnection(int id, string authenticationMethodKey)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = id,
                Name = "OAuth Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = authenticationMethodKey,
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
            return connection;
        }
    }
}
