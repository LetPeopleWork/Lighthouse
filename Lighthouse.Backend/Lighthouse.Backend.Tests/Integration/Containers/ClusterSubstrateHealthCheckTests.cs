using Lighthouse.Backend.Data;
using Lighthouse.Backend.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class ClusterSubstrateHealthCheckTests
    {
        [Test]
        public async Task Probe_RealPostgresAndRedis_ReportsHealthy()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var healthCheck = CreateHealthCheck(postgres.GetConnectionString(), multiplexer);

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy), result.Description);
        }

        [Test]
        public async Task Probe_SubstrateUnreachable_RefusesStartupWithStructuredEvent()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var unreachablePostgres = "Host=127.0.0.1;Port=1;Database=none;Username=x;Password=y;Timeout=1;Command Timeout=1";
            var healthCheck = CreateHealthCheck(unreachablePostgres, multiplexer);

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
                Assert.That(result.Description, Does.StartWith("health.startup.refused:"),
                    "a substrate that cannot satisfy the Earned-Trust probe refuses startup with a structured " +
                    "health.startup.refused event naming the lie — driving /health/startup Unhealthy so the pod never serves");
            }
        }

        private static ClusterSubstrateHealthCheck CreateHealthCheck(string postgresConnectionString, IConnectionMultiplexer multiplexer)
        {
            return new ClusterSubstrateHealthCheck(
                Options.Create(new DatabaseConfiguration { Provider = "Postgresql", ConnectionString = postgresConnectionString }),
                multiplexer);
        }
    }
}
