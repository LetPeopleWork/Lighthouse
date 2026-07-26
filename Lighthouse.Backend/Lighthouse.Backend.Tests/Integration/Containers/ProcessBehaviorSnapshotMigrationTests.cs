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
    /// Epic-5427 process-behaviour store probe (milestone-1 Scenario 6, the ProcessBehaviorSnapshot
    /// half deferred from slice-01 by design). Proves against REAL relational providers (SQLite +
    /// Postgres) that the generated AddProcessBehaviorSnapshot migration additively creates the table,
    /// that rows round-trip through the context, that the pre-existing sibling snapshot tables survive,
    /// and that the four-part natural-key unique index (OwnerId, OwnerType, MetricType, RecordedAt) is
    /// enforced. EF InMemory cannot see the unique index — only a real provider can.
    /// </summary>
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class ProcessBehaviorSnapshotMigrationTests
    {
        [Test]
        public async Task Migration_OnSqlite_PersistsProcessBehaviorSnapshotTable()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-process-behavior-snapshot-{Guid.NewGuid():N}.db");
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
        public async Task Migration_OnPostgres_PersistsProcessBehaviorSnapshotTable()
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
            var existingPercentilesCount = await context.PercentilesOverTimeSnapshots.CountAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(existingBlockedCount, Is.Zero,
                    "the AddProcessBehaviorSnapshot migration must not drop or alter the existing BlockedCountSnapshots table");
                Assert.That(existingPercentilesCount, Is.Zero,
                    "the AddProcessBehaviorSnapshot migration must not drop or alter the slice-01 PercentilesOverTimeSnapshots table");
            }
        }

        [Test]
        public async Task UniqueIndex_OnPostgres_PreventsDuplicateRowForSameNaturalKey()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);

            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.ProcessBehaviorSnapshots.Add(Snapshot(42, OwnerType.Team, new DateOnly(2026, 7, 6), 13, 8, 3));
            await context.SaveChangesAsync();

            context.ProcessBehaviorSnapshots.Add(Snapshot(42, OwnerType.Team, new DateOnly(2026, 7, 6), 20, 12, 4));

            var exception = Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync(),
                "unique index on (OwnerId, OwnerType, MetricType, RecordedAt) must reject duplicate rows");
            Assert.That(exception!.InnerException!.Message, Does.Contain("unique").IgnoreCase,
                "the exception must indicate a unique constraint violation");
        }

        [Test]
        public async Task UniqueIndex_OnSqlite_PreventsDuplicateRowForSameNaturalKey()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-process-behavior-snapshot-unique-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);

                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                context.ProcessBehaviorSnapshots.Add(Snapshot(42, OwnerType.Team, new DateOnly(2026, 7, 6), 13, 8, 3));
                await context.SaveChangesAsync();

                context.ProcessBehaviorSnapshots.Add(Snapshot(42, OwnerType.Team, new DateOnly(2026, 7, 6), 20, 12, 4));

                var exception = Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync(),
                    "unique index on (OwnerId, OwnerType, MetricType, RecordedAt) must reject duplicate rows");
                Assert.That(exception!.InnerException!.Message, Does.Contain("UNIQUE").IgnoreCase,
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

        [Test]
        public async Task UniqueIndex_OnSqlite_AllowsRowsDifferingInASingleNaturalKeyPart()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-process-behavior-snapshot-neighbours-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);

                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                context.ProcessBehaviorSnapshots.Add(Snapshot(42, OwnerType.Team, new DateOnly(2026, 7, 6), 13, 8, 3));
                context.ProcessBehaviorSnapshots.Add(Snapshot(43, OwnerType.Team, new DateOnly(2026, 7, 6), 13, 8, 3));
                context.ProcessBehaviorSnapshots.Add(Snapshot(42, OwnerType.Portfolio, new DateOnly(2026, 7, 6), 13, 8, 3));
                context.ProcessBehaviorSnapshots.Add(Snapshot(42, OwnerType.Team, new DateOnly(2026, 7, 7), 13, 8, 3));
                await context.SaveChangesAsync();

                var stored = await context.ProcessBehaviorSnapshots.CountAsync();
                Assert.That(stored, Is.EqualTo(4),
                    "rows differing in exactly one natural-key part are distinct snapshots and must all persist");
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        private static ProcessBehaviorSnapshot Snapshot(
            int ownerId, OwnerType ownerType, DateOnly recordedAt, int unpl, int average, int lnpl)
        {
            return new ProcessBehaviorSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                MetricType = ProcessBehaviorMetricType.Throughput,
                RecordedAt = recordedAt,
                Unpl = unpl,
                Average = average,
                Lnpl = lnpl,
            };
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var pending = await context.Database.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty,
                "the generated AddProcessBehaviorSnapshot migration must apply cleanly on a real provider");
        }

        private static async Task AssertTableRoundTripsAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.ProcessBehaviorSnapshots.Add(Snapshot(7, OwnerType.Team, new DateOnly(2026, 7, 6), 13, 8, 3));
            await context.SaveChangesAsync();

            var reloaded = await context.ProcessBehaviorSnapshots
                .SingleAsync(s => s.OwnerId == 7 && s.OwnerType == OwnerType.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.OwnerId, Is.EqualTo(7),
                    "the migration must persist a ProcessBehaviorSnapshot row");
                Assert.That(reloaded.OwnerType, Is.EqualTo(OwnerType.Team), "OwnerType must round-trip");
                Assert.That(reloaded.MetricType, Is.EqualTo(ProcessBehaviorMetricType.Throughput), "MetricType must round-trip");
                Assert.That(reloaded.RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 6)), "RecordedAt must round-trip");
                Assert.That(reloaded.Unpl, Is.EqualTo(13), "Unpl must round-trip");
                Assert.That(reloaded.Average, Is.EqualTo(8), "Average must round-trip");
                Assert.That(reloaded.Lnpl, Is.EqualTo(3), "Lnpl must round-trip");
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
