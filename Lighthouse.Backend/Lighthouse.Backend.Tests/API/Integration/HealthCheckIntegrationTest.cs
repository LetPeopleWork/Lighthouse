using Lighthouse.Backend.Tests.TestHelpers.Health;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [Category("epic-5305-k8s-readiness")]
    public class HealthCheckIntegrationTest
    {
        [Test]
        public async Task HealthLive_ProcessUp_Returns200()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.ReachableAndMigrated);

            var live = await host.GetLiveAsync();

            Assert.That(live.IsHealthyProbe, Is.True, $"Live probe was {live.StatusCode}: '{live.Body}'");
        }

        [Test]
        public async Task HealthLive_DependencyDownOrSlow_StillReturns200()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.Unreachable);

            var live = await host.GetLiveAsync();

            Assert.That(live.IsHealthyProbe, Is.True, $"Live probe was {live.StatusCode}: '{live.Body}'");
        }

        [Test]
        public async Task HealthReady_DbReachableAndMigrationsApplied_Returns200()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.ReachableAndMigrated);

            var ready = await host.GetReadyAsync();

            Assert.That(ready.IsHealthyProbe, Is.True, $"Ready probe was {ready.StatusCode}: '{ready.Body}'");
        }

        [Test]
        public async Task HealthReady_DbUnreachable_Returns503WhileLiveStays200()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.Unreachable);

            var ready = await host.GetReadyAsync();
            var live = await host.GetLiveAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ready.IsUnhealthyProbe, Is.True, $"Ready probe was {ready.StatusCode}: '{ready.Body}'");
                Assert.That(live.IsHealthyProbe, Is.True, $"Live probe was {live.StatusCode}: '{live.Body}'");
            }
        }

        [Test]
        public async Task HealthReady_MigrationsPending_Returns503()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.ReachableWithPendingMigrations);

            var ready = await host.GetReadyAsync();

            Assert.That(ready.IsUnhealthyProbe, Is.True, $"Ready probe was {ready.StatusCode}: '{ready.Body}'");
        }

        [Test]
        public async Task HealthStartup_CoversSlowBoot_503ThenHealthy()
        {
            using var bootingHost = new HealthCheckTestHost(HealthDatabaseState.ReachableWithPendingMigrations);
            var startupWhileBooting = await bootingHost.GetStartupAsync();

            using var bootedHost = new HealthCheckTestHost(HealthDatabaseState.ReachableAndMigrated);
            var startupWhenReady = await bootedHost.GetStartupAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(startupWhileBooting.IsUnhealthyProbe, Is.True, $"Booting startup probe was {startupWhileBooting.StatusCode}: '{startupWhileBooting.Body}'");
                Assert.That(startupWhenReady.IsHealthyProbe, Is.True, $"Booted startup probe was {startupWhenReady.StatusCode}: '{startupWhenReady.Body}'");
            }
        }

        [Test]
        public async Task Health_SingleContainerNoOrchestrator_EndpointsHarmless200()
        {
            using var host = new HealthCheckTestHost(HealthDatabaseState.ReachableAndMigrated);

            var live = await host.GetLiveAsync();
            var ready = await host.GetReadyAsync();
            var startup = await host.GetStartupAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(live.IsHealthyProbe, Is.True, $"Live probe was {live.StatusCode}: '{live.Body}'");
                Assert.That(ready.IsHealthyProbe, Is.True, $"Ready probe was {ready.StatusCode}: '{ready.Body}'");
                Assert.That(startup.IsHealthyProbe, Is.True, $"Startup probe was {startup.StatusCode}: '{startup.Body}'");
            }
        }
    }
}
