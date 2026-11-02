using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.Health
{
    [TestFixture]
    public class ClusterSubstrateProbeRegistrationTest
    {
        private const string ReadyTag = "ready";
        private const string StartupTag = "startup";

        // The substrate probe is destructive by design (pg_terminate_backend to test reclaim). It must
        // gate startup but NOT run on every readiness probe, or a healthy fleet logs 57P01 forever
        // (epic-5306 manual-test finding). Lock it to the startup endpoint only.
        [Test]
        public void ClusterSubstrate_WhenRedisConfigured_IsStartupOnly_NotReadiness()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration["ConnectionStrings:Redis"] = "localhost:6399";

            Program.ConfigureHealthChecks(builder);

            using var provider = builder.Services.BuildServiceProvider();
            var registration = provider
                .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
                .Value.Registrations
                .SingleOrDefault(r => r.Name == "cluster-substrate");

            Assert.That(registration, Is.Not.Null, "cluster-substrate must be registered when Redis is configured");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(registration!.Tags, Does.Contain(StartupTag), "substrate must gate startup");
                Assert.That(registration.Tags, Does.Not.Contain(ReadyTag), "substrate must NOT run on readiness (it is destructive)");
            }
        }
    }
}
