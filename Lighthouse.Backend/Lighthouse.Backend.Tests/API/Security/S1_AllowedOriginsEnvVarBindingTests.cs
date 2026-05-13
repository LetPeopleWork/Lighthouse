using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Security
{
    [NonParallelizable]
    public class S1_AllowedOriginsEnvVarBindingTests
    {
        private const string AnonymousProbeEndpoint = "/api/latest/version/current";
        private const string SingleOrigin = "https://localhost:48332";
        private const string FirstOrigin = "https://app.example";
        private const string SecondOrigin = "https://admin.example";
        private const string ForeignOrigin = "https://malicious.example.com";

        private static readonly string[] AuthEnvKeys =
        [
            "Authentication__Enabled",
            "Authentication__Authority",
            "Authentication__ClientId",
            "Authentication__ClientSecret",
            "Authentication__MetadataAddress",
            "Authentication__RequireHttpsMetadata",
            "Authentication__AllowedOrigins",
            "Authentication__AllowedOrigins__0",
            "Authentication__AllowedOrigins__1",
        ];

        [SetUp]
        public void ClearAuthEnv()
        {
            foreach (var key in AuthEnvKeys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        [TearDown]
        public void RestoreAuthEnv()
        {
            foreach (var key in AuthEnvKeys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        [Test]
        public void AuthEnabledWithSingleNonIndexedAllowedOriginsEnvVar_HostStarts()
        {
            SetAuthEnabledEnv();
            Environment.SetEnvironmentVariable("Authentication__AllowedOrigins", SingleOrigin);

            using var factory = new TestWebApplicationFactory<Program>();

            Assert.DoesNotThrow(() =>
            {
                using var client = factory.CreateClient();
            });
        }

        [Test]
        public async Task AuthEnabledWithSingleNonIndexedAllowedOriginsEnvVar_PreflightForThatOriginReturns204()
        {
            SetAuthEnabledEnv();
            Environment.SetEnvironmentVariable("Authentication__AllowedOrigins", SingleOrigin);

            using var factory = new TestWebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            using var request = BuildPreflightRequest(AnonymousProbeEndpoint, SingleOrigin);
            using var response = await client.SendAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.AnyOf(HttpStatusCode.NoContent, HttpStatusCode.OK));
                Assert.That(ReadHeader(response, "Access-Control-Allow-Origin"), Is.EqualTo(SingleOrigin));
                Assert.That(ReadHeader(response, "Access-Control-Allow-Credentials"), Is.EqualTo("true"));
            }
        }

        [Test]
        public async Task AuthEnabledWithSingleNonIndexedAllowedOriginsEnvVar_PreflightForForeignOriginRejected()
        {
            SetAuthEnabledEnv();
            Environment.SetEnvironmentVariable("Authentication__AllowedOrigins", SingleOrigin);

            using var factory = new TestWebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            using var request = BuildPreflightRequest(AnonymousProbeEndpoint, ForeignOrigin);
            using var response = await client.SendAsync(request);

            var allowOrigin = ReadHeader(response, "Access-Control-Allow-Origin");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(allowOrigin, Is.Not.EqualTo(ForeignOrigin));
                Assert.That(allowOrigin, Is.Null.Or.Empty);
            }
        }

        [Test]
        public async Task AuthEnabledWithCommaSeparatedAllowedOrigins_FirstOriginAllowed()
        {
            SetAuthEnabledEnv();
            Environment.SetEnvironmentVariable("Authentication__AllowedOrigins", $"{FirstOrigin},{SecondOrigin}");

            using var factory = new TestWebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            using var request = BuildPreflightRequest(AnonymousProbeEndpoint, FirstOrigin);
            using var response = await client.SendAsync(request);

            Assert.That(ReadHeader(response, "Access-Control-Allow-Origin"), Is.EqualTo(FirstOrigin));
        }

        [Test]
        public async Task AuthEnabledWithCommaSeparatedAllowedOrigins_SecondOriginAllowed()
        {
            SetAuthEnabledEnv();
            Environment.SetEnvironmentVariable("Authentication__AllowedOrigins", $"{FirstOrigin},{SecondOrigin}");

            using var factory = new TestWebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            using var request = BuildPreflightRequest(AnonymousProbeEndpoint, SecondOrigin);
            using var response = await client.SendAsync(request);

            Assert.That(ReadHeader(response, "Access-Control-Allow-Origin"), Is.EqualTo(SecondOrigin));
        }

        [Test]
        public async Task AuthEnabledWithSemicolonSeparatedAllowedOrigins_BothOriginsAllowed()
        {
            SetAuthEnabledEnv();
            Environment.SetEnvironmentVariable("Authentication__AllowedOrigins", $"{FirstOrigin};{SecondOrigin}");

            using var factory = new TestWebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            using var firstRequest = BuildPreflightRequest(AnonymousProbeEndpoint, FirstOrigin);
            using var firstResponse = await client.SendAsync(firstRequest);
            using var secondRequest = BuildPreflightRequest(AnonymousProbeEndpoint, SecondOrigin);
            using var secondResponse = await client.SendAsync(secondRequest);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadHeader(firstResponse, "Access-Control-Allow-Origin"), Is.EqualTo(FirstOrigin));
                Assert.That(ReadHeader(secondResponse, "Access-Control-Allow-Origin"), Is.EqualTo(SecondOrigin));
            }
        }

        [Test]
        public async Task AuthEnabledWithIndexedAllowedOrigins_HostStartsAndPreflightAllowed_BackwardsCompatGuard()
        {
            SetAuthEnabledEnv();
            Environment.SetEnvironmentVariable("Authentication__AllowedOrigins__0", FirstOrigin);

            using var factory = new TestWebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            using var request = BuildPreflightRequest(AnonymousProbeEndpoint, FirstOrigin);
            using var response = await client.SendAsync(request);

            Assert.That(ReadHeader(response, "Access-Control-Allow-Origin"), Is.EqualTo(FirstOrigin));
        }

        [Test]
        public void AuthEnabledWithNoAllowedOriginsAnywhere_HostStillFailsClosed_SecurityGuaranteePreserved()
        {
            SetAuthEnabledEnv();

            using var factory = new TestWebApplicationFactory<Program>();

            var startupException = Assert.Throws<InvalidOperationException>(() =>
            {
                using var client = factory.CreateClient();
            });

            Assert.That(startupException!.Message, Does.Contain("AllowedOrigins"));
        }

        private static void SetAuthEnabledEnv()
        {
            Environment.SetEnvironmentVariable("Authentication__Enabled", "true");
            Environment.SetEnvironmentVariable("Authentication__Authority", "https://example.test/oidc");
            Environment.SetEnvironmentVariable("Authentication__ClientId", "lighthouse-test");
            Environment.SetEnvironmentVariable("Authentication__ClientSecret", "test-secret");
            Environment.SetEnvironmentVariable("Authentication__MetadataAddress", "https://example.test/oidc/.well-known/openid-configuration");
            Environment.SetEnvironmentVariable("Authentication__RequireHttpsMetadata", "false");
        }

        private static HttpRequestMessage BuildPreflightRequest(string path, string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, path);
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            return request;
        }

        private static string? ReadHeader(HttpResponseMessage response, string name)
        {
            if (response.Headers.TryGetValues(name, out var values))
            {
                return string.Join(",", values);
            }
            return null;
        }
    }
}
