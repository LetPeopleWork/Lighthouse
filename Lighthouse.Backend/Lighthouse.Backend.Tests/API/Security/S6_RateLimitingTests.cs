using System.Globalization;
using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.Tests.TestHelpers.ForwardedHeaders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Security
{
    [NonParallelizable]
    public class S6_RateLimitingTests
    {
        private const string LoginPath = "/api/v1/auth/login";
        private const string VersionPath = "/api/v1/version/current";
        private const string PrimaryClientIp = "203.0.113.10";
        private const string SecondaryClientIp = "203.0.113.99";
        private const string FreshWindowClientIp = "203.0.113.11";
        private const string IndependentPrimaryClientIp = "203.0.113.12";
        private const string AuthDisabledClientIp = "203.0.113.14";
        private const string AdjacentEndpointClientIp = "203.0.113.15";
        private const string FirstSharedHostClientIp = "203.0.113.16";
        private const string SecondSharedHostClientIp = "203.0.113.17";
        private const string TrustedProxyIp = "127.0.0.1";

        private const int PermitLimit = 3;
        private const int WindowSeconds = 2;

        private TestWebApplicationFactory<Program> root = null!;
        private WebApplicationFactory<Program> host = null!;

        // The limiter keeps one bucket per client address for as long as the host lives, and nothing empties
        // it. Every test that uses up a bucket therefore uses an address no other test uses.
        [OneTimeSetUp]
        public void StartTheHost()
        {
            root = new TestWebApplicationFactory<Program>();
            host = BuildFactory(root, rateLimitsEnabled: true, authEnabled: false);
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            host.Dispose();
            root.Dispose();
        }

        [Test]
        public async Task S6_SharedHost_FirstTestUsesUpTheLimitOfItsAddress()
        {
            await UseUpTheLimitAndExpectARefusal(FirstSharedHostClientIp);
        }

        [Test]
        public async Task S6_SharedHost_SecondTestUsesUpTheLimitOfItsAddress()
        {
            await UseUpTheLimitAndExpectARefusal(SecondSharedHostClientIp);
        }

        [Test]
        public async Task S6_WalkingSkeleton_ExceedsPerIpLoginLimit_Returns429WithRetryAfter()
        {
            using var client = host.CreateClient();

            var statusesBeforeLimit = new List<HttpStatusCode>();
            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, PrimaryClientIp);
                statusesBeforeLimit.Add(response.StatusCode);
            }

            using var throttledResponse = await SendLoginRequest(client, PrimaryClientIp);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(statusesBeforeLimit, Has.All.Not.EqualTo(HttpStatusCode.TooManyRequests));
                Assert.That(throttledResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

                var retryAfter = ReadHeader(throttledResponse, "Retry-After");
                Assert.That(retryAfter, Is.Not.Null.And.Not.Empty, "Retry-After header must be present on 429");
                Assert.That(int.TryParse(retryAfter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds), Is.True);
                Assert.That(seconds, Is.GreaterThan(0));
            }
        }

        [Test]
        public async Task S6_FreshWindow_RequestsAcceptedAgain()
        {
            using var client = host.CreateClient();

            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, FreshWindowClientIp);
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
            }

            using var saturatingResponse = await SendLoginRequest(client, FreshWindowClientIp);
            Assert.That(saturatingResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests), "Precondition: bucket saturated");

            await Task.Delay(TimeSpan.FromSeconds(WindowSeconds + 1));

            var freshStatuses = new List<HttpStatusCode>();
            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, FreshWindowClientIp);
                freshStatuses.Add(response.StatusCode);
            }

            Assert.That(freshStatuses, Has.All.Not.EqualTo(HttpStatusCode.TooManyRequests));
        }

        [Test]
        public async Task S6_DifferentForwardedIp_IndependentBucket_NotThrottled()
        {
            using var client = host.CreateClient();

            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, IndependentPrimaryClientIp);
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
            }

            using var primarySaturatedResponse = await SendLoginRequest(client, IndependentPrimaryClientIp);
            Assert.That(primarySaturatedResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests), "Precondition: primary IP saturated");

            var secondaryStatuses = new List<HttpStatusCode>();
            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, SecondaryClientIp);
                secondaryStatuses.Add(response.StatusCode);
            }

            Assert.That(secondaryStatuses, Has.All.Not.EqualTo(HttpStatusCode.TooManyRequests));
        }

        [Test]
        public async Task S6_AuthDisabled_RateLimiterStillActive_BeforeAuthMiddleware()
        {
            using var client = host.CreateClient();

            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, AuthDisabledClientIp);
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
            }

            using var throttledResponse = await SendLoginRequest(client, AuthDisabledClientIp);

            Assert.That(throttledResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }

        [Test]
        public async Task S6_NonAuthAdjacentEndpoint_NotThrottled_Regression()
        {
            using var client = host.CreateClient();

            var statuses = new List<HttpStatusCode>();
            for (var requestIndex = 0; requestIndex < 100; requestIndex++)
            {
                using var response = await SendRequest(client, VersionPath, AdjacentEndpointClientIp);
                statuses.Add(response.StatusCode);
            }

            Assert.That(statuses, Has.All.Not.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(statuses, Has.All.EqualTo(HttpStatusCode.OK));
        }

        // Every other test configures the policy it exercises, so they pass whether or not the shipped file
        // defines it - and an undefined policy throttles nothing, silently. Only reading the shipped file
        // catches a refinement contribution policy that has gone missing or drifted from thirty a minute.
        [Test]
        public void TheShippedConfiguration_LimitsRefinementContributionsToThirtyAMinute()
        {
            var appsettingsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "appsettings.json");
            using var document = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
            var rateLimits = document.RootElement.GetProperty(RateLimitingConfiguration.SectionName);

            Assert.That(rateLimits.GetProperty("Policies").TryGetProperty(RateLimitingConfiguration.RefinementContributionPolicy, out var policy), Is.True,
                $"policy '{RateLimitingConfiguration.RefinementContributionPolicy}' is undefined in appsettings.json, so every vote write runs unthrottled");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(policy.GetProperty(nameof(FixedWindowPolicyConfiguration.PermitLimit)).GetInt32(), Is.EqualTo(30));
                Assert.That(policy.GetProperty(nameof(FixedWindowPolicyConfiguration.WindowSeconds)).GetInt32(), Is.EqualTo(60));
                Assert.That(policy.GetProperty(nameof(FixedWindowPolicyConfiguration.QueueLimit)).GetInt32(), Is.Zero);
            }
        }

        private async Task UseUpTheLimitAndExpectARefusal(string clientIp)
        {
            using var client = host.CreateClient();

            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, clientIp);
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests),
                    "the permits of this address were already spent before this test sent anything");
            }

            using var throttledResponse = await SendLoginRequest(client, clientIp);

            Assert.That(throttledResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }

        private static WebApplicationFactory<Program> BuildFactory(
            TestWebApplicationFactory<Program> root, bool rateLimitsEnabled, bool authEnabled)
        {
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(
                        new ForwardedHeadersTestStartupFilter(IPAddress.Parse(TrustedProxyIp)));
                });

                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = authEnabled ? "true" : "false",
                        ["Authentication:TrustedProxies:0"] = TrustedProxyIp,
                        ["RateLimits:Enabled"] = rateLimitsEnabled ? "true" : "false",
                        ["RateLimits:Policies:AuthLogin:PermitLimit"] = PermitLimit.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:AuthLogin:WindowSeconds"] = WindowSeconds.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:AuthLogin:QueueLimit"] = "0",
                        ["RateLimits:Policies:ApiKeys:PermitLimit"] = PermitLimit.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:ApiKeys:WindowSeconds"] = WindowSeconds.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:ApiKeys:QueueLimit"] = "0",
                        ["RateLimits:Policies:BootstrapSystemAdmin:PermitLimit"] = PermitLimit.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:BootstrapSystemAdmin:WindowSeconds"] = WindowSeconds.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:BootstrapSystemAdmin:QueueLimit"] = "0",
                    };

                    configurationBuilder.AddInMemoryCollection(settings);
                });
            });
        }

        private static Task<HttpResponseMessage> SendLoginRequest(HttpClient client, string forwardedFor)
        {
            return SendRequest(client, LoginPath, forwardedFor);
        }

        private static async Task<HttpResponseMessage> SendRequest(HttpClient client, string path, string forwardedFor)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("X-Forwarded-For", forwardedFor);
            return await client.SendAsync(request);
        }

        private static string? ReadHeader(HttpResponseMessage response, string name)
        {
            if (response.Headers.TryGetValues(name, out var values))
            {
                return string.Join(",", values);
            }

            if (response.Content.Headers.TryGetValues(name, out var contentValues))
            {
                return string.Join(",", contentValues);
            }

            return null;
        }
    }
}
