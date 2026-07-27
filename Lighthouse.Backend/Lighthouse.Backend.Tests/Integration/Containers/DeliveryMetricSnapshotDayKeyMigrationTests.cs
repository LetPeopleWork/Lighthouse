using System.Globalization;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Data.Converters;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.Architecture;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// Bug #5567 step 02-02 - DeliveryMetricSnapshot converges on a DateOnly day key.
    ///
    /// The honest claim under test is NOT "a second row per visual day is prevented": the shipped
    /// repository already normalised every write to midnight under a unique (DeliveryId, RecordedAt)
    /// index, so a range scan over that data WAS equality on a day key. What is new and testable is
    /// (a) the day key is a DateOnly and therefore structurally out of reach of the global
    /// Properties&lt;DateTime&gt;() UTC converter, (b) existing rows survive the backfill on their
    /// original day on both real providers, (c) the migration is additive-only, and (d) a colliding
    /// population aborts the migration with a diagnostic naming the offending rows instead of being
    /// silently de-duplicated.
    /// </summary>
    [TestFixture]
    [Category("bug-5567-utc-today-anchor")]
    public class DeliveryMetricSnapshotDayKeyMigrationTests
    {
        public enum ProviderUnderTest
        {
            Sqlite,
            Postgres,
        }

        private static readonly string[] LegacyColumnsThatMustSurvive = ["RecordedAt"];

        private static readonly DateTime[] SeededInstants =
        [
            new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc),
        ];

        private static readonly DateTime[] CollidingInstants =
        [
            new DateTime(2026, 5, 25, 6, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 25, 18, 0, 0, DateTimeKind.Utc),
        ];

        /// <summary>
        /// CHARACTERIZATION test. This behaviour already worked at HEAD via the range scan; it is
        /// pinned here so the move to a DateOnly key cannot break it, NOT as evidence for the step.
        /// </summary>
        [Test]
        public async Task RecordedDay_IsADateOnlyKey_AndTheSameDayWriteUpsertsInPlace()
        {
            await WithSqliteAsync(async provider =>
            {
                await MigrateToLatestAsync(provider);

                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var deliveryId = await GivenPersistedDeliveryAsync(context);

                var day = new DateOnly(2026, 5, 25);
                context.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
                {
                    DeliveryId = deliveryId,
                    RecordedDay = day,
                    RecordedAt = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    TotalWork = 10,
                });
                await context.SaveChangesAsync();

                var sameDayRow = await context.DeliveryMetricSnapshots
                    .SingleAsync(snapshot => snapshot.DeliveryId == deliveryId && snapshot.RecordedDay == day);
                sameDayRow.TotalWork = 25;
                await context.SaveChangesAsync();

                var rows = await context.DeliveryMetricSnapshots
                    .Where(snapshot => snapshot.DeliveryId == deliveryId)
                    .ToListAsync();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(rows, Has.Count.EqualTo(1),
                        "a second write for the same delivery-day must update in place, not append");
                    Assert.That(rows[0].TotalWork, Is.EqualTo(25),
                        "the in-place update must be the one that survived");
                    Assert.That(rows[0].RecordedDay, Is.EqualTo(day),
                        "the day key must be the DateOnly that was written");
                }
            });
        }

        [TestCase(ProviderUnderTest.Sqlite)]
        [TestCase(ProviderUnderTest.Postgres)]
        public async Task ExistingRows_BackfillToTheirOriginalDay(ProviderUnderTest providerUnderTest)
        {
            await WithProviderAsync(providerUnderTest, async provider =>
            {
                var deliveryId = await SeedLegacyRowsUnderPreviousSchemaAsync(provider, SeededInstants);

                await MigrateToLatestAsync(provider);

                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var rows = await context.DeliveryMetricSnapshots
                    .AsNoTracking()
                    .Where(snapshot => snapshot.DeliveryId == deliveryId)
                    .OrderBy(snapshot => snapshot.RecordedDay)
                    .ToListAsync();

                var expectedDays = SeededInstants.Select(DateOnly.FromDateTime).ToList();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(rows, Has.Count.EqualTo(SeededInstants.Length),
                        "the backfill must lose no row");
                    Assert.That(rows.Select(row => row.RecordedDay).ToList(), Is.EqualTo(expectedDays),
                        "every backfilled day key must equal the calendar date of that row's original RecordedAt - no shift");
                    Assert.That(rows.Select(row => row.RecordedAt).ToList(), Is.EqualTo(SeededInstants.ToList()),
                        "the legacy instant column must be left untouched by the backfill");
                }
            });
        }

        [Test]
        public void Migration_IsAdditiveOnly_AndDropsNothing()
        {
            var migrationFiles = GeneratedMigrationFiles();

            Assert.That(migrationFiles, Has.Count.EqualTo(2),
                "Create-Migration.ps1 must have produced one migration per provider");

            using (Assert.EnterMultipleScope())
            {
                foreach (var file in migrationFiles)
                {
                    var upBody = ExpandOnlyMigrationGuard.ExtractUpMethodBody(File.ReadAllText(file));

                    Assert.That(
                        ExpandOnlyMigrationGuard.FindDestructiveOperationsInUp(File.ReadAllText(file)),
                        Is.Empty,
                        $"{Path.GetFileName(file)} must be expand-only");
                    Assert.That(upBody, Does.Not.Contain("DropIndex("),
                        $"{Path.GetFileName(file)} must keep the legacy (DeliveryId, RecordedAt) unique index during the expand phase");
                    Assert.That(upBody, Does.Not.Contain("DELETE"),
                        $"{Path.GetFileName(file)} must not delete rows - deduplication was considered and rejected");
                }
            }
        }

        [Test]
        public async Task Migration_KeepsTheLegacyColumnAndIndex_OnSqlite()
        {
            await WithSqliteAsync(async provider =>
            {
                await MigrateToLatestAsync(provider);

                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                var indexes = await ReadSingleColumnAsync(
                    context,
                    "SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = 'DeliveryMetricSnapshots'");
                var columns = await ReadSingleColumnAsync(
                    context,
                    "SELECT name FROM pragma_table_info('DeliveryMetricSnapshots')");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(columns, Is.SupersetOf(LegacyColumnsThatMustSurvive),
                        "expand-only: the legacy RecordedAt column must still exist after the migration");
                    Assert.That(columns, Does.Contain("RecordedDay"),
                        "the migration must add the DateOnly day key column");
                    Assert.That(indexes, Does.Contain("IX_DeliveryMetricSnapshots_DeliveryId_RecordedAt"),
                        "expand-only: the legacy unique index must survive the migration");
                    Assert.That(indexes, Does.Contain("IX_DeliveryMetricSnapshots_DeliveryId_RecordedDay"),
                        "the migration must add the day-key unique index");
                }
            });
        }

        [TestCase(ProviderUnderTest.Sqlite)]
        [TestCase(ProviderUnderTest.Postgres)]
        public async Task CollidingRows_AbortTheMigrationWithADiagnosticNamingThem(ProviderUnderTest providerUnderTest)
        {
            await WithProviderAsync(providerUnderTest, async provider =>
            {
                // Two rows for one delivery that reduce to the same calendar day. Unreachable through
                // the current writer, reachable from a restored backup or an older version's database.
                var deliveryId = await SeedLegacyRowsUnderPreviousSchemaAsync(provider, CollidingInstants);

                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                var exception = Assert.Throws<InvalidOperationException>(
                    () => DeliveryMetricSnapshotDayCollisionGuard.EnsureNoCollisions(context),
                    "a colliding population must abort the migration rather than be silently de-duplicated");

                var survivingRows = await context.DeliveryMetricSnapshots.AsNoTracking().CountAsync();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(exception!.Message, Does.Contain(deliveryId.ToString(CultureInfo.InvariantCulture)),
                        "the diagnostic must name the colliding DeliveryId");
                    Assert.That(exception.Message, Does.Contain("2026-05-25"),
                        "the diagnostic must name the colliding calendar day");
                    Assert.That(survivingRows, Is.EqualTo(CollidingInstants.Length),
                        "the guard must not repair, collapse or delete anything - deduplication is forbidden");
                }
            });
        }

        /// <summary>
        /// The R1 property this whole step exists for: the global convention attaches
        /// UtcDateTimeConverter to Properties&lt;DateTime&gt;() only, so the day key cannot be
        /// shifted by a zone conversion - not on write, not on a query parameter. Asserted
        /// structurally (no converter on the property) AND behaviourally (a Kind=Local instant on
        /// the sibling DateTime column goes through the converter while the DateOnly does not).
        /// </summary>
        [Test]
        public async Task RecordedDay_IsExemptFromTheGlobalUtcDateTimeConverter()
        {
            await WithSqliteAsync(async provider =>
            {
                await MigrateToLatestAsync(provider);

                int deliveryId;
                var day = new DateOnly(2026, 5, 25);
                var localInstant = DateTime.SpecifyKind(new DateTime(2026, 5, 25, 0, 30, 0), DateTimeKind.Local);

                using (var writeScope = provider.CreateScope())
                {
                    var writeContext = writeScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                    deliveryId = await GivenPersistedDeliveryAsync(writeContext);

                    var entityType = writeContext.Model.FindEntityType(typeof(DeliveryMetricSnapshot))!;

                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(
                            entityType.FindProperty(nameof(DeliveryMetricSnapshot.RecordedDay))!.GetValueConverter(),
                            Is.Null,
                            "the DateOnly day key must carry no value converter - that exemption is the point of the step");
                        Assert.That(
                            entityType.FindProperty(nameof(DeliveryMetricSnapshot.RecordedAt))!.GetValueConverter(),
                            Is.TypeOf<UtcDateTimeConverter>(),
                            "the legacy DateTime column must still be converted, which is what the day key escapes");
                    }

                    writeContext.DeliveryMetricSnapshots.Add(new DeliveryMetricSnapshot
                    {
                        DeliveryId = deliveryId,
                        RecordedDay = day,
                        RecordedAt = localInstant,
                    });
                    await writeContext.SaveChangesAsync();
                }

                using var readScope = provider.CreateScope();
                var readContext = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var reloaded = await readContext.DeliveryMetricSnapshots
                    .AsNoTracking()
                    .SingleAsync(snapshot => snapshot.DeliveryId == deliveryId);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(reloaded.RecordedDay, Is.EqualTo(day),
                        "the DateOnly day key must round-trip byte-identically even when the sibling instant is Kind=Local");
                    Assert.That(reloaded.RecordedAt, Is.EqualTo(localInstant.ToUniversalTime()),
                        "the sibling DateTime went through UtcDateTimeConverter - proof the converter is live on this entity");
                }
            });
        }

        /// <summary>
        /// Brings the database up to the migration IMMEDIATELY BEFORE the one under test and writes
        /// snapshot rows through raw SQL, so the rows exist exactly as an older release left them -
        /// no RecordedDay column, no day-key index. Returns the delivery they belong to.
        /// </summary>
        private static async Task<int> SeedLegacyRowsUnderPreviousSchemaAsync(ServiceProvider provider, DateTime[] instants)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            await context.GetService<IMigrator>().MigrateAsync(PreviousMigrationId(context));

            var deliveryId = await GivenPersistedDeliveryAsync(context);

            foreach (var instant in instants)
            {
                await InsertLegacySnapshotAsync(context, deliveryId, instant);
            }

            return deliveryId;
        }

        private static async Task InsertLegacySnapshotAsync(LighthouseAppContext context, int deliveryId, DateTime recordedAt)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "DeliveryMetricSnapshots" ("DeliveryId", "RecordedAt", "TotalWork", "DoneWork", "RemainingWork")
                VALUES ({0}, {1}, 0, 0, 0)
                """,
                deliveryId,
                recordedAt);
        }

        private static async Task<int> GivenPersistedDeliveryAsync(LighthouseAppContext context)
        {
            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Connection",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
            };

            context.Portfolios.Add(portfolio);
            await context.SaveChangesAsync();

            var delivery = new Delivery("Release 1", new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), portfolio.Id);
            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            return delivery.Id;
        }

        private static string PreviousMigrationId(LighthouseAppContext context)
        {
            var migrations = context.Database.GetMigrations().ToList();
            var index = migrations.FindIndex(id =>
                id.EndsWith(DeliveryMetricSnapshotDayCollisionGuard.GuardedMigrationSuffix, StringComparison.Ordinal));

            Assert.That(index, Is.GreaterThan(0),
                "the AddDeliveryMetricSnapshotRecordedDay migration must exist and must not be the first migration");

            return migrations[index - 1];
        }

        private static Task WithProviderAsync(ProviderUnderTest providerUnderTest, Func<ServiceProvider, Task> assertion)
        {
            return providerUnderTest == ProviderUnderTest.Sqlite
                ? WithSqliteAsync(assertion)
                : WithPostgresAsync(assertion);
        }

        private static async Task MigrateToLatestAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            DeliveryMetricSnapshotDayCollisionGuard.EnsureNoCollisions(context);
            await context.Database.MigrateAsync();

            var pending = await context.Database.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty,
                "the generated AddDeliveryMetricSnapshotRecordedDay migration must apply cleanly on a real provider");
        }

        private static async Task<List<string>> ReadSingleColumnAsync(LighthouseAppContext context, string sql)
        {
            var values = new List<string>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(0));
            }

            return values;
        }

        private static List<string> GeneratedMigrationFiles()
        {
            var root = RepositoryRoot();
            var files = new List<string>();

            foreach (var project in new[] { "Lighthouse.Migrations.Postgres", "Lighthouse.Migrations.Sqlite" })
            {
                files.AddRange(Directory.EnumerateFiles(
                    Path.Combine(root, project, "Migrations"),
                    "*_AddDeliveryMetricSnapshotRecordedDay.cs",
                    SearchOption.TopDirectoryOnly));
            }

            return files;
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the migration-source scan.");
            return directory!.FullName;
        }

        private static async Task WithSqliteAsync(Func<ServiceProvider, Task> assertion)
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-delivery-metric-day-key-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await assertion(provider);
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        private static async Task WithPostgresAsync(Func<ServiceProvider, Task> assertion)
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await assertion(provider);
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
