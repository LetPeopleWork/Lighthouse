using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class OAuthControllerIntegrationTest
    {
        private const string ProviderKey = "jira.oauth";
        private const string EncryptedClientId = "enc-client-id";
        private const string EncryptedClientSecret = "enc-client-secret";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<IOAuthProvider> providerMock = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;

        [SetUp]
        public void SetUp()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            providerMock = new Mock<IOAuthProvider>();
            providerMock.SetupGet(p => p.ProviderKey).Returns(ProviderKey);
            providerMock.SetupGet(p => p.DefaultScopes).Returns(new[] { "read:jira-work" });

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                    {
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Lighthouse:BaseUrl"] = "https://lighthouse.test",
                        });
                    });

                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IOAuthProvider>();
                        services.AddSingleton(providerMock.Object);

                        services.RemoveAll<ICryptoService>();
                        services.AddSingleton<ICryptoService, FakeCryptoService>();

                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });

            client = factory.CreateClient();
            SeedConnection();
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task Connect_AsSystemAdminWithPremium_Returns200WithAuthorizationUrl()
        {
            providerMock
                .Setup(p => p.BuildAuthorizationUrl(It.IsAny<OAuthFlowContext>()))
                .Returns(new Uri("https://auth.atlassian.com/authorize?client_id=client-id-123"));
            client.AsSystemAdmin();

            var response = await client.PostAsJsonAsync(
                $"/api/oauth/{ProviderKey}/connect",
                new { connectionId = SeededConnectionId });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var body = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(body);
                Assert.That(document.RootElement.TryGetProperty("authorizationUrl", out var authUrl), Is.True, body);
                Assert.That(authUrl.GetString(), Is.EqualTo("https://auth.atlassian.com/authorize?client_id=client-id-123"));
            }
        }

        [Test]
        public async Task Connect_AsSystemAdminWithoutPremium_Returns403()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
            client.AsSystemAdmin();

            var response = await client.PostAsJsonAsync(
                $"/api/oauth/{ProviderKey}/connect",
                new { connectionId = SeededConnectionId });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task Connect_AsNonSystemAdmin_Returns403()
        {
            client.AsViewer();

            var response = await client.PostAsJsonAsync(
                $"/api/oauth/{ProviderKey}/connect",
                new { connectionId = SeededConnectionId });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task Connect_AsAnonymous_Returns401()
        {
            client.AsAnonymous();

            var response = await client.PostAsJsonAsync(
                $"/api/oauth/{ProviderKey}/connect",
                new { connectionId = SeededConnectionId });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Callback_WithValidState_Redirects302ToSuccessPageAndPersistsValidCredential()
        {
            var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
            providerMock
                .Setup(p => p.ExchangeCodeAsync("auth-code-123", It.IsAny<OAuthFlowContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthTokens("at", "rt", expiresAt));

            var validState = IssueStateToken(SeededConnectionId, ProviderKey);

            using var noRedirectClient = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await noRedirectClient.GetAsync(
                $"/api/oauth/callback?provider={ProviderKey}&code=auth-code-123&state={Uri.EscapeDataString(validState)}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
                Assert.That(response.Headers.Location, Is.Not.Null);
                Assert.That(response.Headers.Location!.ToString(),
                    Is.EqualTo($"/settings/connections/new?oauth=success&connectionId={SeededConnectionId}"));
            }

            using var verificationScope = factory.Services.CreateScope();
            var credentialRepo = verificationScope.ServiceProvider.GetRequiredService<IRepository<OAuthCredential>>();
            var credential = credentialRepo.GetByPredicate(c => c.WorkTrackingSystemConnectionId == SeededConnectionId);
            Assert.That(credential, Is.Not.Null);
            Assert.That(credential!.Status, Is.EqualTo(OAuthCredentialStatus.Valid));
        }

        [Test]
        public async Task Callback_WithoutProviderQueryParam_Redirects302ToSuccess()
        {
            var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
            providerMock
                .Setup(p => p.ExchangeCodeAsync("auth-code-456", It.IsAny<OAuthFlowContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthTokens("at", "rt", expiresAt));

            var validState = IssueStateToken(SeededConnectionId, ProviderKey);

            using var noRedirectClient = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await noRedirectClient.GetAsync(
                $"/api/oauth/callback?code=auth-code-456&state={Uri.EscapeDataString(validState)}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        }

        [Test]
        public async Task Callback_WithTamperedState_Returns400AndPersistsNoCredential()
        {
            var validState = IssueStateToken(SeededConnectionId, ProviderKey);
            var tampered = TamperPayload(validState);

            client.AsAnonymous();

            var response = await client.GetAsync(
                $"/api/oauth/callback?provider={ProviderKey}&code=anything&state={Uri.EscapeDataString(tampered)}");

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("invalid state").IgnoreCase, body);
            }

            using var verificationScope = factory.Services.CreateScope();
            var credentialRepo = verificationScope.ServiceProvider.GetRequiredService<IRepository<OAuthCredential>>();
            var credential = credentialRepo.GetByPredicate(c => c.WorkTrackingSystemConnectionId == SeededConnectionId);
            Assert.That(credential, Is.Null);
        }

        [Test]
        public async Task Disconnect_AsSystemAdminWithPremium_Returns204AndTransitionsCredentialToDisconnected()
        {
            SeedOAuthCredential(SeededConnectionId);
            client.AsSystemAdmin();

            var response = await client.PostAsJsonAsync(
                $"/api/oauth/{ProviderKey}/disconnect",
                new { connectionId = SeededConnectionId });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            using var verificationScope = factory.Services.CreateScope();
            var credentialRepo = verificationScope.ServiceProvider.GetRequiredService<IRepository<OAuthCredential>>();
            var credential = credentialRepo.GetByPredicate(c => c.WorkTrackingSystemConnectionId == SeededConnectionId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(credential, Is.Not.Null);
                Assert.That(credential!.Status, Is.EqualTo(OAuthCredentialStatus.Disconnected));
                Assert.That(credential.AccessToken, Is.Empty);
                Assert.That(credential.RefreshToken, Is.Empty);
            }
        }

        private int SeededConnectionId { get; set; }

        private string IssueStateToken(int connectionId, string providerKey)
        {
            using var scope = factory.Services.CreateScope();
            var stateIssuer = scope.ServiceProvider.GetRequiredService<IOAuthStateTokenIssuer>();
            return stateIssuer.Issue(connectionId, providerKey);
        }

        private void SeedConnection()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var connection = new WorkTrackingSystemConnection
            {
                Name = "OAuth Jira",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = ProviderKey,
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = OAuthWorkTrackingOptionNames.ClientId,
                Value = EncryptedClientId,
                IsSecret = false,
                IsOptional = false,
            });
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = OAuthWorkTrackingOptionNames.ClientSecret,
                Value = EncryptedClientSecret,
                IsSecret = true,
                IsOptional = false,
            });

            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            SeededConnectionId = connection.Id;
        }

        private void SeedOAuthCredential(int connectionId)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.OAuthCredentials.Add(new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connectionId,
                AccessToken = "existing-at",
                RefreshToken = "existing-rt",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            });
            dbContext.SaveChanges();
        }

        private static string TamperPayload(string token)
        {
            var separatorIndex = token.IndexOf('.');
            var payload = token[..separatorIndex];
            var hash = token[(separatorIndex + 1)..];
            var tamperedPayload = payload.Length > 0
                ? (payload[0] == 'A' ? 'B' : 'A') + payload[1..]
                : "A";
            return $"{tamperedPayload}.{hash}";
        }
    }
}
