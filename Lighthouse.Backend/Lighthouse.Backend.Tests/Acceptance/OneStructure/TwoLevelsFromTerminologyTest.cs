using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetup;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
using Lighthouse.Backend.Platform.Encryption;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Platform.Seeding;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.Acceptance.OneStructure;
using Lighthouse.Backend.Tests.Integration.Containers;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// The upgrade runs here on Postgres as well, in a container. The test tier guard knows a container fixture by this
// namespace, so the fast loop leaves it out; the folder keeps it beside the other scenarios of the restructuring.
namespace Lighthouse.Backend.Tests.Integration.Containers.Acceptance.OneStructure
{
    /// <summary>
    /// After the upgrade an installation has two levels: the one where the work is done, named after its Teams and
    /// Work Items, and the one above it, named after its Portfolios and Features, both in the organisation's own
    /// words. Each connection lists, per level, the item types its units on that level read, once each whatever the
    /// case. The upgrade runs on both real databases people install on, as the start of the application runs it.
    /// </summary>
    [TestFixture]
    public class TwoLevelsFromTerminologyTest : IntegrationFixture
    {
        public enum Database
        {
            Sqlite,
            Postgres,
        }

        private static readonly DateOnly Today = new(2026, 11, 28);

        private static readonly string[] TheEightWords = ["team", "teams", "portfolio", "portfolios", "workItem", "workItems", "feature", "features"];

        // Started by the first Postgres case, so the SQLite cases alone need no Docker.
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
        public async Task The_upgrade_gives_two_levels_named_in_the_organisations_own_words([Values] Database database)
        {
            await using var installation = await GivenAnInstallationShapedLikeTheOnesPeopleUpgradeFrom(database);

            await WhenTheApplicationStarts(installation);

            await ThenTheLevelsAre(installation,
                new LevelView("Team", 1, "Squad", "Squads", "Work Item", "Work Items"),
                new LevelView("Portfolio", 2, "Initiative", "Initiatives", "Feature", "Features"));
        }

        [Test]
        public async Task A_fresh_install_gets_two_levels_with_the_seeded_words_and_no_connection_mappings([Values] Database database)
        {
            await using var installation = await GivenAFreshInstall(database);

            await WhenTheApplicationStarts(installation);

            await ThenTheLevelsAre(installation,
                new LevelView("Team", 1, "Team", "Teams", "Work Item", "Work Items"),
                new LevelView("Portfolio", 2, "Portfolio", "Portfolios", "Feature", "Features"));
            await ThenNoConnectionHasAMapping(installation);
        }

        [Test]
        public async Task Each_connection_lists_on_each_level_the_types_its_units_there_read_once_whatever_the_case([Values] Database database)
        {
            await using var installation = await GivenAnInstallationShapedLikeTheOnesPeopleUpgradeFrom(database);

            await WhenTheApplicationStarts(installation);

            var mappings = await TheConnectionMappings(installation);
            ThenTheConnectionLists(installation, mappings, "letpeoplework.atlassian.net", team: ["User Story", "Bug", "Epic"], portfolio: ["Epic", "Change"]);
            ThenTheConnectionLists(installation, mappings, "dev.azure.com/letpeoplework", team: ["User Story", "Bug"], portfolio: ["Epic"]);
            ThenTheConnectionLists(installation, mappings, "Weekly upload", team: ["User Story", "Bug"], portfolio: ["Epic"]);
            ThenTheConnectionLists(installation, mappings, "linear.app/letpeoplework", team: [], portfolio: []);
            ThenTheConnectionListsOnTheTeamLevel(installation, mappings, "letpeoplework.service-now.com", ["Incident", "Problem"]);
        }

        [Test]
        public async Task A_type_a_Team_and_a_Portfolio_of_one_connection_both_read_sits_on_both_levels([Values] Database database)
        {
            await using var installation = await GivenAnInstallationShapedLikeTheOnesPeopleUpgradeFrom(database);

            await WhenTheApplicationStarts(installation);

            var mappings = await TheConnectionMappings(installation);
            ThenTheTypesOnBothLevelsAre(installation, mappings, "letpeoplework.atlassian.net", ["Epic"]);
            ThenTheTypesOnBothLevelsAre(installation, mappings, "dev.azure.com/letpeoplework", []);
        }

