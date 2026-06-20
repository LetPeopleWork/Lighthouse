using System.Text.RegularExpressions;
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
    public class MigrationsAppliedHealthCheckTest
    {
        private static readonly Regex PendingMigrationsDescription = new(@"^\d+ migration\(s\) pending\.$", RegexOptions.Compiled);

        [Test]
        public async Task CheckHealthAsync_AllMigrationsApplied_ReportsHealthy()
        {
            using var context = MigratedContext();
            var subject = new MigrationsAppliedHealthCheck(context);

            var result = await subject.CheckHealthAsync(new HealthCheckContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
                Assert.That(result.Description, Is.EqualTo("All migrations applied."));
            }
        }

        [Test]
        public async Task CheckHealthAsync_MigrationsPending_ReportsUnhealthyWithPendingCount()
        {
            using var context = PendingMigrationsContext();
            var subject = new MigrationsAppliedHealthCheck(context);

            var result = await subject.CheckHealthAsync(new HealthCheckContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
                Assert.That(result.Description, Does.Match(PendingMigrationsDescription));
            }
        }

        [Test]
        public void CheckHealthAsync_CancellationRequested_PropagatesOperationCanceled()
        {
            using var context = MigratedContext();
            var subject = new MigrationsAppliedHealthCheck(context);

            Assert.That(
                async () => await subject.CheckHealthAsync(new HealthCheckContext(), new CancellationToken(true)),
                Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public async Task CheckHealthAsync_MigrationLookupThrowsNonCancellation_ReportsUnhealthy()
        {
            var context = MigratedContext();
            await context.DisposeAsync();
            var subject = new MigrationsAppliedHealthCheck(context);

            var result = await subject.CheckHealthAsync(new HealthCheckContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
                Assert.That(result.Description, Is.EqualTo("Unable to determine migration state."));
            }
        }

        [Test]
        public void Constructor_NullDbContext_Throws()
        {
            Assert.That(() => new MigrationsAppliedHealthCheck(null!), Throws.InstanceOf<ArgumentNullException>());
        }

        private static LighthouseAppContext MigratedContext()
        {
            var context = BuildContext($"DataSource=health-migrated-{UniqueToken()}.db;Pooling=False");
            context.Database.Migrate();
            return context;
        }

        private static LighthouseAppContext PendingMigrationsContext()
        {
            var context = BuildContext($"DataSource=health-pending-{UniqueToken()}.db;Pooling=False");
            context.Database.EnsureCreated();
            return context;
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
