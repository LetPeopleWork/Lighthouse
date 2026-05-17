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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    // Regression test for ADO OAuth team-update failure observed 2026-05-17:
    // "A second operation was started on this context instance before a previous operation completed."
    // Production wire-up: AzureDevOpsWorkTrackingConnector.ConvertAdoWorkItemToLighthouseWorkItemBase
    // fans out 8 concurrent ConvertAdoWorkItemToLighthouseWorkItem calls via SemaphoreSlim + Task.WhenAll;
    // each call walks GetClientAsync -> BuildVssCredentialsAsync -> OAuthBearerAuthStrategy.ApplyAsync ->
    // OAuthService.EnsureFreshTokenAsync -> LoadValidCredentialOrThrow -> credentialRepository.GetByPredicate(...).
    // All N callers share the SAME scoped LighthouseAppContext, so EF Core's IConcurrencyDetector
    // fires on the second concurrent read. PAT auth never touches the DB at request time, hence works.
    // The single-flight refresh test passes because it uses Mock<IRepository<OAuthCredential>>
    // (mocks have no concurrency detector).
    [TestFixture]
    public class OAuthCredentialConcurrentLoadTest
    {
        private const int ConnectionId = 77;
        private const string ProviderKey = "ado.oauth";
        private const string EncryptedAccessToken = "enc-at";
        private const string PlaintextAccessToken = "plain-at";
        private const int ConcurrentCallers = 32;

        [Test]
        public async Task EnsureFreshTokenAsync_ConcurrentCallersSharingDbContext_DoNotRaceTheDbContext()
        {
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 17, 9, 23, 0, TimeSpan.Zero));

            var cryptoServiceMock = new Mock<ICryptoService>();
            cryptoServiceMock.Setup(c => c.Decrypt(EncryptedAccessToken)).Returns(PlaintextAccessToken);
            cryptoServiceMock.Setup(c => c.Encrypt(It.IsAny<string>())).Returns<string>(v => v);

            var contextOptions = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LighthouseAppContext(
                contextOptions,
                cryptoServiceMock.Object,
                NullLogger<LighthouseAppContext>.Instance);

            var connection = new WorkTrackingSystemConnection
            {
                Id = ConnectionId,
                Name = "OAuth ADO Connection",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                AuthenticationMethodKey = ProviderKey,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = OAuthWorkTrackingOptionNames.ClientId,
                Value = "enc-client-id",
                IsSecret = false,
            });
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = OAuthWorkTrackingOptionNames.ClientSecret,
                Value = "enc-client-secret",
                IsSecret = true,
            });
            context.WorkTrackingSystemConnections.Add(connection);

            context.OAuthCredentials.Add(new OAuthCredential
            {
                Id = 901,
                WorkTrackingSystemConnectionId = ConnectionId,
                AccessToken = EncryptedAccessToken,
                RefreshToken = "enc-rt",
                ExpiresAt = timeProvider.GetUtcNow().AddHours(1),
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = timeProvider.GetUtcNow().AddMinutes(-1),
            });
            await context.SaveChangesAsync();

            var connectionRepository = new WorkTrackingSystemConnectionRepository(
                context, NullLogger<WorkTrackingSystemConnectionRepository>.Instance);
            var credentialRepository = new OAuthCredentialRepository(
                context, NullLogger<OAuthCredentialRepository>.Instance);

            var providerRegistryMock = new Mock<IOAuthProviderRegistry>();
            var stateTokenIssuerMock = new Mock<IOAuthStateTokenIssuer>();
            var serviceConfigMock = new Mock<IServiceConfig>();
            serviceConfigMock.SetupGet(c => c.BaseUrl).Returns("https://lighthouse.example.com");

            var sut = new OAuthService(
                providerRegistryMock.Object,
                connectionRepository,
                credentialRepository,
                cryptoServiceMock.Object,
                stateTokenIssuerMock.Object,
                serviceConfigMock.Object,
                timeProvider,
                NullLogger<OAuthService>.Instance,
                Mock.Of<IHttpContextAccessor>(),
                OAuthRefreshOptions.Default);

            var startGate = new ManualResetEventSlim(false);
            var tasks = Enumerable.Range(0, ConcurrentCallers)
                .Select(_ => Task.Run(async () =>
                {
                    startGate.Wait();
                    return await sut.EnsureFreshTokenAsync(ConnectionId, CancellationToken.None);
                }))
                .ToArray();

            startGate.Set();

            try
            {
                var tokens = await Task.WhenAll(tasks);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(tokens, Has.All.EqualTo(PlaintextAccessToken));
                    Assert.That(tokens, Has.Length.EqualTo(ConcurrentCallers));
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("second operation", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail(
                    "OAuthService.EnsureFreshTokenAsync raced the shared LighthouseAppContext. " +
                    "Concurrent callers must serialise their DB access (per-connection lock, " +
                    "DbContext factory, or pre-resolved auth header) before the parallel " +
                    "ConvertAdoWorkItemToLighthouseWorkItemBase fan-out can use OAuth credentials. " +
                    $"Original error: {ex.Message}");
            }
        }
    }
}