        [Test]
        public async Task The_eight_words_of_Terminology_read_the_same_after_the_upgrade([Values] Database database)
        {
            await using var installation = await GivenAnInstallationShapedLikeTheOnesPeopleUpgradeFrom(database);
            var before = TheEightWordsAsTerminologyAnswers(installation);

            await WhenTheApplicationStarts(installation);

            ThenTheEightWordsAre(installation, before);
        }

        [Test]
        public async Task Renaming_through_Terminology_after_the_upgrade_lands_on_the_level([Values] Database database)
        {
            await using var installation = await GivenAnUpgradedInstallation(database);
            var before = TheEightWordsAsTerminologyAnswers(installation);

            await WhenTerminologyIsUpdated(installation, ("team", "Crew"), ("teams", "Crews"), ("feature", "Outcome"), ("features", "Outcomes"));

            await ThenTheLevelsAre(installation,
                new LevelView("Team", 1, "Crew", "Crews", "Work Item", "Work Items"),
                new LevelView("Portfolio", 2, "Initiative", "Initiatives", "Outcome", "Outcomes"));
            ThenTheEightWordsAre(installation, WithValues(before, ("team", "Crew"), ("teams", "Crews"), ("feature", "Outcome"), ("features", "Outcomes")));
        }

        [Test]
        public async Task A_second_start_leaves_the_levels_and_the_mappings_as_the_first_start_left_them([Values] Database database)
        {
            await using var installation = await GivenAnUpgradedInstallation(database);
            var afterTheFirstStart = await WhatTheStructureReads(installation);

            await WhenTheApplicationStarts(installation);

            await ThenTheStructureStillReads(installation, afterTheFirstStart);
        }

        private async Task<Installation> GivenAnInstallationShapedLikeTheOnesPeopleUpgradeFrom(Database database)
        {
            var installation = await AnEmptyDatabase(database);
            using var scope = installation.Services.CreateScope();
            UpgradeFixture.WriteTheInstallation(scope.ServiceProvider, Today);
            return installation;
        }

        private async Task<Installation> GivenAFreshInstall(Database database)
        {
            var installation = await AnEmptyDatabase(database);
            using var scope = installation.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<TerminologySeeder>().Seed();
            return installation;
        }

        private async Task<Installation> GivenAnUpgradedInstallation(Database database)
        {
            var installation = await GivenAnInstallationShapedLikeTheOnesPeopleUpgradeFrom(database);
            await WhenTheApplicationStarts(installation);
            return installation;
        }

        private static async Task WhenTheApplicationStarts(Installation installation)
        {
            using var scope = installation.Services.CreateScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
            }
        }

        private static async Task WhenTerminologyIsUpdated(Installation installation, params (string Key, string Value)[] words)
        {
            using var scope = installation.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ITerminologyService>()
                .UpdateTerminology([.. words.Select(word => new TerminologyEntry { Key = word.Key, Value = word.Value })]);
        }

        private static async Task ThenTheLevelsAre(Installation installation, params LevelView[] expected)
        {
            var read = await TheStructure(installation);

            Assert.That(read, Is.InstanceOf<StructureRead.Migrated>(),
                $"On {installation.Database} the structure reads {read}, so the start built no levels. An installation has two levels after the upgrade, named from its Terminology; the start-up step must build them.");

            var levels = ((StructureRead.Migrated)read).Structure.Levels;
            Assert.That(levels, Is.EqualTo(expected),
                $"On {installation.Database} the levels are [{string.Join("; ", levels)}], expected [{string.Join("; ", expected)}]. Each level takes its unit and item names, singular and plural, from the organisation's Terminology, level 1 for Teams and Work Items and level 2 for Portfolios and Features; renaming a word in Terminology renames it on its level.");
        }

        private static async Task ThenNoConnectionHasAMapping(Installation installation)
        {
            var mappings = await TheConnectionMappings(installation);

            Assert.That(mappings, Is.Empty,
                $"On {installation.Database} a fresh install with no connection lists mappings for connections {string.Join(", ", mappings.Select(mapping => mapping.ConnectionId))}. A mapping belongs to a connection; with none there is nothing to map.");
        }

