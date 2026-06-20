using Lighthouse.Backend.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lighthouse.Backend.Tests.Health
{
    [Category("epic-5305-k8s-readiness")]
    public class DrainReadinessHealthCheckTest
    {
        [Test]
        public async Task CheckHealthAsync_NotDraining_ReportsHealthy()
        {
            var readinessState = new ReadinessState();
            var subject = new DrainReadinessHealthCheck(readinessState);

            var result = await subject.CheckHealthAsync(new HealthCheckContext());

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        }

        [Test]
        public async Task CheckHealthAsync_Draining_ReportsUnhealthy()
        {
            var readinessState = new ReadinessState();
            readinessState.BeginDraining();
            var subject = new DrainReadinessHealthCheck(readinessState);

            var result = await subject.CheckHealthAsync(new HealthCheckContext());

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        }
    }
}
