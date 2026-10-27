using System.Data.Common;
using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize
{
    /// <summary>
    /// A real host over a fresh SQLite database, with Teams, Portfolios,
    /// Features and Work Items seeded the way a sync leaves them. Each Feature records how many of
    /// each Team's Work Items sit under it, which is what a Team's size of that Feature is read from.
    ///
    /// The instance clock is pinned so the windows the scenarios ask about stay put, and every date is
    /// a number of days before that fixed today. Every command the host sends to the database is
    /// counted, so a scenario can tell whether an answer costs more of them as the data grows.
    ///
    /// Percentiles are read the way Lighthouse takes them: sort the sizes from smallest to largest;
    /// the Nth percentile is the size at place N% of the count, rounded down, counting the smallest as
    /// place 1, or the smallest when that place comes to less than 1.
    /// </summary>
    public abstract class TeamFeatureSizeAcceptanceTest
    {
        protected const string ToDo = "To Do";
        protected const string InProgress = "In Progress";
        protected const string Done = "Done";

        protected const string Equinox = "Equinox";
        protected const string Gravity = "Gravity";
        protected const string Lightspeed = "Lightspeed";
        protected const string Zenith = "Zenith";
        protected const string Meridian = "Meridian";
        protected const string Polaris = "Polaris";
        protected const string OceanExplorer = "Ocean Explorer";
        protected const string Orion = "Orion";

        protected const string AllFeaturesForSizeChart = "allFeaturesForSizeChart";
        protected const string SizePercentiles = "sizePercentiles";
        protected const string FeatureSizePercentilesInfo = "featureSizePercentilesInfo";
        protected const string FeaturesInProgress = "featuresInProgress";

        /// <summary>No Team is ever seeded with this number.</summary>
        protected const int NoSuchTeam = 9999;

        protected static readonly DateTimeOffset Today = new(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);

        private readonly Dictionary<string, int> teamIds = [];

        private readonly Dictionary<string, int> portfolioIds = [];

        private readonly List<int> commandsPerAnswer = [];

        private TestWebApplicationFactory<Program> rootFactory = null!;

        private CommandsSentToTheDatabase commands = null!;

        private int featuresSeededForCounting;

        private HttpStatusCode readerOfTheTeamWasAnswered;

        private HttpStatusCode readerOfAnotherTeamWasAnswered;

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        protected HttpResponseMessage Response { get; private set; } = null!;

        protected string Body { get; private set; } = string.Empty;

        /// <summary>The window most scenarios ask about: the 30 days up to and including today.</summary>
        protected static DateTime WindowStart => Today.UtcDateTime.Date.AddDays(-29);

        protected static DateTime WindowEnd => Today.UtcDateTime.Date;

        [SetUp]
        public void Init()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            // NUnit keeps one fixture instance for every test in the class, and each test starts
            // from an empty database, so names remembered by the previous test point at nothing.
            teamIds.Clear();
            portfolioIds.Clear();
            commandsPerAnswer.Clear();
            featuresSeededForCounting = 0;

            commands = new CommandsSentToTheDatabase();
            rootFactory = new TestWebApplicationFactory<Program>();

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory).WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILighthouseClock>();
                    services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Today));
                    services.ConfigureDbContext<LighthouseAppContext>((_, options) => options.AddInterceptors(commands));
                });
            });

            Client = Factory.CreateClient();

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            using (var scope = Factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Response?.Dispose();
            Client.Dispose();
            Factory.Dispose();
            rootFactory.Dispose();
        }

        // --- Given ---

        /// <summary>
        /// Gravity's parts of ten Features closed in the window are 1 to 10, one closed each day
        /// before today. Zenith has 20 Work Items under each, so a whole-Feature number would show.
        /// </summary>
        protected void GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen()
        {
            var oceanExplorer = PortfolioNamed(OceanExplorer);
            var gravity = TeamNamed(Gravity);
            var zenith = TeamNamed(Zenith);

            for (var part = 1; part <= 10; part++)
            {
                SeedFeature($"OE-3{part:00}", StateCategories.Done, [oceanExplorer], [(gravity, part), (zenith, 20)], closedDaysAgo: part);
            }
        }

        /// <summary>
        /// Features of Ocean Explorer that Gravity works on, every other one closed two days ago and
        /// the rest in progress. Gravity's part of the Nth one is N; Zenith has 3 Work Items under each.
        /// </summary>
        protected void GivenGravityWorksOnFeatures(int howMany)
        {
            var gravity = TeamNamed(Gravity);
            var zenith = TeamNamed(Zenith);
            var oceanExplorer = PortfolioNamed(OceanExplorer);

            for (var added = 0; added < howMany; added++)
            {
                featuresSeededForCounting++;
                var state = featuresSeededForCounting % 2 == 0 ? StateCategories.Done : StateCategories.Doing;
                SeedFeature($"OE-4{featuresSeededForCounting:00}", state, [oceanExplorer],
                    [(gravity, featuresSeededForCounting), (zenith, 3)], closedDaysAgo: 2);
            }
        }

        /// <summary>Gravity has taken on more Features since it was last asked.</summary>
        protected void GivenGravityHasTakenOnMoreFeatures(int howMany) => GivenGravityWorksOnFeatures(howMany);

        // --- When ---

        /// <summary>
        /// Gravity is refreshed the way a sync ends, so nothing it remembered can answer for it, and
        /// is then asked once as a System Admin. The commands that one request sends to the database
        /// are counted.
        ///
        /// The host is asked once before the refresh, so that anything it reads only on its first
        /// request of a route is not counted against the answer.
        /// </summary>
        protected async Task WhenGravityIsRefreshedAndAskedFor(string metric)
        {
            var gravity = TeamNamed(Gravity);
            var route = TeamMetric(gravity, metric, WindowStart, WindowEnd);

            Client.AsSystemAdmin();
            await Ask(route);

            RefreshTheTeam(gravity);

            var before = commands.Count;
            await Ask(route);
            commandsPerAnswer.Add(commands.Count - before);
        }

        protected async Task WhenAReaderOfTheTeamAndAReaderOfAnotherTeamBothAsk(Uri route, int teamId, int otherTeamId)
        {
            Client.AsTeamViewer(teamId, "reads-this-team");
            await Ask(route);
            readerOfTheTeamWasAnswered = Response.StatusCode;

            Client.AsTeamViewer(otherTeamId, "reads-another-team");
            await Ask(route);
            readerOfAnotherTeamWasAnswered = Response.StatusCode;
        }

        /// <summary>Someone who may read the Team and each of the named Portfolios, and nothing else.</summary>
        protected void AsAReaderOf(int teamId, params int[] readablePortfolioIds)
        {
            var grants = readablePortfolioIds
                .Select(id => $"{ClaimsDrivenRbacAdministrationService.ViewerPortfolioGrantPrefix}{id}")
                .Prepend($"{ClaimsDrivenRbacAdministrationService.ViewerTeamGrantPrefix}{teamId}");

            Client.DefaultRequestHeaders.Remove(TestAuthHandler.SubjectHeader);
            Client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
            Client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "reads-the-team-and-some-portfolios");
            Client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', grants));
        }

        /// <summary>
        /// The step every Team refresh ends with once the Team's Work Items are synced:
        /// <c>TeamDataService.UpdateTeamData</c> calls <c>ITeamMetricsService.UpdateTeamMetrics</c>
        /// right after <c>UpdateWorkItemsForTeam</c>, which forgets the Team's remembered metrics and
        /// moves its last update. The sync before it reaches the work tracking system, which this host
        /// does not have, so the scenarios start from this step.
        /// </summary>
        protected void RefreshTheTeam(int teamId)
        {
            using var scope = Factory.Services.CreateScope();
            var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
            scope.ServiceProvider.GetRequiredService<ITeamMetricsService>().UpdateTeamMetrics(team).GetAwaiter().GetResult();
        }

        /// <summary>
        /// What a Portfolio refresh announces once its Features are synced. Every handler the instance
        /// registers for it runs, including the one that forgets the Portfolio's remembered metrics.
        /// </summary>
        protected void RefreshThePortfolio(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>()
                .PublishAsync(new PortfolioFeaturesRefreshed(portfolioId))
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// A scenario may ask several times, so each answer replaces and disposes the one before it.
        /// </summary>
        protected async Task Ask(Uri route)
        {
            Response?.Dispose();
            Response = await Client.GetAsync(route);
            Body = await Response.Content.ReadAsStringAsync();
        }

        // --- Then ---

        protected void ThenTheAnswerArrived()
        {
            Assert.That(Response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Asked {Response.RequestMessage?.RequestUri}. Body: {Body}");
        }

        protected void ThenTheAnswerSaysThereIsNoSuchTeam()
        {
            Assert.That(Response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                $"Asked {Response.RequestMessage?.RequestUri}. Body: {Body}");
        }

        protected void ThenTheWindowIsRefused()
        {
            Assert.That(Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"Asked {Response.RequestMessage?.RequestUri}. Body: {Body}");
        }

        protected void ThenOnlyTheReaderOfTheTeamIsAnswered()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(readerOfTheTeamWasAnswered, Is.EqualTo(HttpStatusCode.OK),
                    "Reading the Team is enough to be answered.");
                Assert.That(readerOfAnotherTeamWasAnswered, Is.EqualTo(HttpStatusCode.NotFound),
                    "Someone who cannot read the Team is told there is no such Team, like every other Team metric does.");
            }
        }

        /// <summary>
        /// The last answer cost as many database commands as the first, and the first cost some, so
        /// the count is real rather than an answer that never reached the database.
        /// </summary>
        protected void ThenTheLastAnswerCostAsManyCommandsAsTheFirst()
        {
            var first = commandsPerAnswer[0];
            var last = commandsPerAnswer[^1];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.GreaterThan(0), "The first answer should have read the database.");
                Assert.That(last, Is.EqualTo(first),
                    $"The first answer took {first} database commands and the last took {last}.");
            }
        }

        /// <summary>
        /// The percentiles of Gravity's parts of the Done Features <see cref="GivenGravityWorksOnFeatures"/>
        /// seeds: with 2 Features, one is Done with a part of 2; with 20, ten are Done with parts 2, 4 … 20.
        /// </summary>
        protected static Dictionary<int, int> PercentilesOfTheFeaturesGravityWorksOn(int howMany) => howMany switch
        {
            2 => new Dictionary<int, int> { [50] = 2, [70] = 2, [85] = 2, [95] = 2 },
            20 => new Dictionary<int, int> { [50] = 10, [70] = 14, [85] = 16, [95] = 18 },
            _ => throw new ArgumentOutOfRangeException(nameof(howMany), howMany, "Only 2 and 20 are worked out."),
        };

        // --- Helpers ---

        /// <summary>The Team of that name, seeded the first time a scenario mentions it.</summary>
        protected int TeamNamed(string name)
        {
            if (!teamIds.TryGetValue(name, out var id))
            {
                id = SeedTeam(name);
                teamIds[name] = id;
            }

            return id;
        }

        /// <summary>The Portfolio of that name, seeded the first time a scenario mentions it.</summary>
        protected int PortfolioNamed(string name)
        {
            if (!portfolioIds.TryGetValue(name, out var id))
            {
                id = SeedPortfolio(name);
                portfolioIds[name] = id;
            }

            return id;
        }

        protected int SeedTeam(string name)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = ANewConnection(),
                WorkItemTypes = ["User Story"],
                ToDoStates = ["New"],
                DoingStates = [InProgress],
                DoneStates = [Done],
            };

            repository.Add(team);
            repository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        protected int SeedPortfolio(string name)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = ANewConnection(),
                ToDoStates = [ToDo],
                DoingStates = [InProgress],
                DoneStates = [Done],
            };

            repository.Add(portfolio);
            repository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        /// <summary>
        /// A Feature as a Portfolio refresh leaves it: in each of <paramref name="portfolioIds"/>, with
        /// <paramref name="workItemsPerTeam"/> naming how many of each Team's Work Items sit under it.
        /// A Feature that is done closed <paramref name="closedDaysAgo"/> days before today.
        /// </summary>
        protected void SeedFeature(
            string referenceId,
            StateCategories stateCategory,
            int[] portfolioIds,
            (int TeamId, int WorkItems)[] workItemsPerTeam,
            int closedDaysAgo = 5,
            bool sizedByThePortfolioDefault = false)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var teams = sp.GetRequiredService<IRepository<Team>>();
            var portfolios = sp.GetRequiredService<IRepository<Portfolio>>();
            var features = sp.GetRequiredService<IRepository<Feature>>();

            var closedDate = Today.UtcDateTime.Date.AddDays(-closedDaysAgo).AddHours(12);

            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Feature {referenceId}",
                Type = "Epic",
                State = StateNameFor(stateCategory),
                StateCategory = stateCategory,
                CreatedDate = closedDate.AddDays(-40),
                StartedDate = stateCategory == StateCategories.ToDo ? null : closedDate.AddDays(-20),
                ClosedDate = stateCategory == StateCategories.Done ? closedDate : null,
                Order = referenceId,
                IsUsingDefaultFeatureSize = sizedByThePortfolioDefault,
            };

            foreach (var portfolioId in portfolioIds)
            {
                feature.Portfolios.Add(portfolios.GetById(portfolioId)!);
            }

            foreach (var (teamId, workItems) in workItemsPerTeam)
            {
                feature.FeatureWork.Add(new FeatureWork(teams.GetById(teamId)!, remainingWork: 0, totalWorkItems: workItems, feature));
            }

            features.Add(feature);
            features.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// A Work Item the Team finished <paramref name="closedDaysAgo"/> days before today, after
        /// <paramref name="cycleTimeInDays"/> days of work (both ends counted).
        /// </summary>
        protected void SeedFinishedWorkItem(int teamId, int closedDaysAgo, int cycleTimeInDays)
        {
            var closed = Today.UtcDateTime.Date.AddDays(-closedDaysAgo).AddHours(12);
            var started = closed.AddDays(-(cycleTimeInDays - 1));

            SeedWorkItem(teamId, $"Finished in {cycleTimeInDays} days", StateCategories.Done, started, closed, parentReferenceId: string.Empty);
        }

        /// <summary>A Work Item the Team started ten days ago and is still working on, under that parent.</summary>
        protected void SeedWorkItemInProgress(int teamId, string parentReferenceId)
        {
            var started = Today.UtcDateTime.Date.AddDays(-10);

            SeedWorkItem(teamId, $"Work under {parentReferenceId}", StateCategories.Doing, started, closed: null, parentReferenceId);
        }

        protected static Uri TeamMetric(int teamId, string metric, DateTime start, DateTime end) => new(
            $"/api/latest/teams/{teamId}/metrics/{metric}?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}",
            UriKind.Relative);

        protected static Uri PortfolioMetric(int portfolioId, string metric, DateTime start, DateTime end) => new(
            $"/api/latest/portfolios/{portfolioId}/metrics/{metric}?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}",
            UriKind.Relative);

        /// <summary>The Features the Team is working on as of today, as the "Features worked on" list asks.</summary>
        protected static Uri TeamFeaturesInProgress(int teamId) => new(
            $"/api/latest/teams/{teamId}/metrics/{FeaturesInProgress}?asOfDate={WindowEnd:yyyy-MM-dd}",
            UriKind.Relative);

        private void SeedWorkItem(int teamId, string name, StateCategories stateCategory, DateTime started, DateTime? closed, string parentReferenceId)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var reference = $"WI-{Guid.NewGuid():N}";

            var repository = sp.GetRequiredService<IWorkItemRepository>();
            repository.Add(new WorkItem
            {
                TeamId = teamId,
                Team = sp.GetRequiredService<IRepository<Team>>().GetById(teamId)!,
                ReferenceId = reference,
                Name = name,
                Type = "User Story",
                State = StateNameFor(stateCategory),
                StateCategory = stateCategory,
                CreatedDate = started.AddDays(-1),
                StartedDate = started,
                ClosedDate = closed,
                ParentReferenceId = parentReferenceId,
                Order = reference,
            });

            repository.Save().GetAwaiter().GetResult();
        }

        private static string StateNameFor(StateCategories stateCategory) => stateCategory switch
        {
            StateCategories.ToDo => ToDo,
            StateCategories.Doing => InProgress,
            _ => Done,
        };

        private static WorkTrackingSystemConnection ANewConnection() => new()
        {
            Name = $"Connection {Guid.NewGuid():N}",
            WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
        };

        /// <summary>Counts every query, scalar and write command, so no kind of database round trip escapes the count.</summary>
        private sealed class CommandsSentToTheDatabase : DbCommandInterceptor
        {
            private int count;

            public int Count => Volatile.Read(ref count);

            public override InterceptionResult<DbDataReader> ReaderExecuting(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            {
                Interlocked.Increment(ref count);
                return result;
            }

            public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref count);
                return ValueTask.FromResult(result);
            }

            public override InterceptionResult<object> ScalarExecuting(
                DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
            {
                Interlocked.Increment(ref count);
                return result;
            }

            public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref count);
                return ValueTask.FromResult(result);
            }

            public override InterceptionResult<int> NonQueryExecuting(
                DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
            {
                Interlocked.Increment(ref count);
                return result;
            }

            public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref count);
                return ValueTask.FromResult(result);
            }
        }
    }
}
