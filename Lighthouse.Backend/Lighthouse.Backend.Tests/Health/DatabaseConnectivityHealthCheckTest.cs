using Lighthouse.Backend.Data;
using Lighthouse.Backend.Health;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Health
{
    [TestFixture]
    [Category("epic-5305-k8s-readiness")]
    public class DatabaseConnectivityHealthCheckTest
    {
        [Test]
        public async Task CheckHealthAsync_DatabaseReachable_ReportsHealthy()
        {
            using var context = ReachableContext();
            var subject = new DatabaseConnectivityHealthCheck(context);

            var result = await subject.CheckHealthAsync(new HealthCheckContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
                Assert.That(result.Description, Is.EqualTo("Database reachable."));
            }
        }

        [Test]
        public async Task CheckHealthAsync_DatabaseUnreachableWithoutException_ReportsUnhealthy()
        {
            using var context = UnreachableContext();
            var subject = new DatabaseConnectivityHealthCheck(context);

            var result = await subject.CheckHealthAsync(new HealthCheckContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
                Assert.That(result.Description, Is.EqualTo("Database unreachable."));
            }
        }

        [Test]
        public void CheckHealthAsync_CancellationRequested_PropagatesOperationCanceled()
        {
            using var context = ReachableContext();
            var subject = new DatabaseConnectivityHealthCheck(context);

            Assert.That(
                async () => await subject.CheckHealthAsync(new HealthCheckContext(), new CancellationToken(true)),
                Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public async Task CheckHealthAsync_ConnectivityCheckThrowsNonCancellation_ReportsUnhealthy()
        {
            var context = ReachableContext();
            await context.DisposeAsync();
            var subject = new DatabaseConnectivityHealthCheck(context);

            var result = await subject.CheckHealthAsync(new HealthCheckContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
                Assert.That(result.Description, Is.EqualTo("Database unreachable."));
            }
        }

        [Test]
        public void Constructor_NullDbContext_Throws()
        {
            Assert.That(() => new DatabaseConnectivityHealthCheck(null!), Throws.InstanceOf<ArgumentNullException>());
        }

        private static LighthouseAppContext ReachableContext()
        {
            var context = BuildContext($"DataSource=health-conn-{UniqueToken()}.db;Pooling=False");
            context.Database.EnsureCreated();
            return context;
        }

        private static LighthouseAppContext UnreachableContext()
        {
            return BuildContext($"DataSource=health-conn-missing-{UniqueToken()}.db;Mode=ReadOnly;Pooling=False");
        }

        private static LighthouseAppContext BuildContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LighthouseAppContext>();
            optionsBuilder.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite"));

            return new LighthouseAppContext(optionsBuilder.Options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
        }

        private static string UniqueToken() => Path.GetRandomFileName().Replace(".", "");
    }
}
