using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// DISTILL acceptance probe (Epic 5074) — Slice 05: the additive <c>IsPredefined</c> column on the
    /// <c>AdditionalFieldDefinition</c> table (default false — system-owned flag, ADR-071). Proves against
    /// SQLite + Postgres that the generated additive migration applies cleanly and the column defaults to
    /// false for a newly-inserted field. InMemory misses migrations — real providers required (mirrors
    /// <see cref="BlockedStalenessThresholdMigrationTests"/>).
    ///
    /// [Ignore]-pending: enable in DELIVER after generating the AddIsPredefinedToAdditionalFieldDefinition
    /// migration. It reads the column via raw SQL (not via a not-yet-existing model member) so it COMPILES
    /// against today's types; it fails RED today because the column does not exist.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    [Category("slice-05")]
    public class PredefinedAdditionalFieldMigrationTests
    {
        [Test]
        [Ignore("DELIVER slice-05 — additive IsPredefined column + migration not yet authored")]
        public async Task Migration_OnSqlite_PersistsColumnAndDefaultsToFalse()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-pf-5074-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);
                await AssertIsPredefinedDefaultsToFalseAsync(provider);
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
        [Ignore("DELIVER slice-05 — additive IsPredefined column + migration not yet authored")]
        public async Task Migration_OnPostgres_PersistsColumnAndDefaultsToFalse()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertIsPredefinedDefaultsToFalseAsync(provider);
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var pending = await context.Database.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty, "the generated AddIsPredefinedToAdditionalFieldDefinition migration must apply cleanly on a real provider");
        }

        private static async Task AssertIsPredefinedDefaultsToFalseAsync(ServiceProvider provider)
        {
            int fieldId;
            using (var writeScope = provider.CreateScope())
            {
                var context = writeScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var connection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                    AdditionalFieldDefinitions =
                    {
                        new AdditionalFieldDefinition { DisplayName = "Team", Reference = "customfield_10050" },
                    },
                };

                context.WorkTrackingSystemConnections.Add(connection);
                await context.SaveChangesAsync();
                fieldId = connection.AdditionalFieldDefinitions[0].Id;
            }

            using var readScope = provider.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var isPredefined = await ReadIsPredefinedColumnAsync(readContext, fieldId);

            Assert.That(isPredefined, Is.False, "IsPredefined must default to false for a user-created additional field");
        }

        private static async Task<bool> ReadIsPredefinedColumnAsync(LighthouseAppContext context, int fieldId)
        {
            var dbConnection = context.Database.GetDbConnection();
            await dbConnection.OpenAsync();
            try
            {
                using var command = dbConnection.CreateCommand();
                command.CommandText = "SELECT \"IsPredefined\" FROM \"AdditionalFieldDefinition\" WHERE \"Id\" = @id";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@id";
                parameter.Value = fieldId;
                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToBoolean(result);
            }
            catch (DbException exception)
            {
                Assert.Fail($"The IsPredefined column is absent on AdditionalFieldDefinition — the additive slice-05 migration is not yet authored. {exception.Message}");
                return false;
            }
            finally
            {
                await dbConnection.CloseAsync();
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
