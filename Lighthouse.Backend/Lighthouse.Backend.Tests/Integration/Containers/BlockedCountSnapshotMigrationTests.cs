using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// ADR-069 BlockedCountSnapshot store probe. Proves against REAL relational providers
    /// (SQLite + Postgres) that the generated AddBlockedCountSnapshot migration creates
    /// the table and that rows round-trip through the repository.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedCountSnapshotMigrationTests
    {
        [Test]
        public async Task Migration_OnSqlite_PersistsBlockedCountSnapshotTable()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-blocked-count-snapshot-{Guid.NewGuid():N}.db");
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
        public async Task Migration_OnPostgres_PersistsBlockedCountSnapshotTable()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertTableRoundTripsAsync(provider);
        }

        [Test]
        public async Task UniqueIndex_PreventsDuplicateRowForSameOwnerDay()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-blocked-count-snapshot-unique-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);

                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                var ownerId = 42;
                var ownerType = OwnerType.Team;
                var recordedAt = new DateOnly(2026, 7, 6);

                var first = new BlockedCountSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    RecordedAt = recordedAt,
                    BlockedCount = 3,
                };

                context.BlockedCountSnapshots.Add(first);
                await context.SaveChangesAsync();

                var duplicate = new BlockedCountSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    RecordedAt = recordedAt,
                    BlockedCount = 5,
                };

                context.BlockedCountSnapshots.Add(duplicate);

                var ex = Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync(),
                    "unique index on (OwnerId, OwnerType, RecordedAt) must reject duplicate rows");
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
                "the generated AddBlockedCountSnapshot migration must apply cleanly on a real provider");
        }

        private static async Task AssertTableRoundTripsAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var snapshot = new BlockedCountSnapshot
            {
                OwnerId = 7,
                OwnerType = OwnerType.Team,
                RecordedAt = new DateOnly(2026, 7, 6),
                BlockedCount = 12,
            };

            context.BlockedCountSnapshots.Add(snapshot);
            await context.SaveChangesAsync();

            var reloaded = await context.BlockedCountSnapshots
                .SingleAsync(s => s.OwnerId == 7 && s.OwnerType == OwnerType.Team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.OwnerId, Is.EqualTo(7),
                    "the migration must persist a BlockedCountSnapshot row");
                Assert.That(reloaded.OwnerType, Is.EqualTo(OwnerType.Team),
                    "OwnerType must round-trip");
                Assert.That(reloaded.RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 6)),
                    "RecordedAt must round-trip");
                Assert.That(reloaded.BlockedCount, Is.EqualTo(12),
                    "BlockedCount must round-trip");
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
