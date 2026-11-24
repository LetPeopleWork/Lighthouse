using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Platform.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// The clean-up that runs after every Portfolio refresh and at every start keeps a Feature only while it is in a
    /// Portfolio, or while it is a parent item that a Feature in a Portfolio still names as its parent. Everything
    /// else is deleted with its remaining work and its forecasts, so it leaves the Team's Features tab and the
    /// forecasts. A kept parent item that is in no Portfolio itself is kept only as its children's parent name: its
    /// remaining work is deleted, so it is on no Team's Features tab and in no forecast. A store that already holds
    /// such leftovers is cleaned by the same run, with no migration.
    /// </summary>
    [TestFixture]
    public class OrphanedFeatureCleanupServiceTests
    {
        private FeatureStore store = null!;
        private int deletedByTheLastRun;
        private readonly List<(int Deleted, FeatureHistory? Watched)> runs = [];

        [SetUp]
        public void SetUp()
        {
            store = new FeatureStore();
        }

        [TearDown]
        public void TearDown()
        {
            store.Dispose();
        }

        [Test]
        public async Task AFormerFeatureInNoPortfolioThatIsNoOnesParent_IsDeletedWithItsRemainingWorkAndForecast_AndLeavesTheTeamsFeaturesTab()
        {
            var team = await store.ATeam("Team");
            var portfolio = await store.APortfolio("Portfolio");
            await store.AFeature(team, "CURRENT-1", remainingWork: 3, inPortfolios: [portfolio]);
            await store.AFormerParentItem(team, "FORMER-1", remainingWork: 5, withAForecast: true);

            await TheCleanupRuns();

            await TheStoredFeaturesAre("CURRENT-1");
            TheTeamsFeaturesTabShows(team, "CURRENT-1");
            TheTeamsForecastsTakeIn(team, "CURRENT-1");
            await TheRemainingWorkIs(("CURRENT-1", 3));
            await NoForecastIsLeftOver();
        }

        [Test]
        public async Task AFeatureInNoPortfolioThatWasNeverAParent_IsDeletedWithItsRemainingWorkAndForecast()
        {
            var team = await store.ATeam("Team");
            await store.AFeature(team, "LEFT-1", remainingWork: 5, withAForecast: true);

            await TheCleanupRuns();

            await TheStoredFeaturesAre();
            await TheRemainingWorkIs();
            await NoForecastIsLeftOver();
        }

        [Test]
        public async Task AParentItemThatAFeatureInAPortfolioStillNamesAsItsParent_IsKept()
        {
            var team = await store.ATeam("Team");
            var portfolio = await store.APortfolio("Portfolio");
            await store.AFormerParentItem(team, "PARENT-1", remainingWork: 0, withAForecast: false);
            await store.AFeature(team, "CHILD-1", remainingWork: 2, inPortfolios: [portfolio], parent: "PARENT-1");

            await TheCleanupRuns();

            await TheStoredFeaturesAre("CHILD-1", "PARENT-1");
        }

        [Test]
        public async Task AFormerFeatureKeptOnlyAsTheParentOfAFeatureInAPortfolio_LosesItsRemainingWork_SoNoTeamShowsItAndTheFeaturesBelowItMoveUpInTheForecasts()
        {
            var team = await store.ATeam("Team");
            var otherTeam = await store.ATeam("Other Team");
            var portfolio = await store.APortfolio("Portfolio");
            await store.AFormerParentItem(team, "FORMER-PARENT-1", remainingWork: 5, withAForecast: true, alsoWorkedOnBy: [otherTeam]);
            await store.AFeature(team, "CURRENT-1", remainingWork: 3, inPortfolios: [portfolio]);
            await store.AFeature(team, "CHILD-1", remainingWork: 2, inPortfolios: [portfolio], parent: "FORMER-PARENT-1");

            await TheCleanupRuns();

            await TheStoredFeaturesAre("FORMER-PARENT-1", "CURRENT-1", "CHILD-1");
            TheChildStillShowsItsParentsName("FORMER-PARENT-1");
            await TheRemainingWorkIs(("CURRENT-1", 3), ("CHILD-1", 2));
            TheTeamsFeaturesTabShows(team, "CURRENT-1", "CHILD-1");
            TheTeamsFeaturesTabShows(otherTeam);
            TheForecastsOfTheTeamsTakeInThisOrder([team, otherTeam], "CURRENT-1", "CHILD-1");
        }

        [Test]
        public async Task AParentItemWhoseOnlyChildIsInNoPortfolio_IsDeletedWithThatChild()
        {
            var team = await store.ATeam("Team");
            var portfolio = await store.APortfolio("Portfolio");
            await store.AFormerParentItem(team, "PARENT-1", remainingWork: 4, withAForecast: false);
            await store.AFeature(team, "CHILD-1", remainingWork: 2, parent: "PARENT-1");
            await store.AFeature(team, "CURRENT-1", remainingWork: 3, inPortfolios: [portfolio], parent: "PARENT-2");

            await TheCleanupRuns();

            await TheStoredFeaturesAre("CURRENT-1");
            TheTeamsFeaturesTabShows(team, "CURRENT-1");
        }

        [Test]
        public async Task ASecondCleanupRightAfterTheFirst_DeletesNothingAndChangesNothing()
        {
            var team = await store.ATeam("Team");
            var portfolio = await store.APortfolio("Portfolio");
            await store.AFeature(team, "CURRENT-1", remainingWork: 3, inPortfolios: [portfolio]);
            await store.AFeature(team, "LEFT-1", remainingWork: 5, withAForecast: true);
            var cleanedStore = await store.CleanedOnce();

            await TheCleanupRuns();

            NothingWasDeleted();
            await TheStoreIsStill(cleanedStore);
        }

        [Test]
        public async Task AFormerFeatureThatAFeatureInAPortfolioNamesAsItsParent_StaysTheSameItemWithItsHistoryAndForecastOverRepeatedCleanups()
        {
            var team = await store.ATeam("Team");
            var portfolio = await store.APortfolio("Portfolio");
            await store.AFeature(team, "PARENT-1", remainingWork: 0, withAForecast: true);
            await store.TheFeatureMovedThroughStates("PARENT-1", "To Do", "Doing", "Done");
            await store.AFeature(team, "CHILD-1", remainingWork: 2, inPortfolios: [portfolio], parent: "PARENT-1");
            var parentId = await store.TheIdOf("PARENT-1");

            await TheCleanupRunsThreeTimesWatching("PARENT-1");

            EveryRunLeftTheParent(parentId, withStateChanges: 2, withForecasts: 1);
            NoRunAfterTheFirstDeletedAnything();
        }

        private async Task TheCleanupRuns()
        {
            deletedByTheLastRun = await store.TheCleanup().CleanupAsync();
        }

        private async Task TheStoredFeaturesAre(params string[] references)
        {
            Assert.That(await store.StoredFeatures(), Is.EquivalentTo(references),
                "The Features stored after the clean-up are not the ones expected. " +
                "A Feature is kept only while it is in a Portfolio or while a Feature in a Portfolio names it as its parent; any other Feature stays on the Team's Features tab and in the forecasts with stale numbers. " +
                "Make the clean-up delete every Feature that is in no Portfolio and that no Feature in a Portfolio names as its parent, and keep the others.");
        }

        private void TheTeamsFeaturesTabShows(Team team, params string[] references)
        {
            Assert.That(store.TheTeamsFeaturesTab(team), Is.EquivalentTo(references),
                "The Team's Features tab does not show exactly the Features expected after the clean-up. " +
                "A Feature that belongs to no Portfolio must not show there with its old progress. " +
                "Delete such a Feature, with its remaining work, in the clean-up.");
        }

        private void TheTeamsForecastsTakeIn(Team team, params string[] references)
        {
            Assert.That(store.TheFeaturesTheTeamsForecastsTakeIn(team), Is.EquivalentTo(references),
                "The forecasts of the Team still take in a Feature that belongs to no Portfolio, or miss one that does. " +
                "A stranded Feature takes the Team's throughput and pushes the real Features later. " +
                "Delete such a Feature, with its remaining work, in the clean-up.");
        }

        private void TheChildStillShowsItsParentsName(string parent)
        {
            Assert.That(store.TheParentNamesShownFor(parent), Is.EqualTo(new[] { parent }),
                "The parent of a Feature in a Portfolio no longer resolves to its name after the clean-up. " +
                "The Feature's page shows its parent by name, so a parent item outside every Portfolio is kept for that name. " +
                "Remove only the parent item's remaining work, never the parent item itself.");
        }

        private void TheForecastsOfTheTeamsTakeInThisOrder(Team[] teams, params string[] references)
        {
            Assert.That(store.TheFeaturesTheTeamsForecastsTakeIn(teams), Is.EqualTo(references),
                "The forecasts of the Teams do not take in exactly the Features in a Portfolio, in their order. " +
                "A parent item kept only for its name still takes the Teams' throughput ahead of the real Features and pushes their forecasts later. " +
                "Delete the remaining work of every Feature that is in no Portfolio in the clean-up, rather than setting it to zero.");
        }

        private async Task TheRemainingWorkIs(params (string Feature, int Remaining)[] remainingWork)
        {
            Assert.That(await store.RemainingWork(), Is.EquivalentTo(remainingWork),
                "The remaining work stored after the clean-up is not the remaining work of the Features kept, unchanged. " +
                "Remaining work of a Feature in no Portfolio keeps it on the Team's Features tab and in the forecasts, and the remaining work of a Feature in a Portfolio is what its progress and forecast read. " +
                "Delete the remaining work of every Feature in no Portfolio, with the Feature or, for a parent item kept only for its name, on its own, and leave that of a Feature in a Portfolio as it was.");
        }

        private async Task NoForecastIsLeftOver()
        {
            Assert.That(await store.ForecastCount(), Is.Zero,
                "A forecast is still stored for a Feature the clean-up deleted. " +
                "A forecast of a Feature that no longer exists is stale data nobody can see or correct. " +
                "Delete the forecasts together with the Feature.");
        }

        private async Task TheCleanupRunsThreeTimesWatching(string reference)
        {
            for (var run = 0; run < 3; run++)
            {
                var deleted = await store.TheCleanup().CleanupAsync();
                runs.Add((deleted, await store.TheHistoryOf(reference)));
            }
        }

        private void EveryRunLeftTheParent(int id, int withStateChanges, int withForecasts)
        {
            var asStored = new FeatureHistory(id, withStateChanges, withForecasts);
            Assert.That(runs.Select(run => run.Watched), Is.EqualTo(new[] { asStored, asStored, asStored }),
                "After some clean-up run the parent item is gone, has another Id, or lost state changes or forecasts. " +
                "A Feature in a Portfolio still names it as its parent, so deleting it and storing it again on the next refresh throws away its history and its forecasts each time. " +
                "Keep every Feature that a Feature in a Portfolio names as its parent, whether or not it was ever stored as a parent item, and leave its state changes and forecasts alone.");
        }

        private void NoRunAfterTheFirstDeletedAnything()
        {
            Assert.That(runs.Skip(1).Select(run => run.Deleted), Has.Exactly(2).Items.And.All.Zero,
                "A clean-up run after the first reported deleted Features on a store nothing had changed. " +
                "The clean-up runs after every refresh and at every start, so a run that keeps finding something to delete deletes and re-creates items over and over. " +
                "Make the rule depend only on Portfolios and parent links, so one run leaves nothing for the next.");
        }

        private void NothingWasDeleted()
        {
            Assert.That(deletedByTheLastRun, Is.Zero,
                "A second clean-up right after the first reported deleted Features. " +
                "The clean-up runs after every refresh and at every start, so a run that finds more to delete on a store it just cleaned would delete kept Features bit by bit. " +
                "Make the rule depend only on Portfolios and parent links, so one run leaves nothing for the next.");
        }

        private async Task TheStoreIsStill(StoreSnapshot cleanedStore)
        {
            Assert.That(await store.Snapshot(), Is.EqualTo(cleanedStore),
                "A second clean-up right after the first changed the stored Features or their remaining work. " +
                "Every refresh and start runs the clean-up again, so it must leave a cleaned store as it is. " +
                "Make the rule depend only on Portfolios and parent links, so one run leaves nothing for the next.");
        }
    }

    /// <summary>One stored Feature by Id, with how many state changes and forecasts it has.</summary>
    internal sealed record FeatureHistory(int Id, int StateChanges, int Forecasts);

    /// <summary>The stored Features by Id with their remaining work; equal when the same Features with the same work are stored.</summary>
    internal sealed record StoreSnapshot(string Features, string RemainingWork);

    /// <summary>
    /// A database of the scenario's own, on SQLite in memory with foreign keys on, as production has them, so deleting a
    /// Feature deletes its remaining work and forecasts. Features are named by their reference.
    /// </summary>
    internal sealed class FeatureStore : IDisposable
    {
        private readonly Mock<ICryptoService> cryptoService = new();
        private readonly Mock<ILogger<LighthouseAppContext>> contextLogger = new();
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<LighthouseAppContext> options;
        private readonly LighthouseAppContext seed;

        public FeatureStore()
        {
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON";
                pragma.ExecuteNonQuery();
            }

            options = new DbContextOptionsBuilder<LighthouseAppContext>().UseSqlite(connection).Options;
            seed = NewContext();
            seed.Database.EnsureCreated();
        }

        public LighthouseAppContext NewContext() => new(options, cryptoService.Object, contextLogger.Object);

        public async Task<Team> ATeam(string name)
        {
            var team = new Team { Name = name, WorkTrackingSystemConnectionId = (await AConnection(name)).Id };
            seed.Teams.Add(team);
            await seed.SaveChangesAsync();
            return team;
        }

        public async Task<Portfolio> APortfolio(string name)
        {
            var portfolio = new Portfolio { Name = name, WorkTrackingSystemConnectionId = (await AConnection(name)).Id };
            seed.Portfolios.Add(portfolio);
            await seed.SaveChangesAsync();
            return portfolio;
        }

        public Task AFeature(Team team, string reference, int remainingWork, Portfolio[]? inPortfolios = null, string parent = "", bool withAForecast = false)
            => Store(team, reference, remainingWork, inPortfolios ?? [], parent, flaggedAsParent: false, withAForecast);

        /// <summary>An item the parent half of a refresh stored as a parent, which stays flagged as one ever after.</summary>
        public Task AFormerParentItem(Team team, string reference, int remainingWork, bool withAForecast, Portfolio[]? inPortfolios = null, Team[]? alsoWorkedOnBy = null)
            => Store(team, reference, remainingWork, inPortfolios ?? [], parent: "", flaggedAsParent: true, withAForecast, alsoWorkedOnBy ?? []);

        public IOrphanedFeatureCleanupService TheCleanup()
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => NewContext());
            var scopes = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
            return new OrphanedFeatureCleanupService(scopes, Mock.Of<ILogger<OrphanedFeatureCleanupService>>());
        }

        public async Task<StoreSnapshot> CleanedOnce()
        {
            await TheCleanup().CleanupAsync();
            return await Snapshot();
        }

        public async Task<List<string>> StoredFeatures()
        {
            using var context = NewContext();
            return await context.Features.Select(feature => feature.ReferenceId).ToListAsync();
        }

        public List<string> TheTeamsFeaturesTab(Team team)
        {
            using var context = NewContext();
            return [.. Features(context).FeaturesOfTeam(team.Id).Select(feature => feature.ReferenceId)];
        }

        public List<string> TheFeaturesTheTeamsForecastsTakeIn(params Team[] teams)
        {
            using var context = NewContext();
            return [.. Features(context).FeaturesWorkedOnBy([.. teams.Select(team => team.Id)]).Select(feature => feature.ReferenceId)];
        }

        public List<string> TheParentNamesShownFor(string parent)
        {
            using var context = NewContext();
            return [.. Features(context).ParentsOf([parent]).Select(found => found.Name)];
        }

        public async Task<List<(string Feature, int Remaining)>> RemainingWork()
        {
            using var context = NewContext();
            var rows = await context.Set<FeatureWork>()
                .Select(work => new { work.Feature.ReferenceId, work.RemainingWorkItems })
                .ToListAsync();
            return [.. rows.Select(row => (row.ReferenceId, row.RemainingWorkItems))];
        }

        public async Task<int> ForecastCount()
        {
            using var context = NewContext();
            return await context.Set<WhenForecast>().CountAsync();
        }

        public async Task TheFeatureMovedThroughStates(string reference, params string[] states)
        {
            var feature = await seed.Features.SingleAsync(stored => stored.ReferenceId == reference);
            var day = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);
            for (var step = 1; step < states.Length; step++)
            {
                seed.FeatureStateTransitions.Add(new FeatureStateTransition
                {
                    FeatureId = feature.Id,
                    FromState = states[step - 1],
                    ToState = states[step],
                    TransitionedAt = day.AddDays(step),
                });
            }

            await seed.SaveChangesAsync();
        }

        public async Task<int> TheIdOf(string reference)
        {
            using var context = NewContext();
            return await context.Features.Where(feature => feature.ReferenceId == reference).Select(feature => feature.Id).SingleAsync();
        }

        public async Task<FeatureHistory?> TheHistoryOf(string reference)
        {
            using var context = NewContext();
            var id = await context.Features.Where(feature => feature.ReferenceId == reference).Select(feature => (int?)feature.Id).SingleOrDefaultAsync();
            if (id is null)
            {
                return null;
            }

            var stateChanges = await context.FeatureStateTransitions.CountAsync(transition => transition.FeatureId == id);
            var forecasts = await context.Set<WhenForecast>().CountAsync(forecast => forecast.FeatureId == id);
            return new FeatureHistory(id.Value, stateChanges, forecasts);
        }

        public async Task<StoreSnapshot> Snapshot()
        {
            using var context = NewContext();
            var features = await context.Features.OrderBy(feature => feature.Id).Select(feature => feature.Id + ":" + feature.ReferenceId).ToListAsync();
            var work = await context.Set<FeatureWork>().OrderBy(row => row.FeatureId).ThenBy(row => row.TeamId)
                .Select(row => row.FeatureId + "/" + row.TeamId + ":" + row.RemainingWorkItems + "/" + row.TotalWorkItems).ToListAsync();
            return new StoreSnapshot(string.Join(", ", features), string.Join(", ", work));
        }

        public void Dispose()
        {
            seed.Dispose();
            connection.Dispose();
        }

        private static FeatureRepository Features(LighthouseAppContext context)
            => new(context, FeatureOrderingTestHelper.FollowingTheTracker(), Mock.Of<ILogger<FeatureRepository>>());

        private async Task Store(Team team, string reference, int remainingWork, Portfolio[] portfolios, string parent, bool flaggedAsParent, bool withAForecast, Team[]? alsoWorkedOnBy = null)
        {
            var feature = new Feature(team, remainingWork)
            {
                Name = reference,
                ReferenceId = reference,
                ParentReferenceId = parent,
                IsParentFeature = flaggedAsParent,
                Order = "1",
            };
            foreach (var portfolio in portfolios)
            {
                feature.Portfolios.Add(portfolio);
            }

            foreach (var otherTeam in alsoWorkedOnBy ?? [])
            {
                feature.AddOrUpdateWorkForTeam(otherTeam, remainingWork, remainingWork);
            }

            if (withAForecast)
            {
                feature.Forecasts.Add(new WhenForecast { NumberOfItems = remainingWork });
            }

            seed.Features.Add(feature);
            await seed.SaveChangesAsync();
        }

        private async Task<WorkTrackingSystemConnection> AConnection(string owner)
        {
            var workTrackingConnection = new WorkTrackingSystemConnection
            {
                Name = $"{owner} Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = "pat",
            };
            seed.WorkTrackingSystemConnections.Add(workTrackingConnection);
            await seed.SaveChangesAsync();
            return workTrackingConnection;
        }
    }
}
