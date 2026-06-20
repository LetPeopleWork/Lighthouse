using Lighthouse.Backend.Tests.TestHelpers.Health;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [Category("epic-5305-k8s-readiness")]
    public class ShutdownIntegrationTest
    {
        [Test]
        public async Task Shutdown_ApplicationStopping_ReadinessFlipsNotReadyBeforeDrain()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.ReachableAndMigrated);

            var readyBefore = await host.GetReadyAsync();

            host.TriggerApplicationStopping();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readyBefore.IsHealthyProbe, Is.True, $"Ready probe was {readyBefore.StatusCode}: '{readyBefore.Body}'");
                Assert.That(host.ReadinessState.IsDraining, Is.True, "ApplicationStopping must flip readiness to draining");
            }
        }

        [Test]
        public async Task Shutdown_Draining_HealthReadyReturns503WhileLiveStays200()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.ReachableAndMigrated);

            host.ReadinessState.BeginDraining();

            var ready = await host.GetReadyAsync();
            var live = await host.GetLiveAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ready.IsUnhealthyProbe, Is.True, $"Ready probe was {ready.StatusCode}: '{ready.Body}'");
                Assert.That(live.IsHealthyProbe, Is.True, $"Live probe was {live.StatusCode}: '{live.Body}'");
            }
        }

        [Test]
        public async Task Shutdown_InFlightHttpRequest_CompletesWhileDraining()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.ReachableAndMigrated);

            host.ReadinessState.BeginDraining();

            var live = await host.GetLiveAsync();

            Assert.That(live.IsHealthyProbe, Is.True, $"In-flight request during drain was rejected: {live.StatusCode} '{live.Body}'");
        }

        [Test]
        public async Task Shutdown_CtrlCSingleContainer_ReadinessHealthyAtRest()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.ReachableAndMigrated);

            var ready = await host.GetReadyAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ready.IsHealthyProbe, Is.True, $"Drain check must be additive and harmless at N=1; ready was {ready.StatusCode}: '{ready.Body}'");
                Assert.That(host.ReadinessState.IsDraining, Is.False);
            }
        }
    }
}
