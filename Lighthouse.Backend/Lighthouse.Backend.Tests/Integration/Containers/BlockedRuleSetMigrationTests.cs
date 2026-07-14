using System.Text.Json;
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
    /// ADR-067 loss-free-migration probe (slice-01 hard AC) + Phase-B drop-column probe. Proves against
    /// REAL relational providers (SQLite + Postgres — InMemory misses migrations) that (a) the additive
    /// <c>BlockedRuleSetJson</c> column applied by the generated migration persists across a reload, and
    /// (b) the migration-time backfill (raw SQL inside the generated migration, not application code) is
    /// loss-free: a legacy row whose only configuration was BlockedStates/BlockedTags ends up with a
    /// BlockedRuleSetJson that reproduces the same blocked decisions. Both migrations are expand-then-drop:
    /// the backfill populates BlockedRuleSetJson first (earlier timestamp), a later migration then drops
    /// the now-unused legacy columns — so seeding "legacy" rows here goes through raw SQL against the
    /// column names directly (the Team/Portfolio model no longer declares BlockedStates/BlockedTags, by
    /// design, once the columns are dropped).
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
        public async Task Migration_OnSqlite_PersistsBlockedRuleSetJsonColumn()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-blocked-5074-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);
                await AssertColumnPersistsAsync(provider);
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
        public async Task Migration_OnPostgres_PersistsBlockedRuleSetJsonColumn()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertColumnPersistsAsync(provider);
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
            Assert.That(pending, Is.Empty, "every generated migration must apply cleanly on a real provider");
        }

        private static async Task AssertColumnPersistsAsync(ServiceProvider provider)
        {
            const string ruleSetJson = "{\"version\":1,\"mode\":\"or\",\"conditions\":[{\"fieldKey\":\"workitem.state\",\"operator\":\"equals\",\"value\":\"Blocked\"}]}";

            int teamWithRuleSetId;
            int teamWithoutRuleSetId;
            using (var writeScope = provider.CreateScope())
            {
                var context = writeScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                var teamWithRuleSet = NewTeam();
                teamWithRuleSet.BlockedRuleSetJson = ruleSetJson;
                var teamWithoutRuleSet = NewTeam();

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

        private static async Task AssertBackfillIsAppliedAndIdempotentAsync(ServiceProvider provider, string previousMigrationId)
        {
            int teamId;
            int portfolioId;
            using (var seedScope = provider.CreateScope())
            {
                var context = seedScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();

                await migrator.MigrateAsync(previousMigrationId);

                // At previousMigrationId the legacy columns are NOT NULL, but years of unrelated later
                // table rebuilds (SQLite recreates the whole table on ANY column change) or the original
                // Postgres AddColumn (which specified no default) mean the columns may no longer have a
                // usable DEFAULT — and the CURRENT Team/Portfolio model doesn't declare these properties,
                // so the plain EF Add()/SaveChanges() below can't supply a value for them either. Restore
                // a working default first so that insert can proceed; the real values are set afterwards.
                await EnsureLegacyColumnsHaveWorkingDefaultAsync(context, "Teams");
                await EnsureLegacyColumnsHaveWorkingDefaultAsync(context, "Portfolios");

                var legacyTeam = NewTeam();
                var legacyPortfolio = NewPortfolio();

                context.Teams.Add(legacyTeam);
                context.Portfolios.Add(legacyPortfolio);
                await context.SaveChangesAsync();

                teamId = legacyTeam.Id;
                portfolioId = legacyPortfolio.Id;

                // Emulate an installation that predates this feature: at previousMigrationId the legacy
                // BlockedStates/BlockedTags columns still exist on disk, but the current Team/Portfolio
                // model no longer declares them (they are dropped by a later migration), so seed the raw
                // columns directly instead of going through the EF model.
                await SeedLegacyBlockedColumnsAsync(context, "Teams", teamId, [BlockedStateOne, BlockedStateTwo], [BlockedTagOne]);
                await SeedLegacyBlockedColumnsAsync(context, "Portfolios", portfolioId, [BlockedStateOne], [BlockedTagOne]);

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

                // Expected outcomes mirror the legacy seed above (BlockedStateOne/Two + BlockedTagOne) —
                // literal, not derived from a model getter, because BlockedStates/BlockedTags no longer
                // exist as C# properties once the columns are dropped.
                var teamCorpus = new (WorkItem Item, bool ExpectedBlocked)[]
                {
                    (NewWorkItem("TEAM-1", BlockedStateOne), true),
                    (NewWorkItem("TEAM-2", BlockedStateTwo), true),
                    (NewWorkItem("TEAM-3", "In Progress"), false),
                };

                var featureCorpus = new (Feature Item, bool ExpectedBlocked)[]
                {
                    (NewFeature("FEAT-1", BlockedStateOne), true),
                    (NewFeature("FEAT-2", "In Progress"), false),
                };

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(teamRuleSetAfterFirstRun, Is.Not.Null,
                        "the migration must backfill BlockedRuleSetJson for a Team whose only configuration was legacy BlockedStates/BlockedTags");
                    Assert.That(portfolioRuleSetAfterFirstRun, Is.Not.Null,
                        "the migration must backfill BlockedRuleSetJson for a Portfolio whose only configuration was legacy BlockedStates/BlockedTags");

                    foreach (var (item, expectedBlocked) in teamCorpus)
                    {
                        Assert.That(blockedItemService.IsBlocked(item, team), Is.EqualTo(expectedBlocked),
                            $"backfilled Team rule set must reproduce legacy blocked behaviour for {item.ReferenceId}");
                    }

                    foreach (var (feature, expectedBlocked) in featureCorpus)
                    {
                        Assert.That(blockedItemService.IsBlocked(feature, portfolio), Is.EqualTo(expectedBlocked),
                            $"backfilled Portfolio rule set must reproduce legacy blocked behaviour for {feature.ReferenceId}");
                    }
                }
            }

            // Idempotency: force the backfill migration's Up() to run a second time (Down() of the
            // later drop-column migration re-adds the now-empty legacy columns; Down() of the backfill
            // migration itself is a data no-op — see its comment) and confirm the already-backfilled
            // rows are untouched.
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

        // EF1002/EF1003 flag any non-constant SQL text passed to ExecuteSqlRaw as a possible injection
        // risk. tableName here is always one of two fixed literals ("Teams"/"Portfolios") supplied by this
        // test's own call sites — never external input — so building the DDL by concatenation is safe;
        // ExecuteSqlRaw's `{n}` parameters are still used for every actual data value.
#pragma warning disable EF1002, EF1003
        private static async Task EnsureLegacyColumnsHaveWorkingDefaultAsync(LighthouseAppContext context, string tableName)
        {
            var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
            if (isSqlite)
            {
                // SQLite has no ALTER COLUMN; recreate the column so it carries a DEFAULT again (it is
                // about to be overwritten by SeedLegacyBlockedColumnsAsync anyway).
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE " + tableName + " DROP COLUMN BlockedStates");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE " + tableName + " DROP COLUMN BlockedTags");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE " + tableName + " ADD COLUMN BlockedStates TEXT NOT NULL DEFAULT '[]'");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE " + tableName + " ADD COLUMN BlockedTags TEXT NOT NULL DEFAULT '[]'");
            }
            else
            {
                // ExecuteSqlRaw uses composite formatting for its {n} parameter placeholders, so a literal
                // '{}' (empty Postgres array) must be escaped as '{{}}'.
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"" + tableName + "\" ALTER COLUMN \"BlockedStates\" SET DEFAULT '{{}}'");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"" + tableName + "\" ALTER COLUMN \"BlockedTags\" SET DEFAULT '{{}}'");
            }
        }
#pragma warning restore EF1002, EF1003

        private static async Task SeedLegacyBlockedColumnsAsync(LighthouseAppContext context, string tableName, int id, string[] states, string[] tags)
        {
            var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

            // Built via concatenation (not a $"..." interpolated string) so the {0}/{1}/{2} placeholders
            // stay literal for ExecuteSqlRaw's own parameterization (EF1002) — tableName is one of two
            // fixed literals ("Teams"/"Portfolios") from this test's own call sites, never external input.
            if (isSqlite)
            {
                var sql = "UPDATE " + tableName + " SET BlockedStates = {0}, BlockedTags = {1} WHERE Id = {2}";
                await context.Database.ExecuteSqlRawAsync(sql, JsonSerializer.Serialize(states), JsonSerializer.Serialize(tags), id);
            }
            else
            {
                var sql = "UPDATE \"" + tableName + "\" SET \"BlockedStates\" = {0}, \"BlockedTags\" = {1} WHERE \"Id\" = {2}";
                await context.Database.ExecuteSqlRawAsync(sql, states, tags, id);
            }
        }

        private static Portfolio NewPortfolio()
        {
            return new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
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

        private static Team NewTeam()
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
