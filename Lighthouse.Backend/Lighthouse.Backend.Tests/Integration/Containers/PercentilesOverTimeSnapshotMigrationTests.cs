using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// Epic-5427 percentiles-over-time store probe (milestone-1 Scenario 6). Proves against REAL
    /// relational providers (SQLite + Postgres) that the generated AddPercentilesOverTimeSnapshot
    /// migration additively creates the table, that rows round-trip through the context, and that the
    /// natural-key unique index (OwnerId, OwnerType, MetricType, Horizon, RecordedAt) is enforced.
    /// </summary>
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class PercentilesOverTimeSnapshotMigrationTests
    {
        [Test]
        public async Task Migration_OnSqlite_PersistsPercentilesOverTimeSnapshotTable()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-percentiles-over-time-snapshot-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);
                await AssertTableRoundTripsAsync(provider);
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        [Test]
        public async Task Migration_OnPostgres_PersistsPercentilesOverTimeSnapshotTable()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertTableRoundTripsAsync(provider);
        }

        [Test]
        public async Task Migration_OnPostgres_PreservesExistingTables()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);

            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            // The migration must be additive: pre-existing sibling snapshot tables remain intact and queryable.
            var existingBlockedCount = await context.BlockedCountSnapshots.CountAsync();
            Assert.That(existingBlockedCount, Is.EqualTo(0),
                "the AddPercentilesOverTimeSnapshot migration must not drop or alter the existing BlockedCountSnapshots table");
        }

        [Test]
        public async Task UniqueIndex_PreventsDuplicateRowForSameNaturalKey()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-percentiles-over-time-snapshot-unique-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);

                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                var ownerId = 42;
                var ownerType = OwnerType.Team;
                var metricType = MetricType.CycleTime;
                int? horizon = 30;
                var recordedAt = new DateOnly(2026, 7, 6);

                var first = new PercentilesOverTimeSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    MetricType = metricType,
                    Horizon = horizon,
                    RecordedAt = recordedAt,
                    P50 = 3,
                    P70 = 5,
                    P85 = 8,
                    P95 = 13,
                };

                context.PercentilesOverTimeSnapshots.Add(first);
                await context.SaveChangesAsync();

                var duplicate = new PercentilesOverTimeSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    MetricType = metricType,
                    Horizon = horizon,
                    RecordedAt = recordedAt,
                    P50 = 4,
                    P70 = 6,
                    P85 = 9,
                    P95 = 14,
                };

                context.PercentilesOverTimeSnapshots.Add(duplicate);

                var ex = Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync(),
                    "unique index on (OwnerId, OwnerType, MetricType, Horizon, RecordedAt) must reject duplicate rows");
                Assert.That(ex!.InnerException!.Message, Does.Contain("UNIQUE").IgnoreCase,
                    "the exception must indicate a unique constraint violation");
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var pending = await context.Database.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty,
                "the generated AddPercentilesOverTimeSnapshot migration must apply cleanly on a real provider");
        }

        private static async Task AssertTableRoundTripsAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var snapshot = new PercentilesOverTimeSnapshot
            {
                OwnerId = 7,
                OwnerType = OwnerType.Team,
                MetricType = MetricType.CycleTime,
                Horizon = 30,
                RecordedAt = new DateOnly(2026, 7, 6),
                P50 = 3,
                P70 = 5,
                P85 = 8,
                P95 = 13,
            };

            context.PercentilesOverTimeSnapshots.Add(snapshot);
            await context.SaveChangesAsync();

            var reloaded = await context.PercentilesOverTimeSnapshots
                .SingleAsync(s => s.OwnerId == 7 && s.OwnerType == OwnerType.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.OwnerId, Is.EqualTo(7),
                    "the migration must persist a PercentilesOverTimeSnapshot row");
                Assert.That(reloaded.OwnerType, Is.EqualTo(OwnerType.Team),
                    "OwnerType must round-trip");
                Assert.That(reloaded.MetricType, Is.EqualTo(MetricType.CycleTime),
                    "MetricType must round-trip");
                Assert.That(reloaded.Horizon, Is.EqualTo(30),
                    "Horizon must round-trip");
                Assert.That(reloaded.RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 6)),
                    "RecordedAt must round-trip");
                Assert.That(reloaded.P50, Is.EqualTo(3), "P50 must round-trip");
                Assert.That(reloaded.P70, Is.EqualTo(5), "P70 must round-trip");
                Assert.That(reloaded.P85, Is.EqualTo(8), "P85 must round-trip");
                Assert.That(reloaded.P95, Is.EqualTo(13), "P95 must round-trip");
            }
        }

        private static ServiceProvider BuildSqliteProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

            return services.BuildServiceProvider();
        }

        private static ServiceProvider BuildPostgresProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            return services.BuildServiceProvider();
        }
    }
}
