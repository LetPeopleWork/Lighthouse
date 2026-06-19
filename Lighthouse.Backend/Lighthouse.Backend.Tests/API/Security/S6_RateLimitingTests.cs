using System.Globalization;
using System.Net;
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
        private const string TrustedProxyIp = "127.0.0.1";

        private const int PermitLimit = 3;
        private const int WindowSeconds = 2;

        [Test]
        public async Task S6_WalkingSkeleton_ExceedsPerIpLoginLimit_Returns429WithRetryAfter()
        {
            using var factory = BuildFactory(rateLimitsEnabled: true, authEnabled: false);
            using var client = factory.CreateClient();

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
            using var factory = BuildFactory(rateLimitsEnabled: true, authEnabled: false);
            using var client = factory.CreateClient();

            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, PrimaryClientIp);
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
            }

            using var saturatingResponse = await SendLoginRequest(client, PrimaryClientIp);
            Assert.That(saturatingResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests), "Precondition: bucket saturated");

            await Task.Delay(TimeSpan.FromSeconds(WindowSeconds + 1));

            var freshStatuses = new List<HttpStatusCode>();
            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, PrimaryClientIp);
                freshStatuses.Add(response.StatusCode);
            }

            Assert.That(freshStatuses, Has.All.Not.EqualTo(HttpStatusCode.TooManyRequests));
        }

        [Test]
        public async Task S6_DifferentForwardedIp_IndependentBucket_NotThrottled()
        {
            using var factory = BuildFactory(rateLimitsEnabled: true, authEnabled: false);
            using var client = factory.CreateClient();

            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, PrimaryClientIp);
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
            }

            using var primarySaturatedResponse = await SendLoginRequest(client, PrimaryClientIp);
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
            using var factory = BuildFactory(rateLimitsEnabled: true, authEnabled: false);
            using var client = factory.CreateClient();

            for (var requestIndex = 0; requestIndex < PermitLimit; requestIndex++)
            {
                using var response = await SendLoginRequest(client, PrimaryClientIp);
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
            }

            using var throttledResponse = await SendLoginRequest(client, PrimaryClientIp);

            Assert.That(throttledResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }

        [Test]
        public async Task S6_NonAuthAdjacentEndpoint_NotThrottled_Regression()
        {
            using var factory = BuildFactory(rateLimitsEnabled: true, authEnabled: false);
            using var client = factory.CreateClient();

            var statuses = new List<HttpStatusCode>();
            for (var requestIndex = 0; requestIndex < 100; requestIndex++)
            {
                using var response = await SendRequest(client, VersionPath, PrimaryClientIp);
                statuses.Add(response.StatusCode);
            }

            Assert.That(statuses, Has.All.Not.EqualTo(HttpStatusCode.TooManyRequests));
        }

        private static WebApplicationFactory<Program> BuildFactory(bool rateLimitsEnabled, bool authEnabled)
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
