using System.Data.Common;
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
    /// After a rollback the previous release writes its refresh rows without knowing the column that marks a
    /// skipped forecast. Those inserts must still succeed, and the rows must read as forecasts that computed,
    /// so the database itself has to fill the column in. Checked on both providers, through the generated
    /// migrations, with the row inserted the way the older build inserts it.
    /// </summary>
    [TestFixture]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    [Category("contract-shape:bounded-change")]
    public class RefreshLogSkippedColumnMigrationTests
    {
        // Started by the first Postgres case, so a run of the SQLite case alone needs no Docker.
        private SharedPostgresContainer? postgres;

        [OneTimeTearDown]
        public async Task StopPostgres()
        {
            if (postgres is not null)
            {
                await postgres.DisposeAsync();
            }
        }

        [Test]
        public async Task On_Sqlite_a_row_written_by_the_previous_release_reads_as_a_forecast_that_computed()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-refreshlog-skipped-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");

                await ThePreviousReleaseWritesAForecastRow(provider);

                Assert.That(await WhetherTheRowReadsAsSkipped(provider), Is.False);
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
        public async Task On_Postgres_a_row_written_by_the_previous_release_reads_as_a_forecast_that_computed()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();

            await using var provider = BuildPostgresProvider(postgres.ConnectionStringToANewDatabase());

            await ThePreviousReleaseWritesAForecastRow(provider);

            Assert.That(await WhetherTheRowReadsAsSkipped(provider), Is.False);
        }

        private static async Task ThePreviousReleaseWritesAForecastRow(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var type = (int)RefreshType.Forecast;
            var mode = (int)SyncMode.Full;
            var executedAt = new DateTime(2026, 10, 23, 10, 15, 0, DateTimeKind.Utc);

            await context.Database.ExecuteSqlAsync($"""
                INSERT INTO "RefreshLogs" ("Type", "EntityId", "EntityName", "ItemCount", "Mode", "RecordsScanned", "RecordsFetched",
                    "DurationMs", "ExecutedAt", "Success", "Cancelled", "RecordsWhoseLinksNamedMoreThanOneParent", "RecordsPlacedFromLinksNamingMoreThanOneParent")
                VALUES ({type}, {7}, {"Payments Platform"}, {412}, {mode}, {0}, {0}, {269719L}, {executedAt}, {true}, {false}, {0}, {0})
                """);
        }

        private static async Task<bool> WhetherTheRowReadsAsSkipped(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            // Qualified with the table on purpose: SQLite reads an unknown double-quoted name on its own as a
            // string, so an unqualified column would "exist" before the migration adds it.
            try
            {
                return await context.Database
                    .SqlQuery<bool>($"SELECT log.\"Skipped\" AS \"Value\" FROM \"RefreshLogs\" AS log")
                    .SingleAsync();
            }
            catch (DbException missingColumn)
            {
                Assert.Fail($"The refresh log has no column that marks a skipped forecast yet: {missingColumn.Message}");
                throw;
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
