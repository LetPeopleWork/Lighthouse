using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// The upgrade that turns the single Parent Override Field into a list of parent sources, on both real
    /// providers. Owners are written with today's model one release back, the real upgrade runs, and what each
    /// owner holds is read with raw SQL, so the test needs nothing from the model the upgrade adds.
    ///
    /// An owner that named a field now lists that field and then the tracker's own parent. An owner that
    /// chose "none" lists only the own parent. An owner that follows its connection's default holds nothing of
    /// its own, and a default that named a field lists it the same way. Going back down drops the new columns
    /// and leaves the old ones holding what an older build reads.
    /// </summary>
    [TestFixture]
    public class ParentSourcesUpgradeMigrationTests : IntegrationFixture
    {
        private const string PreviousMigrationIdSqlite = "20261023085259_AddForecastInputFingerprintAndRefreshLogSkipped";

        private const string PreviousMigrationIdPostgres = "20261023085311_AddForecastInputFingerprintAndRefreshLogSkipped";

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
        public async Task On_SQLite_the_upgrade_lists_each_override_with_the_own_parent_and_going_down_keeps_the_old_columns()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-parent-sources-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await TheUpgradeListsEveryOverrideAndGoingDownKeepsTheOldColumns(provider, PreviousMigrationIdSqlite);
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
        public async Task On_Postgres_the_upgrade_lists_each_override_with_the_own_parent_and_going_down_keeps_the_old_columns()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();

            await using var provider = BuildPostgresProvider(postgres.ConnectionStringToANewDatabase());
            await TheUpgradeListsEveryOverrideAndGoingDownKeepsTheOldColumns(provider, PreviousMigrationIdPostgres);
        }

        private sealed record Seeded(int TeamOwningAField, int TeamOwningNone, int TeamFollowing, int PortfolioOwningAField, int PortfolioFollowing, int Connection, int TheField);

        private static async Task TheUpgradeListsEveryOverrideAndGoingDownKeepsTheOldColumns(ServiceProvider provider, string previousMigrationId)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();

            await migrator.MigrateAsync(previousMigrationId);
            await HistoricalSchemaPatch.AddColumnsTheCurrentModelExpectsAsync(context);
            var seeded = await SeedOwnersOfEveryKind(context);
            await HistoricalSchemaPatch.RemoveColumnsTheCurrentModelExpectsAsync(context);

            await migrator.MigrateAsync();
            Assert.That(await context.Database.GetPendingMigrationsAsync(), Is.Empty, "The upgrade must reach the latest schema.");

            var field = seeded.TheField;
            var theOwnParentAfterTheField = $$"""[{"Kind":1,"AdditionalFieldDefinitionId":{{field}}},{"Kind":0,"AdditionalFieldDefinitionId":null}]""";
            var onlyTheOwnParent = """[{"Kind":0,"AdditionalFieldDefinitionId":null}]""";

            var stored = new Dictionary<string, string?>
            {
                ["Team owning a field"] = await ReadText(context, "Teams", "ParentSources", seeded.TeamOwningAField),
                ["Team owning none"] = await ReadText(context, "Teams", "ParentSources", seeded.TeamOwningNone),
                ["Team following"] = await ReadText(context, "Teams", "ParentSources", seeded.TeamFollowing),
                ["Portfolio owning a field"] = await ReadText(context, "Portfolios", "ParentSources", seeded.PortfolioOwningAField),
                ["Portfolio following"] = await ReadText(context, "Portfolios", "ParentSources", seeded.PortfolioFollowing),
                ["Connection default for Teams"] = await ReadText(context, "WorkTrackingSystemConnections", "ParentSourcesForTeams", seeded.Connection),
                ["Connection default for Portfolios"] = await ReadText(context, "WorkTrackingSystemConnections", "ParentSourcesForPortfolios", seeded.Connection),
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored["Team owning a field"], Is.EqualTo(theOwnParentAfterTheField),
                    "The field it named, then the tracker's own parent, so what it read before is still read first.");
                Assert.That(stored["Team owning none"], Is.EqualTo(onlyTheOwnParent));
                Assert.That(stored["Team following"], Is.Null, "A follower holds no list of its own; it keeps following.");
                Assert.That(stored["Portfolio owning a field"], Is.EqualTo(theOwnParentAfterTheField));
                Assert.That(stored["Portfolio following"], Is.Null);
                Assert.That(stored["Connection default for Teams"], Is.EqualTo(theOwnParentAfterTheField));
                Assert.That(stored["Connection default for Portfolios"], Is.Null, "A default that named nothing stays unset.");
            }

            foreach (var (name, text) in stored.Where(entry => entry.Value is not null))
            {
                Assert.That(ParentSources.FromStoredText(text!).ToStoredText(), Is.EqualTo(text),
                    $"{name}: the text the upgrade wrote in SQL must be the text the serialiser writes, or the two drift apart unseen.");
            }

            await migrator.MigrateAsync(previousMigrationId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await ReadNumber(context, "Teams", "ParentOverrideAdditionalFieldDefinitionId", seeded.TeamOwningAField), Is.EqualTo(field),
                    "Going back leaves the old column holding what an older build reads.");
                Assert.That(await ReadNumber(context, "Portfolios", "ParentOverrideAdditionalFieldDefinitionId", seeded.PortfolioOwningAField), Is.EqualTo(field));
                Assert.That(await ReadNumber(context, "WorkTrackingSystemConnections", "ParentOverrideForTeams", seeded.Connection), Is.EqualTo(field));
                Assert.That(async () => await ReadText(context, "Teams", "ParentSources", seeded.TeamOwningAField), Throws.InstanceOf<DbException>(),
                    "Going back drops the new column and nothing else.");
            }
        }

        private static async Task<Seeded> SeedOwnersOfEveryKind(LighthouseAppContext context)
        {
            var connection = new WorkTrackingSystemConnection { Name = "Jira DC - Payments", WorkTrackingSystem = WorkTrackingSystems.Jira };
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = "Caused by", Reference = "customfield_10500" });
            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();

            var field = connection.AdditionalFieldDefinitions[0].Id;
            connection.ParentOverrideForTeams = field;

            var teamOwningAField = new Team { Name = "Owns a field", WorkTrackingSystemConnection = connection, DoneItemsCutoffDays = 180 };
            teamOwningAField.OwnParentOverride(field);
            var teamOwningNone = new Team { Name = "Owns none", WorkTrackingSystemConnection = connection, DoneItemsCutoffDays = 180 };
            teamOwningNone.OwnParentOverride(null);
            var teamFollowing = new Team { Name = "Follows", WorkTrackingSystemConnection = connection, DoneItemsCutoffDays = 180 };
            teamFollowing.FollowParentOverrideDefault();
            var portfolioOwningAField = new Portfolio { Name = "Owns a field", WorkTrackingSystemConnection = connection };
            portfolioOwningAField.OwnParentOverride(field);
            var portfolioFollowing = new Portfolio { Name = "Follows", WorkTrackingSystemConnection = connection };
            portfolioFollowing.FollowParentOverrideDefault();

            context.AddRange(teamOwningAField, teamOwningNone, teamFollowing, portfolioOwningAField, portfolioFollowing);
            await context.SaveChangesAsync();

            return new Seeded(teamOwningAField.Id, teamOwningNone.Id, teamFollowing.Id, portfolioOwningAField.Id, portfolioFollowing.Id, connection.Id, field);
        }

        private static async Task<string?> ReadText(LighthouseAppContext context, string table, string column, int id)
            => await ReadOne(context, table, column, id) is { } value and not DBNull ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) : null;

        private static async Task<int?> ReadNumber(LighthouseAppContext context, string table, string column, int id)
            => await ReadOne(context, table, column, id) is { } value and not DBNull ? Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) : null;

        private static async Task<object?> ReadOne(LighthouseAppContext context, string table, string column, int id)
        {
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT \"{column}\" FROM \"{table}\" WHERE \"Id\" = @id";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@id";
                parameter.Value = id;
                command.Parameters.Add(parameter);

                return await command.ExecuteScalarAsync();
            }
            finally
            {
                await connection.CloseAsync();
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
