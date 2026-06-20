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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
                Assert.That(result.Description, Is.Not.Empty);
            }
        }

        [Test]
        public async Task CheckHealthAsync_Draining_ReportsUnhealthy()
        {
            var readinessState = new ReadinessState();
            readinessState.BeginDraining();
            var subject = new DrainReadinessHealthCheck(readinessState);

            var result = await subject.CheckHealthAsync(new HealthCheckContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
                Assert.That(result.Description, Is.Not.Empty);
            }
        }

        [Test]
        public void Constructor_NullReadinessState_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DrainReadinessHealthCheck(null!));
        }
    }
}
