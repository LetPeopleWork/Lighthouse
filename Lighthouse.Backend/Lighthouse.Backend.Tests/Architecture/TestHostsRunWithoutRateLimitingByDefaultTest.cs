using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A test host with the rate limiters on stays reachable after it is disposed, so the whole suite would
    /// grow with every host it starts. Hosts therefore run without rate limiting unless a fixture that tests
    /// it asks for it; this keeps that default from flipping back unnoticed. It checks the value the
    /// application's rate-limiting branch reads, so it must change together with that branch.
    /// </summary>
    [TestFixture]
    public class TestHostsRunWithoutRateLimitingByDefaultTest
    {
        [Test]
        public void ADefaultTestHostLeavesRateLimitingOff()
        {
            using var factory = new TestWebApplicationFactory<Program>();

            // The same read the application makes when it decides whether to put the rate limiter in the pipeline.
            var settings = factory.Services.GetRequiredService<IConfiguration>()
                .GetSection(RateLimitingConfiguration.SectionName)
                .Get<RateLimitingConfiguration>() ?? new RateLimitingConfiguration();

            Assert.That(settings.Enabled, Is.False);
        }

        [Test]
        public void TheEmbedHostThatAsksForARateLimitHasRateLimitingOn()
        {
            using var embedHosts = new ViewerEmbedTestHost();
            using var host = embedHosts.WithEmbedRateLimit(permitLimit: 5, windowSeconds: 60);

            var settings = host.Services.GetRequiredService<IConfiguration>()
                .GetSection(RateLimitingConfiguration.SectionName)
                .Get<RateLimitingConfiguration>() ?? new RateLimitingConfiguration();

            Assert.That(settings.Enabled, Is.True);
        }
    }
}