        private static void ThenTheConnectionLists(Installation installation, IReadOnlyList<ConnectionMappingView> mappings, string connection, string[] team, string[] portfolio)
        {
            ThenTheConnectionListsOnTheTeamLevel(installation, mappings, connection, team);
            ThenTheLevelLists(installation, MappingOf(installation, mappings, connection), connection, "Portfolio", portfolio);
        }

        private static void ThenTheConnectionListsOnTheTeamLevel(Installation installation, IReadOnlyList<ConnectionMappingView> mappings, string connection, string[] team)
            => ThenTheLevelLists(installation, MappingOf(installation, mappings, connection), connection, "Team", team);

        private static void ThenTheLevelLists(Installation installation, ConnectionMappingView mapping, string connection, string level, string[] expected)
        {
            var listed = mapping.TypesByLevel.TryGetValue(level, out var types) ? types : [];

            Assert.That(listed, Is.EquivalentTo(expected).Using((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase),
                $"On {installation.Database} the {level} level of {connection} lists [{string.Join(", ", listed)}], expected [{string.Join(", ", expected)}] once each whatever the case. A level of a connection lists every type its units on that level read, a type both levels read sits on both, and a level with no units gets today's create defaults (User Story and Bug for Teams, Epic for Portfolios, nothing on a tracker without types).");
        }

        private static void ThenTheTypesOnBothLevelsAre(Installation installation, IReadOnlyList<ConnectionMappingView> mappings, string connection, string[] expected)
        {
            var onBoth = MappingOf(installation, mappings, connection).TypesOnBothLevels;

            Assert.That(onBoth, Is.EquivalentTo(expected).Using((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase),
                $"On {installation.Database} {connection} reports [{string.Join(", ", onBoth)}] on both levels, expected [{string.Join(", ", expected)}]. A type a Team and a Portfolio of one connection both read stays on both levels and blocks nothing; neither level takes it from the other.");
        }

        private static void ThenTheEightWordsAre(Installation installation, IReadOnlyDictionary<string, TerminologyAnswer> expected)
        {
            var answered = TheEightWordsAsTerminologyAnswers(installation);

            Assert.That(answered, Is.EquivalentTo(expected),
                $"On {installation.Database} Terminology answers [{string.Join(", ", answered.Select(word => $"{word.Key}={word.Value}"))}], expected [{string.Join(", ", expected.Select(word => $"{word.Key}={word.Value}"))}]. Every screen and API that shows these eight words must get the same answer as before, value and default alike: a word nobody changed still answers an empty value with its default, and a renamed word answers its new value.");
        }

        private static async Task ThenTheStructureStillReads(Installation installation, StructureSnapshot afterTheFirstStart)
        {
            var now = await WhatTheStructureReads(installation);

            using var _ = Assert.EnterMultipleScope();
            Assert.That(afterTheFirstStart.Levels, Is.Not.Empty,
                $"On {installation.Database} the first start built no levels, so a second start has nothing to keep. The upgrade must build the two levels on its first start.");
            Assert.That(now, Is.EqualTo(afterTheFirstStart),
                $"On {installation.Database} a second start changed the structure from {afterTheFirstStart} to {now}. The upgrade runs once; a later start must leave the levels and every connection's mapping as they are.");
        }

        private static Dictionary<string, TerminologyAnswer> WithValues(IReadOnlyDictionary<string, TerminologyAnswer> answers, params (string Key, string Value)[] renamed)
        {
            var expected = answers.ToDictionary(answer => answer.Key, answer => answer.Value);
            foreach (var (key, value) in renamed)
            {
                expected[key] = expected[key] with { Value = value };
            }

            return expected;
        }

        private static Dictionary<string, TerminologyAnswer> TheEightWordsAsTerminologyAnswers(Installation installation)
        {
            using var scope = installation.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ITerminologyService>().GetAll()
                .Where(entry => TheEightWords.Contains(entry.Key))
                .ToDictionary(entry => entry.Key, entry => new TerminologyAnswer(entry.Value, entry.DefaultValue));
        }

        private static async Task<StructureRead> TheStructure(Installation installation)
        {
            using var scope = installation.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IOrganisationStructureQuery>().Structure(CancellationToken.None);
        }

        private static async Task<IReadOnlyList<ConnectionMappingView>> TheConnectionMappings(Installation installation)
        {
            using var scope = installation.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IOrganisationStructureQuery>().Mappings(CancellationToken.None);
        }

        private static async Task<StructureSnapshot> WhatTheStructureReads(Installation installation)
        {
            var levels = await TheStructure(installation) is StructureRead.Migrated migrated ? migrated.Structure.Levels.ToList() : [];
            var mappings = (await TheConnectionMappings(installation))
                .OrderBy(mapping => mapping.ConnectionId)
                .Select(mapping => $"{mapping.ConnectionId}: " + string.Join(" | ", mapping.TypesByLevel.OrderBy(level => level.Key, StringComparer.Ordinal).Select(level => $"{level.Key}=[{string.Join(", ", level.Value)}]")))
                .ToList();
            return new StructureSnapshot(string.Join("; ", levels), string.Join("; ", mappings));
        }

        private static ConnectionMappingView MappingOf(Installation installation, IReadOnlyList<ConnectionMappingView> mappings, string connection)
        {
            using var scope = installation.Services.CreateScope();
            var connectionId = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().WorkTrackingSystemConnections
                .Single(stored => stored.Name == connection).Id;
            var mapping = mappings.SingleOrDefault(candidate => candidate.ConnectionId == connectionId);

            Assert.That(mapping, Is.Not.Null,
                $"On {installation.Database} {connection} has no mapping after the upgrade; mappings exist for connections [{string.Join(", ", mappings.Select(candidate => candidate.ConnectionId))}]. Every connection with units gets its mapping at the upgrade.");
            return mapping!;
        }

        private async Task<Installation> AnEmptyDatabase(Database database)
        {
            if (database == Database.Sqlite)
            {
                var file = Path.Combine(Path.GetTempPath(), $"lighthouse-levels-{Guid.NewGuid():N}.db");
                return await Installation.Migrated(database,
                    options => options.UseSqlite($"Data Source={file};Pooling=False", sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")),
                    () => File.Delete(file));
            }

            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();
            return await Installation.Migrated(database,
                options => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")),
                () => { });
        }

        private sealed record StructureSnapshot(string Levels, string Mappings);

        /// <summary>What Terminology answers for one word: the organisation's value, empty when never changed, and its default.</summary>
        private sealed record TerminologyAnswer(string Value, string DefaultValue);

        /// <summary>
        /// One database at the latest schema, with what the start reads to build the levels: Terminology, the
        /// connections and units, and Organisation set-up's structure registrations as the application makes them.
        /// </summary>
        private sealed class Installation : IAsyncDisposable
        {
            private readonly Action removeTheDatabase;

            private Installation(Database database, ServiceProvider services, Action removeTheDatabase)
            {
                Database = database;
                Services = services;
                this.removeTheDatabase = removeTheDatabase;
            }

            public Database Database { get; }

            public ServiceProvider Services { get; }

            public static async Task<Installation> Migrated(Database database, Action<DbContextOptionsBuilder> useDatabase, Action removeTheDatabase)
            {
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton<ICryptoService, FakeCryptoService>();
                services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(TestToday.FrozenNow));
                services.AddDbContext<LighthouseAppContext>(useDatabase);
                services.AddScoped<IRepository<TerminologyEntry>, TerminologyRepository>();
                services.AddScoped<TerminologySeeder>();
                services.AddScoped<ITerminologyService, TerminologyService>();
                services.AddOrganisationStructure();

                var provider = services.BuildServiceProvider();
                using (var scope = provider.CreateScope())
                {
                    await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.MigrateAsync();
                }

                return new Installation(database, provider, removeTheDatabase);
            }

            public async ValueTask DisposeAsync()
            {
                await Services.DisposeAsync();
                removeTheDatabase();
            }
        }
    }
}
