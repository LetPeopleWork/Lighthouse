using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class SystemInfoWireFormatRegressionTests
    {
        [Test]
        public async Task GetSystemInfo_JsonResponse_UsesAuthenticationEnabledPropertyName_NotIsAuthenticationEnabled()
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "true",
                ["Authorization:Enabled"] = "false",
            };

            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = WithConfigurationOverrides(rootFactory, overrides);
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/latest/SystemInfo");
            response.EnsureSuccessStatusCode();
            var rawJson = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.TryGetProperty("authenticationEnabled", out var authProperty), Is.True,
                    $"Expected JSON property 'authenticationEnabled' (per milestone-1 acceptance criteria); raw response was: {rawJson}");
                Assert.That(authProperty.GetBoolean(), Is.True);
                Assert.That(root.TryGetProperty("isAuthenticationEnabled", out _), Is.False,
                    $"JSON must NOT contain the legacy 'isAuthenticationEnabled' key; raw response was: {rawJson}");
            }
        }

        [Test]
        public async Task GetSystemInfo_JsonResponse_UsesAuthorizationEnabledPropertyName_NotIsAuthorizationEnabled()
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "true",
                ["Authorization:Enabled"] = "true",
            };

            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = WithConfigurationOverrides(rootFactory, overrides);
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/latest/SystemInfo");
            response.EnsureSuccessStatusCode();
            var rawJson = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.TryGetProperty("authorizationEnabled", out var authzProperty), Is.True,
                    $"Expected JSON property 'authorizationEnabled' (per milestone-1 acceptance criteria); raw response was: {rawJson}");
                Assert.That(authzProperty.GetBoolean(), Is.True);
                Assert.That(root.TryGetProperty("isAuthorizationEnabled", out _), Is.False,
                    $"JSON must NOT contain the legacy 'isAuthorizationEnabled' key; raw response was: {rawJson}");
            }
        }

        [Test]
        public async Task GetSystemInfo_JsonResponse_KeepsAgreedNamesWhenAuthAndRbacDisabled()
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "false",
                ["Authorization:Enabled"] = "false",
            };

            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = WithConfigurationOverrides(rootFactory, overrides);
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/latest/SystemInfo");
            response.EnsureSuccessStatusCode();
            var rawJson = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.TryGetProperty("authenticationEnabled", out var authProperty), Is.True);
                Assert.That(authProperty.GetBoolean(), Is.False);
                Assert.That(root.TryGetProperty("authorizationEnabled", out var authzProperty), Is.True);
                Assert.That(authzProperty.GetBoolean(), Is.False);
                Assert.That(root.TryGetProperty("isAuthenticationEnabled", out _), Is.False);
                Assert.That(root.TryGetProperty("isAuthorizationEnabled", out _), Is.False);
            }
        }

        private static WebApplicationFactory<Program> WithConfigurationOverrides(
            WebApplicationFactory<Program> root,
            Dictionary<string, string?> overrides)
        {
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(overrides);
                });
            });
        }
    }
}
