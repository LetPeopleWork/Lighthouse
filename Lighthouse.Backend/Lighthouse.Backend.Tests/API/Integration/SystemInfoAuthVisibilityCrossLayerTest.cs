using System.Net.Http.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Startup;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class SystemInfoAuthVisibilityCrossLayerTest
    {
        [Test]
        public async Task BannerAndApi_AgreeOnAllThreeAuthFields_WhenAuthAndRbacEnabledWithEmergencyAdmin()
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "true",
                ["Authorization:Enabled"] = "true",
                ["Authorization:EmergencySystemAdminSubjects:0"] = "alice@example.com",
                ["Authorization:EmergencySystemAdminSubjects:1"] = "bob@example.com",
            };

            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = WithConfigurationOverrides(rootFactory, overrides);
            using var client = factory.CreateClient();

            var systemInfo = await client.GetFromJsonAsync<SystemInfo>("/api/latest/systeminfo");
            var bannerLines = AuthPostureBanner.BuildAuthPostureLines(BuildConfiguration(overrides));

            Assert.That(systemInfo, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(systemInfo!.IsAuthenticationEnabled, Is.True);
                Assert.That(systemInfo.IsAuthorizationEnabled, Is.True);
                Assert.That(systemInfo.EmergencyAdminSubjects, Is.EqualTo(new[] { "alice@example.com", "bob@example.com" }));

                Assert.That(bannerLines, Has.Some.Contains("Authentication").And.Contains("Enabled"));
                Assert.That(bannerLines, Has.Some.Contains("Authorization").And.Contains("Enabled"));
                Assert.That(bannerLines, Has.Some.Contains("Emergency Admin").And.Contains("alice@example.com, bob@example.com"));
            }
        }

        [Test]
        public async Task BannerAndApi_AgreeOnAllThreeAuthFields_WhenAuthAndRbacDisabledWithoutEmergencyAdmin()
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "false",
                ["Authorization:Enabled"] = "false",
            };

            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = WithConfigurationOverrides(rootFactory, overrides);
            using var client = factory.CreateClient();

            var systemInfo = await client.GetFromJsonAsync<SystemInfo>("/api/latest/systeminfo");
            var bannerLines = AuthPostureBanner.BuildAuthPostureLines(BuildConfiguration(overrides));

            Assert.That(systemInfo, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(systemInfo!.IsAuthenticationEnabled, Is.False);
                Assert.That(systemInfo.IsAuthorizationEnabled, Is.False);
                Assert.That(systemInfo.EmergencyAdminSubjects, Is.Empty);

                Assert.That(bannerLines, Has.Some.Contains("Authentication").And.Contains("Disabled"));
                Assert.That(bannerLines, Has.Some.Contains("Authorization").And.Contains("Disabled"));
                Assert.That(bannerLines, Has.None.Contains("Emergency Admin"));
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

        private static IConfiguration BuildConfiguration(Dictionary<string, string?> overrides)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(overrides)
                .Build();
        }
    }
}
