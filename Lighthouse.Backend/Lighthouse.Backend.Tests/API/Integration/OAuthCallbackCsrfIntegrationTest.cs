using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class OAuthCallbackCsrfIntegrationTest
    {
        private const string ProviderKey = AuthenticationMethodKeys.JiraOAuth;
        private const string EncryptedClientId = "enc-client-id";
        private const string EncryptedClientSecret = "enc-client-secret";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private int seededConnectionId;

        [SetUp]
        public void SetUp()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

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
        public async Task Callback_TamperedStateToken_Returns400AndPersistsNoCredential()
        {
            var validState = IssueStateToken(seededConnectionId, ProviderKey);
            var tampered = TamperPayload(validState);

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
            var credential = credentialRepo.GetByPredicate(c => c.WorkTrackingSystemConnectionId == seededConnectionId);
            Assert.That(credential, Is.Null);
        }

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

            seededConnectionId = connection.Id;
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
