using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.Tests.TestHelpers.ForwardedHeaders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// Separate from the other consent endpoint tests, and serial, because this one has to exhaust
    /// the limiter. The limiter's window is process-wide and no test resets it, so a saturated bucket
    /// would fail every other call to the same endpoint for the length of the window.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataConsentRateLimitTests
    {
        private const string ConsentRoute = "/api/latest/usagedata/consent";
        private const string ClientIp = "203.0.113.42";
        private const string TrustedProxyIp = "127.0.0.1";

        private const int PermitLimit = 3;
        private const int WindowSeconds = 60;

        [Test]
        public async Task PostConsent_IsRateLimited()
        {
            using var factory = BuildFactory();
            using var client = factory.CreateClient();

            // This fixture builds its own host rather than deriving from the integration base, so it
            // owns the schema too. The first permitted calls really do write consent rows.
            using var scope = factory.Services.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            databaseContext.Database.EnsureDeleted();
            databaseContext.Database.EnsureCreated();

            var permitted = 0;
            HttpStatusCode? refused = null;

            for (var attempt = 0; attempt < 200 && refused is null; attempt++)
            {
                using var response = await PostDecision(client);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    refused = response.StatusCode;
                }
                else
                {
                    permitted++;
                }
            }

            using (Assert.EnterMultipleScope())
            {
                // Without this, a policy misconfigured to PermitLimit 0 - refusing the very first
                // call - satisfies the test that exists to prove the endpoint still works.
                Assert.That(permitted, Is.GreaterThan(0),
                    "a limiter that refuses everyone is not a limiter, it is an outage");
                Assert.That(refused, Is.EqualTo(HttpStatusCode.TooManyRequests),
                    "the endpoint is unauthenticated and writes a durable row per call. Unlimited, a "
                    + "single caller could plant a granted row and keep an instance emitting for a "
                    + "whole liveness window");
            }

            databaseContext.Database.EnsureDeleted();
        }

        private static Task<HttpResponseMessage> PostDecision(HttpClient client)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, ConsentRoute)
            {
                Content = JsonContent.Create(new { decision = "granted" }),
            };
            request.Headers.Add("X-Forwarded-For", ClientIp);

            return client.SendAsync(request);
        }

        private static WebApplicationFactory<Program> BuildFactory()
        {
            var root = new TestWebApplicationFactory<Program>();
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(
                        new ForwardedHeadersTestStartupFilter(IPAddress.Parse(TrustedProxyIp)));
                });

                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "false",
                        ["Authentication:TrustedProxies:0"] = TrustedProxyIp,
                        ["RateLimits:Enabled"] = "true",
                        ["RateLimits:Policies:UsageDataConsent:PermitLimit"] =
                            PermitLimit.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:UsageDataConsent:WindowSeconds"] =
                            WindowSeconds.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:UsageDataConsent:QueueLimit"] = "0",
                    });
                });
            });
        }
    }
}
