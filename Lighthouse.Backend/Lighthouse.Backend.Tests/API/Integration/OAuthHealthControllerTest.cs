using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Tests.TestHelpers;
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

        [SetUp]
        public void SetUp()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            providerMock = new Mock<IOAuthProvider>();
            providerMock.SetupGet(p => p.ProviderKey).Returns(ProviderKey);
            providerMock.SetupGet(p => p.DefaultScopes).Returns(new[] { "read:jira-work" });

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IOAuthProvider>();
                        services.AddSingleton(providerMock.Object);

                        services.RemoveAll<ICryptoService>();
                        services.AddSingleton<ICryptoService, FakeCryptoService>();
                    });
                });

            client = factory.CreateClient();
            ResetDatabase();
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task GetHealth_AsSystemAdmin_ReturnsTotalAndDisconnectedCounts()
        {
            SeedConnectionWithCredential(OAuthCredentialStatus.RefreshFailed);
            SeedConnectionWithCredential(OAuthCredentialStatus.Disconnected);
            SeedConnectionWithCredential(OAuthCredentialStatus.Valid);
            client.AsSystemAdmin();

            var response = await client.GetAsync("/api/oauth/health");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetProperty("totalOAuthConnections").GetInt32(), Is.EqualTo(3), body);
                Assert.That(root.GetProperty("disconnectedCount").GetInt32(), Is.EqualTo(2), body);
                Assert.That(root.GetProperty("firstDisconnectedConnectionId").ValueKind, Is.EqualTo(JsonValueKind.Number), body);
            }
        }

        [Test]
        public async Task GetHealth_AsNonSystemAdmin_Returns403()
        {
            client.AsViewer();

            var response = await client.GetAsync("/api/oauth/health");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        private void ResetDatabase()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        private void SeedConnectionWithCredential(OAuthCredentialStatus status)
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
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            dbContext.SaveChanges();
        }
    }
}
