using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// The upgrade that lets Teams and Portfolios follow a default set on their connection, on both real
    /// providers. Four owners of each setting exist before it: one names a field of its own connection, one names
    /// nothing, one names a field that has since been deleted, and one names a field that belongs to another
    /// connection. After the upgrade the first owns its field; the other three follow the connection default,
    /// which nobody has set yet, so they read nothing. The first two read exactly what they read before, so their
    /// fingerprints, which decide whether the next update downloads everything, come out the same; the last two
    /// read the tracker's own value again, so theirs move and they download everything once.
    ///
    /// The source column of each setting is read with raw SQL, so this test needs nothing from the model that
    /// the upgrade adds. The owners are written with today's model, which still lets a value point anywhere.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6304")]
    [Category("story-6313")]
    [Category("contract-shape:bounded-change")]
    public class ConnectionDefaultsUpgradeMigrationTests
    {
        private const string PreviousMigrationIdSqlite = "20261015132326_AddPlacedParentCountToRefreshLog";

        private const string PreviousMigrationIdPostgres = "20261015132336_AddPlacedParentCountToRefreshLog";

        private const int AFieldThatWasDeleted = 987_654;

        private const int ConnectionDefault = 0;

        private const int Own = 1;

        public enum UpgradedSetting
        {
            TeamsParentOverrideField,
            PortfoliosParentOverrideField,
            PortfoliosDependencyField,
            PortfoliosSizeEstimateField,
            PortfoliosFeatureOwnerField,
            TeamsEstimation,
            PortfoliosEstimation,
        }

        private enum Before
        {
            NamesAFieldOfItsConnection,
            NamesNothing,
            NamesADeletedField,
            NamesAFieldOfAnotherConnection,
        }

        private sealed record Seeded(Before Was, int Id, string FingerprintBefore);

        [TestCase(UpgradedSetting.TeamsParentOverrideField, Ignore = PendingEpic6303.Slice05a, Category = "slice-05a,ac-4.4")]
        [TestCase(UpgradedSetting.PortfoliosParentOverrideField, Ignore = PendingEpic6303.Slice08, Category = "slice-08,ac-7.5")]
        [TestCase(UpgradedSetting.PortfoliosDependencyField, Ignore = PendingEpic6303.Slice09, Category = "slice-09,ac-8.5")]
        [TestCase(UpgradedSetting.PortfoliosSizeEstimateField, Ignore = PendingEpic6303.Slice10, Category = "slice-10,ac-9.5")]
        [TestCase(UpgradedSetting.PortfoliosFeatureOwnerField, Ignore = PendingEpic6303.Slice11, Category = "slice-11,ac-10.5")]
        [TestCase(UpgradedSetting.TeamsEstimation, Ignore = PendingEpic6303.Slice12, Category = "slice-12,ac-11.6")]
        [TestCase(UpgradedSetting.PortfoliosEstimation, Ignore = PendingEpic6303.Slice12, Category = "slice-12,ac-11.6")]
        public async Task On_SQLite_owners_that_named_nothing_or_a_missing_field_follow_and_owners_keep_their_field(UpgradedSetting setting)
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-defaults-6303-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await TheUpgradeTurnsTheOwnersIntoFollowersOrOwners(provider, PreviousMigrationIdSqlite, setting);
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        [TestCase(UpgradedSetting.TeamsParentOverrideField, Ignore = PendingEpic6303.Slice05a, Category = "slice-05a,ac-4.4")]
        [TestCase(UpgradedSetting.PortfoliosParentOverrideField, Ignore = PendingEpic6303.Slice08, Category = "slice-08,ac-7.5")]
        [TestCase(UpgradedSetting.PortfoliosDependencyField, Ignore = PendingEpic6303.Slice09, Category = "slice-09,ac-8.5")]
        [TestCase(UpgradedSetting.PortfoliosSizeEstimateField, Ignore = PendingEpic6303.Slice10, Category = "slice-10,ac-9.5")]
        [TestCase(UpgradedSetting.PortfoliosFeatureOwnerField, Ignore = PendingEpic6303.Slice11, Category = "slice-11,ac-10.5")]
        [TestCase(UpgradedSetting.TeamsEstimation, Ignore = PendingEpic6303.Slice12, Category = "slice-12,ac-11.6")]
        [TestCase(UpgradedSetting.PortfoliosEstimation, Ignore = PendingEpic6303.Slice12, Category = "slice-12,ac-11.6")]
        public async Task On_Postgres_owners_that_named_nothing_or_a_missing_field_follow_and_owners_keep_their_field(UpgradedSetting setting)
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await TheUpgradeTurnsTheOwnersIntoFollowersOrOwners(provider, PreviousMigrationIdPostgres, setting);
        }

        private static async Task TheUpgradeTurnsTheOwnersIntoFollowersOrOwners(ServiceProvider provider, string previousMigrationId, UpgradedSetting setting)
        {
            List<Seeded> seeded;
            using (var seedScope = provider.CreateScope())
            {
                var context = seedScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();

                await migrator.MigrateAsync(previousMigrationId);
                await HistoricalSchemaPatch.AddColumnsTheCurrentModelExpectsAsync(context);

                seeded = await SeedTheFourOwners(context, setting);

                await HistoricalSchemaPatch.RemoveColumnsTheCurrentModelExpectsAsync(context);
                await migrator.MigrateAsync();

                Assert.That(await context.Database.GetPendingMigrationsAsync(), Is.Empty, "The upgrade must reach the latest schema.");
            }

            using var readScope = provider.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var after = new Dictionary<Before, (int? Source, int? Value, string Fingerprint)>();
            foreach (var owner in seeded)
            {
                var (source, value) = await ReadTheSourceAndValue(readContext, setting, owner.Id);
                after[owner.Was] = (source, value, await TheFingerprintNow(readContext, setting, owner.Id));
            }

            var byWas = seeded.ToDictionary(owner => owner.Was);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(after[Before.NamesAFieldOfItsConnection].Source, Is.EqualTo(Own));
                Assert.That(after[Before.NamesAFieldOfItsConnection].Value, Is.Not.Null, "An owner keeps its own field.");
                Assert.That(after[Before.NamesAFieldOfItsConnection].Fingerprint, Is.EqualTo(byWas[Before.NamesAFieldOfItsConnection].FingerprintBefore));

                Assert.That(after[Before.NamesNothing].Source, Is.EqualTo(ConnectionDefault));
                Assert.That(after[Before.NamesNothing].Value, Is.Null);
                Assert.That(after[Before.NamesNothing].Fingerprint, Is.EqualTo(byWas[Before.NamesNothing].FingerprintBefore),
                    "With no default set yet, a follower reads nothing, exactly as before, so its next update does not download everything.");

                foreach (var repaired in new[] { Before.NamesADeletedField, Before.NamesAFieldOfAnotherConnection })
                {
                    Assert.That(after[repaired].Source, Is.EqualTo(ConnectionDefault), $"{repaired}: a field the connection does not have is treated as no field at all.");
                    Assert.That(after[repaired].Value, Is.Null, $"{repaired}");
                }
            }
        }

        private static async Task<List<Seeded>> SeedTheFourOwners(LighthouseAppContext context, UpgradedSetting setting)
        {
            var theirs = AConnection("Jira DC – Treasury", "Treasury Caused by");
            context.WorkTrackingSystemConnections.Add(theirs);
            await context.SaveChangesAsync();
            var anotherConnectionsField = theirs.AdditionalFieldDefinitions[0].Id;

            var seeded = new List<Seeded>();
            foreach (var before in Enum.GetValues<Before>())
            {
                var connection = AConnection($"Jira DC – Payments {before}", "Caused by");
                context.WorkTrackingSystemConnections.Add(connection);
                await context.SaveChangesAsync();

                int? value = before switch
                {
                    Before.NamesAFieldOfItsConnection => connection.AdditionalFieldDefinitions[0].Id,
                    Before.NamesADeletedField => AFieldThatWasDeleted,
                    Before.NamesAFieldOfAnotherConnection => anotherConnectionsField,
                    _ => null,
                };

                var owner = AnOwner(setting, $"{before}", connection, value);
                context.Add(owner);
                await context.SaveChangesAsync();

                seeded.Add(new Seeded(before, owner.Id, FetchFingerprint.For((IWorkItemQueryOwner)owner)));
            }

            return seeded;
        }

        private static WorkTrackingSystemConnection AConnection(string name, string fieldName)
        {
            var connection = new WorkTrackingSystemConnection { Name = name, WorkTrackingSystem = WorkTrackingSystems.Jira };
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = fieldName, Reference = "customfield_10500" });
            return connection;
        }

        private static WorkTrackingSystemOptionsOwner AnOwner(UpgradedSetting setting, string name, WorkTrackingSystemConnection connection, int? value)
        {
            if (setting is UpgradedSetting.TeamsParentOverrideField or UpgradedSetting.TeamsEstimation)
            {
                var team = new Team { Name = name, WorkTrackingSystemConnection = connection, DoneItemsCutoffDays = 180 };
                SetTheValue(team, setting, value);
                return team;
            }

            var portfolio = new Portfolio { Name = name, WorkTrackingSystemConnection = connection };
            SetTheValue(portfolio, setting, value);
            return portfolio;
        }

        private static void SetTheValue(WorkTrackingSystemOptionsOwner owner, UpgradedSetting setting, int? value)
        {
            switch (setting)
            {
                case UpgradedSetting.TeamsParentOverrideField:
                case UpgradedSetting.PortfoliosParentOverrideField:
                    owner.ParentOverrideAdditionalFieldDefinitionId = value;
                    break;
                case UpgradedSetting.PortfoliosDependencyField:
                    ((Portfolio)owner).DependencyOverrideAdditionalFieldDefinitionId = value;
                    break;
                case UpgradedSetting.PortfoliosSizeEstimateField:
                    ((Portfolio)owner).SizeEstimateAdditionalFieldDefinitionId = value;
                    break;
                case UpgradedSetting.PortfoliosFeatureOwnerField:
                    ((Portfolio)owner).FeatureOwnerAdditionalFieldDefinitionId = value;
                    break;
                case UpgradedSetting.TeamsEstimation:
                case UpgradedSetting.PortfoliosEstimation:
                    owner.EstimationAdditionalFieldDefinitionId = value;
                    owner.EstimationUnit = value is null ? null : "Points";
                    break;
            }
        }

        private static (string Table, string Source, string Value) TheColumns(UpgradedSetting setting) => setting switch
        {
            UpgradedSetting.TeamsParentOverrideField => ("Teams", "ParentOverrideSource", "ParentOverrideAdditionalFieldDefinitionId"),
            UpgradedSetting.PortfoliosParentOverrideField => ("Portfolios", "ParentOverrideSource", "ParentOverrideAdditionalFieldDefinitionId"),
            UpgradedSetting.PortfoliosDependencyField => ("Portfolios", "DependencyFieldSource", "DependencyOverrideAdditionalFieldDefinitionId"),
            UpgradedSetting.PortfoliosSizeEstimateField => ("Portfolios", "SizeEstimateFieldSource", "SizeEstimateAdditionalFieldDefinitionId"),
            UpgradedSetting.PortfoliosFeatureOwnerField => ("Portfolios", "FeatureOwnerFieldSource", "FeatureOwnerAdditionalFieldDefinitionId"),
            UpgradedSetting.TeamsEstimation => ("Teams", "EstimationSource", "EstimationAdditionalFieldDefinitionId"),
            UpgradedSetting.PortfoliosEstimation => ("Portfolios", "EstimationSource", "EstimationAdditionalFieldDefinitionId"),
            _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, null),
        };

        private static async Task<(int? Source, int? Value)> ReadTheSourceAndValue(LighthouseAppContext context, UpgradedSetting setting, int ownerId)
        {
            var (table, source, value) = TheColumns(setting);
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT \"{source}\", \"{value}\" FROM \"{table}\" WHERE \"Id\" = @id";
                var id = command.CreateParameter();
                id.ParameterName = "@id";
                id.Value = ownerId;
                command.Parameters.Add(id);

                await using var reader = await command.ExecuteReaderAsync();
                Assert.That(await reader.ReadAsync(), Is.True, $"Owner {ownerId} is gone from {table}.");

                return (reader.IsDBNull(0) ? null : Convert.ToInt32(reader.GetValue(0)), reader.IsDBNull(1) ? null : Convert.ToInt32(reader.GetValue(1)));
            }
            catch (DbException exception)
            {
                Assert.Fail($"{table} has no {source} column, so the upgrade that adds it has not been written yet. {exception.Message}");
                return (null, null);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private static async Task<string> TheFingerprintNow(LighthouseAppContext context, UpgradedSetting setting, int ownerId)
        {
            IWorkItemQueryOwner reloaded = TheColumns(setting).Table == "Teams"
                ? await context.Teams.AsNoTracking().Include(team => team.WorkTrackingSystemConnection).ThenInclude(connection => connection.AdditionalFieldDefinitions).SingleAsync(team => team.Id == ownerId)
                : await context.Portfolios.AsNoTracking().Include(portfolio => portfolio.WorkTrackingSystemConnection).ThenInclude(connection => connection.AdditionalFieldDefinitions).SingleAsync(portfolio => portfolio.Id == ownerId);

            return FetchFingerprint.For(reloaded);
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
