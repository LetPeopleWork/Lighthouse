using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Lighthouse.Backend.Tests.API.Security
{
    public class S3_UpdateNotificationHubAuthorizeTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        private const string HubNegotiatePath = "/api/updateNotificationHub/negotiate?negotiateVersion=1";

        [Test]
        public async Task S3_AuthEnabled_HubHandshakeWithoutSession_Returns401()
        {
            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            using var client = factory.CreateClient();

            client.AsAnonymous();

            var response = await client.PostAsync(HubNegotiatePath, content: null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task S3_AuthEnabled_HubHandshakeWithValidSession_Returns200()
        {
            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            using var client = factory.CreateClient();

            client.AsViewer();

            var response = await client.PostAsync(HubNegotiatePath, content: null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task S3_AuthDisabled_HubHandshakeNoFallbackPolicy_Returns200_RegressionGuard()
        {
            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = WithAuthenticationDisabled(rootFactory);
            using var client = factory.CreateClient();

            var response = await client.PostAsync(HubNegotiatePath, content: null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        private static WebApplicationFactory<Program> WithAuthenticationDisabled(WebApplicationFactory<Program> root)
        {
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "false",
                    });
                });
            });
        }
    }
}
