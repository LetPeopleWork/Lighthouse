using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
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
        private const string BlockedTagOne = "urgent";

        private const string PreviousMigrationIdSqlite = "20260711064246_AddIsPredefinedToAdditionalFieldDefinition";
        private const string PreviousMigrationIdPostgres = "20260711064256_AddIsPredefinedToAdditionalFieldDefinition";

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

        [Test]
        public async Task Migration_OnSqlite_BackfillsLegacyBlockedConfiguration_AndIsIdempotent()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-blocked-backfill-5074-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await AssertBackfillIsAppliedAndIdempotentAsync(provider, PreviousMigrationIdSqlite);
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
        public async Task Migration_OnPostgres_BackfillsLegacyBlockedConfiguration_AndIsIdempotent()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await AssertBackfillIsAppliedAndIdempotentAsync(provider, PreviousMigrationIdPostgres);
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

        private static async Task AssertBackfillIsAppliedAndIdempotentAsync(ServiceProvider provider, string previousMigrationId)
        {
            int teamId;
            int portfolioId;
            using (var seedScope = provider.CreateScope())
            {
                var context = seedScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();

                await migrator.MigrateAsync(previousMigrationId);

                var legacyTeam = NewTeam(blockedStates: [BlockedStateOne, BlockedStateTwo]);
                legacyTeam.BlockedTags = [BlockedTagOne];
                var legacyPortfolio = NewPortfolio(blockedStates: [BlockedStateOne], blockedTags: [BlockedTagOne]);

                context.Teams.Add(legacyTeam);
                context.Portfolios.Add(legacyPortfolio);
                await context.SaveChangesAsync();

                teamId = legacyTeam.Id;
                portfolioId = legacyPortfolio.Id;

                await migrator.MigrateAsync();

                var pendingAfterFirstRun = await context.Database.GetPendingMigrationsAsync();
                Assert.That(pendingAfterFirstRun, Is.Empty, "the backfill migration must reach the latest schema on first application");
            }

            string? teamRuleSetAfterFirstRun;
            string? portfolioRuleSetAfterFirstRun;
            using (var readScope = provider.CreateScope())
            {
                var context = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var team = await context.Teams.SingleAsync(t => t.Id == teamId);
                var portfolio = await context.Portfolios.SingleAsync(p => p.Id == portfolioId);

                teamRuleSetAfterFirstRun = team.BlockedRuleSetJson;
                portfolioRuleSetAfterFirstRun = portfolio.BlockedRuleSetJson;

                var blockedItemService = new BlockedItemService(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider());

                var teamCorpus = new[]
                {
                    NewWorkItem("TEAM-1", BlockedStateOne),
                    NewWorkItem("TEAM-2", BlockedStateTwo),
                    NewWorkItem("TEAM-3", "In Progress"),
                };

                var featureCorpus = new[]
                {
                    NewFeature("FEAT-1", BlockedStateOne),
                    NewFeature("FEAT-2", "In Progress"),
                };

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(teamRuleSetAfterFirstRun, Is.Not.Null,
                        "the migration must backfill BlockedRuleSetJson for a Team whose only configuration was legacy BlockedStates/BlockedTags");
                    Assert.That(portfolioRuleSetAfterFirstRun, Is.Not.Null,
                        "the migration must backfill BlockedRuleSetJson for a Portfolio whose only configuration was legacy BlockedStates/BlockedTags");

                    foreach (var item in teamCorpus)
                    {
                        Assert.That(blockedItemService.IsBlocked(item, team), Is.EqualTo(LegacyIsBlocked(team, item)),
                            $"backfilled Team rule set must reproduce legacy blocked behaviour for {item.ReferenceId}");
                    }

                    foreach (var feature in featureCorpus)
                    {
                        Assert.That(blockedItemService.IsBlocked(feature, portfolio), Is.EqualTo(LegacyIsBlocked(portfolio, feature)),
                            $"backfilled Portfolio rule set must reproduce legacy blocked behaviour for {feature.ReferenceId}");
                    }
                }
            }

            // Idempotency: force the backfill migration's Up() to run a second time (Down() is a schema
            // no-op — it only removes the migration history row) and confirm the already-backfilled rows
            // are untouched.
            using (var rerunScope = provider.CreateScope())
            {
                var context = rerunScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();

                await migrator.MigrateAsync(previousMigrationId);
                await migrator.MigrateAsync();
            }

            using var finalScope = provider.CreateScope();
            var finalContext = finalScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var finalTeam = await finalContext.Teams.SingleAsync(t => t.Id == teamId);
            var finalPortfolio = await finalContext.Portfolios.SingleAsync(p => p.Id == portfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(finalTeam.BlockedRuleSetJson, Is.EqualTo(teamRuleSetAfterFirstRun),
                    "re-running the backfill migration must not change an already-backfilled Team row");
                Assert.That(finalPortfolio.BlockedRuleSetJson, Is.EqualTo(portfolioRuleSetAfterFirstRun),
                    "re-running the backfill migration must not change an already-backfilled Portfolio row");
            }
        }

        private static bool LegacyIsBlocked(Portfolio portfolio, Feature item)
            => portfolio.BlockedStates.Any(state => string.Equals(state, item.State, StringComparison.OrdinalIgnoreCase));

        private static Portfolio NewPortfolio(List<string> blockedStates, List<string> blockedTags)
        {
            return new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                BlockedStates = blockedStates,
                BlockedTags = blockedTags,
            };
        }

        private static Feature NewFeature(string referenceId, string state)
        {
            return new Feature
            {
                ReferenceId = referenceId,
                Name = $"Feature {referenceId}",
                Type = "Epic",
                State = state,
                Tags = [],
            };
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
