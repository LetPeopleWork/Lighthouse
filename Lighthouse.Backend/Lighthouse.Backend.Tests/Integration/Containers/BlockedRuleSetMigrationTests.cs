using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// ADR-067 loss-free-migration probe (slice-01 hard AC). Proves against REAL relational providers
    /// (SQLite + Postgres — InMemory misses migrations) that (a) the additive <c>BlockedRuleSetJson</c>
    /// column applied by the generated migration persists across a reload, and (b) the application-layer
    /// auto-migration backfill is loss-free: for a fixture corpus, an item's blocked status computed the
    /// legacy way (BlockedStates lists) equals the status computed via the synthesized rule set — no item
    /// flips. The migration itself is expand-only (adds a nullable column, no data step); the backfill is
    /// the read-side synthesis in <see cref="BlockedItemService"/>.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedRuleSetMigrationTests
    {
        private const string BlockedStateOne = "Blocked";
        private const string BlockedStateTwo = "On Hold";

        [Test]
        public async Task Migration_OnSqlite_PersistsColumn_AndBackfillIsLossFree()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-blocked-5074-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);
                await AssertColumnPersistsAsync(provider);
                await AssertBackfillIsLossFreeAsync(provider);
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
        public async Task Migration_OnPostgres_PersistsColumn_AndBackfillIsLossFree()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertColumnPersistsAsync(provider);
            await AssertBackfillIsLossFreeAsync(provider);
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var pending = await context.Database.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty, "the generated AddBlockedRuleSetJson migration must apply cleanly on a real provider");
        }

        private static async Task AssertColumnPersistsAsync(ServiceProvider provider)
        {
            const string ruleSetJson = "{\"version\":1,\"mode\":\"or\",\"conditions\":[{\"fieldKey\":\"workitem.state\",\"operator\":\"equals\",\"value\":\"Blocked\"}]}";

            int teamWithRuleSetId;
            int teamWithoutRuleSetId;
            using (var writeScope = provider.CreateScope())
            {
                var context = writeScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                var teamWithRuleSet = NewTeam(blockedStates: []);
                teamWithRuleSet.BlockedRuleSetJson = ruleSetJson;
                var teamWithoutRuleSet = NewTeam(blockedStates: []);

                context.Teams.Add(teamWithRuleSet);
                context.Teams.Add(teamWithoutRuleSet);
                await context.SaveChangesAsync();

                teamWithRuleSetId = teamWithRuleSet.Id;
                teamWithoutRuleSetId = teamWithoutRuleSet.Id;
            }

            using var readScope = provider.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var reloadedWithRuleSet = await readContext.Teams.SingleAsync(t => t.Id == teamWithRuleSetId);
            var reloadedWithoutRuleSet = await readContext.Teams.SingleAsync(t => t.Id == teamWithoutRuleSetId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloadedWithRuleSet.BlockedRuleSetJson, Is.EqualTo(ruleSetJson),
                    "the migrated BlockedRuleSetJson column must round-trip a stored rule set");
                Assert.That(reloadedWithoutRuleSet.BlockedRuleSetJson, Is.Null,
                    "the migrated column is nullable — an owner that never configured a rule set reads back null");
            }
        }

        private static async Task AssertBackfillIsLossFreeAsync(ServiceProvider provider)
        {
            int teamId;
            using (var writeScope = provider.CreateScope())
            {
                var context = writeScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var legacyTeam = NewTeam(blockedStates: [BlockedStateOne, BlockedStateTwo]);
                context.Teams.Add(legacyTeam);
                await context.SaveChangesAsync();
                teamId = legacyTeam.Id;
            }

            using var readScope = provider.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var team = await readContext.Teams.SingleAsync(t => t.Id == teamId);

            var blockedItemService = new BlockedItemService(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider());

            var corpus = new[]
            {
                NewWorkItem("PHX-1", BlockedStateOne),
                NewWorkItem("PHX-2", BlockedStateTwo),
                NewWorkItem("PHX-3", "blocked"),
                NewWorkItem("PHX-4", "In Progress"),
                NewWorkItem("PHX-5", "Done"),
            };

            var flippedItems = corpus
                .Where(item => LegacyIsBlocked(team, item) != blockedItemService.IsBlocked(item, team))
                .Select(item => item.ReferenceId)
                .ToList();

            var newlyBlocked = corpus.Count(item => blockedItemService.IsBlocked(item, team));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(flippedItems, Is.Empty,
                    "the auto-migration backfill is loss-free: no fixture item changes blocked status between the legacy " +
                    "BlockedStates definition and the synthesized rule set");
                Assert.That(newlyBlocked, Is.EqualTo(3),
                    "the parity corpus is non-vacuous — the two blocked states plus a case-insensitive match resolve blocked");
            }
        }

        private static bool LegacyIsBlocked(Team team, WorkItem item)
            => team.BlockedStates.Any(state => string.Equals(state, item.State, StringComparison.OrdinalIgnoreCase));

        private static Team NewTeam(List<string> blockedStates)
        {
            return new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DoneItemsCutoffDays = 0,
                BlockedStates = blockedStates,
                BlockedTags = [],
            };
        }

        private static WorkItem NewWorkItem(string referenceId, string state)
        {
            return new WorkItem
            {
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = state,
                Tags = [],
            };
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
