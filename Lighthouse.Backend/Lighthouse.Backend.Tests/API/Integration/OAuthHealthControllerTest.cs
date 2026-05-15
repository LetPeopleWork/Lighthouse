using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class OAuthHealthControllerTest
    {
        private const string ProviderKey = "jira.oauth";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<IOAuthProvider> providerMock = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int seededConnectionId;

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
        public async Task GetHealth_AsSystemAdminWithPremium_Returns200WithStaleCountsAndPendingMetrics()
        {
            SeedCredential(OAuthCredentialStatus.RefreshFailed, updatedAt: DateTimeOffset.UtcNow.AddDays(-8));
            SeedCredential(OAuthCredentialStatus.RefreshFailed, updatedAt: DateTimeOffset.UtcNow.AddHours(-25));
            SeedCredential(OAuthCredentialStatus.Valid, updatedAt: DateTimeOffset.UtcNow.AddDays(-10));
            client.AsSystemAdmin();

            var response = await client.GetAsync("/api/oauth/health");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetProperty("staleRefreshFailedCount24h").GetInt64(), Is.EqualTo(2), body);
                Assert.That(root.GetProperty("staleRefreshFailedCount7d").GetInt64(), Is.EqualTo(1), body);

                var setup = root.GetProperty("setupSuccessRate30d");
                Assert.That(setup.GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null), body);
                Assert.That(setup.GetProperty("unavailableReason").GetString(), Is.EqualTo("event_store_pending"), body);

                var refresh = root.GetProperty("refreshSuccessRate7d");
                Assert.That(refresh.GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null), body);
                Assert.That(refresh.GetProperty("unavailableReason").GetString(), Is.EqualTo("event_store_pending"), body);
            }
        }

        [Test]
        public async Task GetHealth_AsSystemAdminWithoutPremium_Returns403()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
            client.AsSystemAdmin();

            var response = await client.GetAsync("/api/oauth/health");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task GetHealth_AsNonSystemAdmin_Returns403()
        {
            client.AsViewer();

            var response = await client.GetAsync("/api/oauth/health");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
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
            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            seededConnectionId = connection.Id;
        }

        private void SeedCredential(OAuthCredentialStatus status, DateTimeOffset updatedAt)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"OAuth Jira #{Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = ProviderKey,
            };
            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            dbContext.OAuthCredentials.Add(new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = "at",
                RefreshToken = "rt",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Status = status,
                UpdatedAt = updatedAt,
            });
            dbContext.SaveChanges();
        }
    }
}
