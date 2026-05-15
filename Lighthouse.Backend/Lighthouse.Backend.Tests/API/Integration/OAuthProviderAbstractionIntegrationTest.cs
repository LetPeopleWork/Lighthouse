using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.OAuth.Providers;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class OAuthProviderAbstractionIntegrationTest
    {
        private const string StubProviderKey = AuthenticationMethodKeys.StubOAuth;
        private const string EncryptedClientId = "enc-client-id";
        private const string EncryptedClientSecret = "enc-client-secret";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int seededConnectionId;

        [SetUp]
        public void SetUp()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            AuthenticationMethodSchema.SetExtraOAuthKeysForTesting(new[] { StubProviderKey });

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
                        services.RemoveAll<ICryptoService>();
                        services.AddSingleton<ICryptoService, FakeCryptoService>();

                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);

                        services.AddSingleton<IOAuthProvider>(sp =>
                        {
                            var serviceConfig = sp.GetRequiredService<IServiceConfig>();
                            var timeProvider = sp.GetRequiredService<TimeProvider>();
                            return new StubOAuthProvider(serviceConfig, timeProvider);
                        });
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
            AuthenticationMethodSchema.SetExtraOAuthKeysForTesting(Array.Empty<string>());
        }

        [Test]
        public async Task NewProviderAddedViaTestDi_FlowInitiates_WithoutTouchingCoreInfrastructure()
        {
            client.AsSystemAdmin();

            var response = await client.PostAsJsonAsync(
                $"/api/oauth/{StubProviderKey}/connect",
                new { connectionId = seededConnectionId });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var body = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(body);
                Assert.That(document.RootElement.TryGetProperty("authorizationUrl", out var authUrl), Is.True, body);
                var url = authUrl.GetString();
                Assert.That(url, Is.Not.Null.And.Not.Empty);
                Assert.That(url, Does.Contain("/api/oauth/callback"), body);
                Assert.That(url, Does.Contain("state="), body);
            }
        }

        private void SeedConnection()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Stub OAuth",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = StubProviderKey,
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

            seededConnectionId = connection.Id;
        }
    }
}
