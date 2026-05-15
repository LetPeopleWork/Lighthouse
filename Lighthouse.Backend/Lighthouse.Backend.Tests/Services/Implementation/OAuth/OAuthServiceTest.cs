using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    [TestFixture]
    public class OAuthServiceTest
    {
        private const int ConnectionId = 42;
        private const string ProviderKey = "jira.oauth";
        private const string EncryptedClientId = "enc-client-id";
        private const string EncryptedClientSecret = "enc-client-secret";
        private const string PlaintextClientId = "client-id-123";
        private const string PlaintextClientSecret = "client-secret-xyz";
        private const string BaseUrl = "https://lighthouse.example.com";
        private const string ValidStateToken = "valid.state.token";

        private static readonly string[] DefaultProviderScopes = ["read:jira-work"];

        private Mock<IOAuthProviderRegistry> providerRegistryMock = null!;
        private Mock<IOAuthProvider> providerMock = null!;
        private Mock<IRepository<WorkTrackingSystemConnection>> connectionRepositoryMock = null!;
        private Mock<IRepository<OAuthCredential>> credentialRepositoryMock = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<IOAuthStateTokenIssuer> stateTokenIssuerMock = null!;
        private Mock<IServiceConfig> serviceConfigMock = null!;
        private FakeTimeProvider timeProvider = null!;

        [SetUp]
        public void SetUp()
        {
            providerRegistryMock = new Mock<IOAuthProviderRegistry>();
            providerMock = new Mock<IOAuthProvider>();
            providerMock.SetupGet(p => p.ProviderKey).Returns(ProviderKey);
            providerMock.SetupGet(p => p.DefaultScopes).Returns(DefaultProviderScopes);
            providerRegistryMock.Setup(r => r.GetByKey(ProviderKey)).Returns(providerMock.Object);

            connectionRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            credentialRepositoryMock = new Mock<IRepository<OAuthCredential>>();

            cryptoServiceMock = new Mock<ICryptoService>();
            cryptoServiceMock.Setup(c => c.Decrypt(EncryptedClientId)).Returns(PlaintextClientId);
            cryptoServiceMock.Setup(c => c.Decrypt(EncryptedClientSecret)).Returns(PlaintextClientSecret);

            stateTokenIssuerMock = new Mock<IOAuthStateTokenIssuer>();
            serviceConfigMock = new Mock<IServiceConfig>();
            serviceConfigMock.SetupGet(c => c.BaseUrl).Returns(BaseUrl);

            timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero));
        }

        [Test]
        public async Task InitiateAsync_ResolvesProviderAndReturnsAuthorizationUrlBuiltFromFlowContext()
        {
            var connection = CreateOAuthConnection(ConnectionId, ProviderKey);
            connectionRepositoryMock.Setup(r => r.GetById(ConnectionId)).Returns(connection);
            stateTokenIssuerMock.Setup(i => i.Issue(ConnectionId, ProviderKey)).Returns(ValidStateToken);

            OAuthFlowContext? capturedContext = null;
            var expectedAuthorizationUrl = new Uri("https://auth.atlassian.com/authorize?client_id=client-id-123");
            providerMock
                .Setup(p => p.BuildAuthorizationUrl(It.IsAny<OAuthFlowContext>()))
                .Callback<OAuthFlowContext>(ctx => capturedContext = ctx)
                .Returns(expectedAuthorizationUrl);

            var sut = CreateService();

            var authorizationUrl = await sut.InitiateAsync(ConnectionId, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authorizationUrl, Is.EqualTo(expectedAuthorizationUrl));
                Assert.That(capturedContext, Is.Not.Null);
                Assert.That(capturedContext!.ConnectionId, Is.EqualTo(ConnectionId));
                Assert.That(capturedContext.ProviderKey, Is.EqualTo(ProviderKey));
                Assert.That(capturedContext.ClientId, Is.EqualTo(PlaintextClientId));
                Assert.That(capturedContext.ClientSecret, Is.EqualTo(PlaintextClientSecret));
                Assert.That(capturedContext.RedirectUri, Is.EqualTo(new Uri($"{BaseUrl}/api/oauth/callback")));
                Assert.That(capturedContext.State, Is.EqualTo(ValidStateToken));
            }
        }

        [Test]
        public void InitiateAsync_ConnectionNotFound_ThrowsArgumentException()
        {
            connectionRepositoryMock.Setup(r => r.GetById(99)).Returns((WorkTrackingSystemConnection?)null);
            var sut = CreateService();

            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => sut.InitiateAsync(99, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("99"));
        }

        [Test]
        public void InitiateAsync_ProviderNotRegistered_PropagatesOAuthProviderNotFoundException()
        {
            const string unknownProviderKey = "nonexistent.oauth";
            var connection = CreateOAuthConnection(ConnectionId, unknownProviderKey);
            connectionRepositoryMock.Setup(r => r.GetById(ConnectionId)).Returns(connection);
            providerRegistryMock
                .Setup(r => r.GetByKey(unknownProviderKey))
                .Throws(new OAuthProviderNotFoundException($"No IOAuthProvider for key '{unknownProviderKey}'."));

            var sut = CreateService();

            Assert.ThrowsAsync<OAuthProviderNotFoundException>(
                () => sut.InitiateAsync(ConnectionId, CancellationToken.None));
        }

        [Test]
        public async Task CompleteAsync_VerifiesStateExchangesCodeAndPersistsCredential()
        {
            var connection = CreateOAuthConnection(ConnectionId, ProviderKey);
            connectionRepositoryMock.Setup(r => r.GetById(ConnectionId)).Returns(connection);
            stateTokenIssuerMock
                .Setup(i => i.Verify(ValidStateToken))
                .Returns(new OAuthStateClaims
                {
                    ConnectionId = ConnectionId,
                    ProviderKey = ProviderKey,
                    Nonce = "nonce",
                    ExpiresAt = timeProvider.GetUtcNow().AddMinutes(10),
                });
            var expiresAt = timeProvider.GetUtcNow().AddHours(1);
            var tokens = new OAuthTokens("at", "rt", expiresAt);
            providerMock
                .Setup(p => p.ExchangeCodeAsync("auth-code-123", It.IsAny<OAuthFlowContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokens);

            OAuthCredential? persistedCredential = null;
            credentialRepositoryMock
                .Setup(r => r.Add(It.IsAny<OAuthCredential>()))
                .Callback<OAuthCredential>(c => persistedCredential = c);

            var sut = CreateService();

            var result = await sut.CompleteAsync("auth-code-123", ValidStateToken, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ConnectionId, Is.EqualTo(ConnectionId));
                Assert.That(result.Status, Is.EqualTo(OAuthCredentialStatus.Valid));
                Assert.That(result.ErrorMessage, Is.Null);
                Assert.That(persistedCredential, Is.Not.Null);
                Assert.That(persistedCredential!.WorkTrackingSystemConnectionId, Is.EqualTo(ConnectionId));
                Assert.That(persistedCredential.AccessToken, Is.EqualTo("at"));
                Assert.That(persistedCredential.RefreshToken, Is.EqualTo("rt"));
                Assert.That(persistedCredential.ExpiresAt, Is.EqualTo(expiresAt));
                Assert.That(persistedCredential.Status, Is.EqualTo(OAuthCredentialStatus.Valid));
                Assert.That(persistedCredential.UpdatedAt, Is.EqualTo(timeProvider.GetUtcNow()));
            }
            credentialRepositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public void CompleteAsync_InvalidStateToken_PropagatesAndDoesNotPersistCredential()
        {
            stateTokenIssuerMock
                .Setup(i => i.Verify("tampered"))
                .Throws(new OAuthStateTokenInvalidException("HMAC mismatch."));
            var sut = CreateService();

            Assert.ThrowsAsync<OAuthStateTokenInvalidException>(
                () => sut.CompleteAsync("auth-code-123", "tampered", CancellationToken.None));

            credentialRepositoryMock.Verify(r => r.Add(It.IsAny<OAuthCredential>()), Times.Never);
            credentialRepositoryMock.Verify(r => r.Update(It.IsAny<OAuthCredential>()), Times.Never);
            credentialRepositoryMock.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task DisconnectAsync_ExistingCredential_SetsStatusDisconnectedAndClearsTokens()
        {
            var existing = new OAuthCredential
            {
                Id = 7,
                WorkTrackingSystemConnectionId = ConnectionId,
                AccessToken = "old-at",
                RefreshToken = "old-rt",
                ExpiresAt = timeProvider.GetUtcNow().AddHours(1),
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = timeProvider.GetUtcNow().AddDays(-1),
            };
            credentialRepositoryMock
                .Setup(r => r.GetByPredicate(It.IsAny<Func<OAuthCredential, bool>>()))
                .Returns<Func<OAuthCredential, bool>>(predicate => predicate(existing) ? existing : null);

            var sut = CreateService();

            await sut.DisconnectAsync(ConnectionId, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(existing.Status, Is.EqualTo(OAuthCredentialStatus.Disconnected));
                Assert.That(existing.AccessToken, Is.Empty);
                Assert.That(existing.RefreshToken, Is.Empty);
                Assert.That(existing.UpdatedAt, Is.EqualTo(timeProvider.GetUtcNow()));
            }
            credentialRepositoryMock.Verify(r => r.Update(existing), Times.Once);
            credentialRepositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task EnsureFreshTokenAsync_ValidCredential_ReturnsDecryptedAccessToken()
        {
            cryptoServiceMock.Setup(c => c.Decrypt("encrypted-at")).Returns("plain-at");

            var credential = new OAuthCredential
            {
                Id = 7,
                WorkTrackingSystemConnectionId = ConnectionId,
                AccessToken = "encrypted-at",
                RefreshToken = "encrypted-rt",
                ExpiresAt = timeProvider.GetUtcNow().AddHours(1),
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = timeProvider.GetUtcNow(),
            };
            credentialRepositoryMock
                .Setup(r => r.GetByPredicate(It.IsAny<Func<OAuthCredential, bool>>()))
                .Returns<Func<OAuthCredential, bool>>(predicate => predicate(credential) ? credential : null);

            var sut = CreateService();

            var token = await sut.EnsureFreshTokenAsync(ConnectionId, CancellationToken.None);

            Assert.That(token, Is.EqualTo("plain-at"));
        }

        [Test]
        public void EnsureFreshTokenAsync_NoCredential_ThrowsInvalidOperationException()
        {
            credentialRepositoryMock
                .Setup(r => r.GetByPredicate(It.IsAny<Func<OAuthCredential, bool>>()))
                .Returns((OAuthCredential?)null);

            var sut = CreateService();

            Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.EnsureFreshTokenAsync(ConnectionId, CancellationToken.None));
        }

        [Test]
        public void EnsureFreshTokenAsync_RefreshFailedCredential_ThrowsInvalidOperationException()
        {
            var credential = new OAuthCredential
            {
                Id = 7,
                WorkTrackingSystemConnectionId = ConnectionId,
                AccessToken = "stored-at",
                RefreshToken = "stored-rt",
                ExpiresAt = timeProvider.GetUtcNow().AddHours(1),
                Status = OAuthCredentialStatus.RefreshFailed,
                UpdatedAt = timeProvider.GetUtcNow(),
            };
            credentialRepositoryMock
                .Setup(r => r.GetByPredicate(It.IsAny<Func<OAuthCredential, bool>>()))
                .Returns<Func<OAuthCredential, bool>>(predicate => predicate(credential) ? credential : null);

            var sut = CreateService();

            Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.EnsureFreshTokenAsync(ConnectionId, CancellationToken.None));
        }

        private OAuthService CreateService()
        {
            return new OAuthService(
                providerRegistryMock.Object,
                connectionRepositoryMock.Object,
                credentialRepositoryMock.Object,
                cryptoServiceMock.Object,
                stateTokenIssuerMock.Object,
                serviceConfigMock.Object,
                timeProvider,
                NullLogger<OAuthService>.Instance);
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
