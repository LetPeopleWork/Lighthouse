using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange
{
    /// <summary>
    /// A real host over a fresh SQLite database, signed in as a System Admin, holding Nordwind Logistics'
    /// connections, Teams and Portfolios. The connection is saved through the same route the connection
    /// page and the command line use, with the body the page would send: the connection as it was read,
    /// with the admin's edit applied.
    ///
    /// What a save started is read where the admin reads it. While an update waits or runs it is a row in
    /// the Task Manager; once it has run it is an entry in the refresh history, which says whether that
    /// update downloaded everything or only what had changed. The update queue, the updaters, the
    /// fingerprint and the refresh history are all production. Only the tracker is a stand-in: each Team
    /// and Portfolio holds one item on it that never changes. It can be told to hold every answer until
    /// the scenario lets it go, and to refuse one Team. It says it can update incrementally, so an update
    /// that downloads everything is one that had a reason to.
    /// </summary>
    public abstract class ConnectionFieldChangeAcceptanceTest
    {
        protected const string NordwindJiraDc = "Nordwind Jira DC";
        protected const string NordwindAzureDevOps = "Nordwind Azure DevOps";
        protected const string NordwindJiraSandbox = "Nordwind Jira Sandbox";
        protected const string NordwindJiraRoadmaps = "Nordwind Jira Roadmaps";

        protected const string Atlas = "Atlas";
        protected const string Borealis = "Borealis";
        protected const string Cobalt = "Cobalt";
        protected const string DeltaOps = "Delta Ops";
        protected const string Harbor = "Harbor";
        protected const string FleetPlatform = "Fleet Platform";
        protected const string CustomerApps = "Customer Apps";
        protected const string RoadmapNorth = "Roadmap North";
        protected const string RoadmapSouth = "Roadmap South";

        protected const string StoryPoints = "Story Points";
        protected const string StoryPointsReference = "customfield_10016";
        protected const string FixVersions = "Fix Versions";
        protected const string FixVersionsReference = "fixVersions";

        /// <summary>
        /// The admin's "Save and update": store the connection and start the updates now.
        /// </summary>
        protected const string UpdateNow = "true";

        /// <summary>
        /// The admin's "Save only": store the connection and leave the updates to the schedule.
        /// </summary>
        protected const string SaveOnly = "false";

        private const string ConnectionsRoute = "/api/latest/worktrackingsystemconnections";
        private const string TaskListRoute = "/api/latest/update/tasks";
        private const string JsonMediaType = "application/json";
        private const string AdditionalFieldDefinitions = "additionalFieldDefinitions";
        private const string DisplayName = "displayName";
        private const string Reference = "reference";

        private static readonly string JiraAddress = JiraWorkTrackingOptionNames.Url;
        private static readonly string JiraUsername = JiraWorkTrackingOptionNames.Username;

        /// <summary>
        /// How long the admin may wait between pressing Save and seeing the updates in the Task Manager.
        /// </summary>
        protected static readonly TimeSpan SoonAfterTheSave = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan LongEnoughForAnyUpdateToFinish = TimeSpan.FromSeconds(30);

        protected static readonly string[] EveryTeamAndPortfolioOnNordwindJiraDc =
            [Atlas, Borealis, Cobalt, DeltaOps, FleetPlatform, CustomerApps];

        protected static readonly string[] NordwindJiraDcsTeams = [Atlas, Borealis, Cobalt, DeltaOps];

        protected static readonly string[] NordwindJiraDcsPortfolios = [FleetPlatform, CustomerApps];

        private static readonly string[] TheCommunityLicencesThreeTeams = [Atlas, Borealis, Cobalt];

        private readonly Dictionary<string, int> connectionIds = [];
        private readonly Dictionary<string, int> teamIds = [];
        private readonly Dictionary<string, int> portfolioIds = [];
        private readonly HashSet<string> teamsTheTrackerRefuses = [];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private Mock<ILicenseService> licence = null!;
        private Mock<IWorkTrackingConnector> tracker = null!;
        private TaskCompletionSource trackerMayAnswer = null!;
        private int lastHistoryEntryBeforeTheSave;

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        protected HttpStatusCode SaveAnswer { get; private set; }

        protected string SaveAnswerBody { get; private set; } = string.Empty;

        public enum FieldEdit
        {
            AddsAField,
            RenamesAFieldsLabel,
            ChangesAFieldsReference,
            RemovesAField,
        }

        public enum EditThatChangesNothingFetched
        {
            NewApiToken,
            NewConnectionName,
            NewJiraAddress,
            NewSignInMethod,
            NewWriteBackMapping,
            SameFieldsInAnotherOrder,
        }

        public enum Refusal
        {
            SomeoneElseSavedFirst,
            TooManyFieldsForTheLicence,
            AnUnfinishedWriteBackMapping,
        }

        [SetUp]
        public async Task Init()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            // NUnit keeps one fixture instance for every test in the class, and each test starts from an
            // empty database, so names remembered by the previous test point at nothing.
            connectionIds.Clear();
            teamIds.Clear();
            portfolioIds.Clear();
            teamsTheTrackerRefuses.Clear();
            lastHistoryEntryBeforeTheSave = 0;
            SaveAnswer = default;
            SaveAnswerBody = string.Empty;

            trackerMayAnswer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            trackerMayAnswer.SetResult();

            licence = new Mock<ILicenseService>();
            licence.Setup(service => service.CanUsePremiumFeatures()).Returns(true);

            tracker = ATrackerThatAnswersWithOneItemEach();
            var connectorFactory = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactory
                .Setup(factory => factory.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(tracker.Object);

            rootFactory = new TestWebApplicationFactory<Program>();
            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory).WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licence.Object);

                    services.RemoveAll<IWorkTrackingConnectorFactory>();
                    services.AddScoped(_ => connectorFactory.Object);

                    services.RemoveAll<IForecastService>();
                    services.AddScoped(_ => ForecastServiceDoubles.AnsweringComputed().Object);
                });
            });

            Client = Factory.CreateClient();
            Client.AsSystemAdmin();

            using var scope = Factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await database.Database.EnsureDeletedAsync();
            await database.Database.EnsureCreatedAsync();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
            }
        }

        /// <summary>
        /// Lets any held update finish before the database beneath it is deleted.
        /// </summary>
        [TearDown]
        public async Task Cleanup()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            trackerMayAnswer.TrySetResult();
            await TheQueueGoesIdle();

            using (var scope = Factory.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeletedAsync();
            }

            Client.Dispose();
            Factory.Dispose();
            rootFactory.Dispose();
        }

        // --- Given ---

        /// <summary>
        /// Nordwind Logistics: "Nordwind Jira DC" reads Story Points for Teams Atlas, Borealis, Cobalt and
        /// Delta Ops and Portfolios Fleet Platform and Customer Apps. Team Harbor reads from "Nordwind
        /// Azure DevOps". Nobody reads from "Nordwind Jira Sandbox".
        /// </summary>
        protected async Task GivenNordwindsConnectionsTeamsAndPortfolios()
        {
            await SeedConnection(NordwindJiraDc, WorkTrackingSystems.Jira, (StoryPoints, StoryPointsReference));
            await SeedConnection(NordwindAzureDevOps, WorkTrackingSystems.AzureDevOps);
            await SeedConnection(NordwindJiraSandbox, WorkTrackingSystems.Jira, (StoryPoints, StoryPointsReference));

            foreach (var team in NordwindJiraDcsTeams)
            {
                await SeedTeam(team, NordwindJiraDc);
            }

            await SeedTeam(Harbor, NordwindAzureDevOps);

            foreach (var portfolio in NordwindJiraDcsPortfolios)
            {
                await SeedPortfolio(portfolio, NordwindJiraDc);
            }
        }

        /// <summary>
        /// The community licence updates at most three Teams and one Portfolio, so this instance is that
        /// size: "Nordwind Jira DC" with no additional field yet, read by Atlas, Borealis, Cobalt and Fleet
        /// Platform.
        /// </summary>
        protected async Task GivenASmallInstanceOnTheCommunityLicence()
        {
            licence.Setup(service => service.CanUsePremiumFeatures()).Returns(false);

            await SeedConnection(NordwindJiraDc, WorkTrackingSystems.Jira);

            foreach (var team in TheCommunityLicencesThreeTeams)
            {
                await SeedTeam(team, NordwindJiraDc);
            }

            await SeedPortfolio(FleetPlatform, NordwindJiraDc);
        }

        /// <summary>
        /// "Nordwind Jira Roadmaps" is read by the Portfolios Roadmap North and Roadmap South and by no Team.
        /// </summary>
        protected async Task GivenAConnectionOnlyPortfoliosReadFrom()
        {
            await SeedConnection(NordwindJiraRoadmaps, WorkTrackingSystems.Jira);
            await SeedPortfolio(RoadmapNorth, NordwindJiraRoadmaps);
            await SeedPortfolio(RoadmapSouth, NordwindJiraRoadmaps);
        }

        /// <summary>
        /// A second field on the connection, already stored, so its fields have an order that can change.
        /// </summary>
        protected async Task GivenTheConnectionAlsoReads(string connection, string displayName, string reference)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            var stored = repository.GetById(connectionIds[connection])!;

            stored.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = displayName, Reference = reference });
            repository.Update(stored);
            await repository.Save();
        }

        protected void GivenTheInstanceRunsOnTheCommunityLicence()
            => licence.Setup(service => service.CanUsePremiumFeatures()).Returns(false);

        /// <summary>
        /// Every one of them has been updated once, so each remembers what it asked the tracker for, and
        /// an update with nothing new to ask for would only fetch what changed.
        /// </summary>
        protected async Task GivenEveryTeamAndPortfolioHasBeenUpdatedOnce()
        {
            foreach (var team in teamIds.Values)
            {
                Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team);
                await TheQueueGoesIdle();
            }

            foreach (var portfolio in portfolioIds.Values)
            {
                Factory.Services.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio);
                await TheQueueGoesIdle();
            }
        }

        /// <summary>
        /// An update that has started stays where it is until the scenario lets the tracker answer, and
        /// the queue, which runs one update at a time, keeps everything else waiting behind it.
        /// </summary>
        protected void GivenTheTrackerDoesNotAnswerUntilToldTo()
            => trackerMayAnswer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        protected void GivenTheTrackerRefusesTheAccountFor(string team) => teamsTheTrackerRefuses.Add(team);

        protected void GivenTheTrackerAcceptsTheAccountAgain() => teamsTheTrackerRefuses.Clear();

        /// <summary>
        /// The tracker offers a field of its own that Lighthouse registers on a connection when it reads
        /// the connection, as Jira does with its Releases.
        /// </summary>
        protected void GivenTheTrackerOffersAFieldOfItsOwn(string displayName, string reference)
            => tracker
                .Setup(connector => connector.GetPredefinedAdditionalFields(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns([new AdditionalFieldDefinition { DisplayName = displayName, Reference = reference, IsPredefined = true }]);

        protected async Task GivenTheUpdateOfTeamIsRunning(string team)
        {
            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(teamIds[team]);
            await TheUpdateOfIs(new UpdateKey(UpdateType.Team, teamIds[team]), UpdateProgress.InProgress);
        }

        protected async Task GivenTheUpdateOfTeamIsWaiting(string team)
        {
            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(teamIds[team]);
            await TheUpdateOfIs(new UpdateKey(UpdateType.Team, teamIds[team]), UpdateProgress.Queued);
        }

        /// <summary>
        /// Everything recorded before this moment is history the scenario is not about.
        /// </summary>
        protected void GivenTheRefreshHistorySoFarIsSetAside()
            => lastHistoryEntryBeforeTheSave = TheRefreshHistory().Select(entry => entry.Id).DefaultIfEmpty(0).Max();

        // The steps below are the same actions and checks as the When and Then steps of the same name, used
        // where an earlier scenario's outcome is the starting point of the next one.

        protected async Task GivenPriyaAddedFixVersionsAndSaved(string connection, string? choice = null)
        {
            await WhenPriyaAddsFixVersionsAndSaves(connection, choice);
            ThenTheSaveIsAccepted();
        }

        protected Task GivenTheUpdateOfHasFailed(string team) => ThenTheUpdateOfFailed(team);

        protected Task GivenNoTeamOrPortfolioHasBeenUpdatedSince() => ThenNoTeamOrPortfolioIsUpdated();

        protected Task GivenSomeoneHasLookedUpTheConnection(string connection) => WhenSomeoneLooksUpTheConnection(connection);

        // --- When: the admin saves the connection ---

        protected Task WhenPriyaSaves(string connection, FieldEdit edit, string? choice = null)
            => SaveTheConnection(connection, read => ApplyAFieldEdit(read, edit), choice);

        protected Task WhenPriyaSaves(string connection, EditThatChangesNothingFetched edit)
            => SaveTheConnection(connection, read => ApplyAnEditThatChangesNothingFetched(read, edit), choice: null);

        protected Task WhenPriyaAddsFixVersionsAndSaves(string connection, string? choice = null)
            => WhenPriyaSaves(connection, FieldEdit.AddsAField, choice);

        protected async Task WhenPriyaAddsFixVersionsAndSaves(string connection, Refusal refusal)
        {
            GivenTheRefreshHistorySoFarIsSetAside();
            var read = await TheConnectionAsThePageReadsIt(connection);

            switch (refusal)
            {
                case Refusal.SomeoneElseSavedFirst:
                    await SomeoneElseRenamesTheConnection(connection);
                    break;
                case Refusal.TooManyFieldsForTheLicence:
                    GivenTheInstanceRunsOnTheCommunityLicence();
                    break;
                case Refusal.AnUnfinishedWriteBackMapping:
                    MappingsOf(read).Add(AWriteBackMappingOnto(additionalFieldDefinitionId: null));
                    break;
            }

            ApplyAFieldEdit(read, FieldEdit.AddsAField);
            await Put(connection, read, choice: null);
        }

        /// <summary>
        /// A read of the connection on its own, the way the command line and the assistant read it.
        /// Reading is when Lighthouse registers the tracker's own fields.
        /// </summary>
        protected async Task WhenSomeoneLooksUpTheConnection(string connection)
        {
            GivenTheRefreshHistorySoFarIsSetAside();
            using var response = await Client.GetAsync(new Uri($"{ConnectionsRoute}/{connectionIds[connection]}", UriKind.Relative));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        protected async Task WhenTheNextScheduledUpdateOfTeamRuns(string team)
        {
            GivenTheRefreshHistorySoFarIsSetAside();
            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(teamIds[team]);
            await TheQueueGoesIdle();
        }

        protected void WhenTheTrackerAnswersAgain() => trackerMayAnswer.TrySetResult();

        // --- Then ---

        protected void ThenTheSaveIsAccepted()
            => Assert.That(SaveAnswer, Is.EqualTo(HttpStatusCode.OK), $"The save was refused: {SaveAnswerBody}");

        protected void ThenTheSaveIsRefusedBecause(Refusal refusal)
            => ThenTheSaveIsRefusedAs(refusal switch
            {
                Refusal.SomeoneElseSavedFirst => HttpStatusCode.Conflict,
                Refusal.TooManyFieldsForTheLicence => HttpStatusCode.Forbidden,
                Refusal.AnUnfinishedWriteBackMapping => HttpStatusCode.BadRequest,
                _ => throw new ArgumentOutOfRangeException(nameof(refusal), refusal, null),
            });

        protected void ThenTheSaveIsRefusedBecauseItAsksForNeitherNowNorLater()
            => ThenTheSaveIsRefusedAs(HttpStatusCode.BadRequest);

        private void ThenTheSaveIsRefusedAs(HttpStatusCode refusal)
            => Assert.That(SaveAnswer, Is.EqualTo(refusal), $"The save answered {(int)SaveAnswer}: {SaveAnswerBody}");

        /// <summary>
        /// One row per Team and Portfolio, and no other Team or Portfolio, shortly after the save. Forecast
        /// rows a finished update may add are not the save's doing and are left out.
        /// </summary>
        protected async Task ThenTheTaskManagerListsOneUpdateForEachOf(params string[] owners)
        {
            var deadline = DateTime.UtcNow + SoonAfterTheSave;
            var listed = await TheTeamsAndPortfoliosInTheTaskManager();

            while (!SameNames(listed, owners) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
                listed = await TheTeamsAndPortfoliosInTheTaskManager();
            }

            Assert.That(listed, Is.EquivalentTo(owners),
                $"The Task Manager should list one update for each Team and Portfolio the save started, within "
                + $"{SoonAfterTheSave.TotalSeconds} seconds of the save. It listed: {Describe(listed)}");
        }

        /// <summary>
        /// Each of them ran exactly once after the save, and each of those runs downloaded everything,
        /// because only a download of everything brings in a field nothing was stored for. Nothing else ran.
        /// </summary>
        protected async Task ThenEachOfThemDownloadsEverythingExactlyOnce(params string[] owners)
        {
            var history = await TheHistoryOnceEachOfThemHasRun(owners);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(history.Select(entry => entry.EntityName), Is.EquivalentTo(owners),
                    $"Exactly one update each, and no update of anything else. The refresh history after the save: {Describe(history)}");
                Assert.That(history.Where(entry => entry.Mode != SyncMode.Full).Select(entry => entry.EntityName), Is.Empty,
                    $"Every one of those updates had to download everything. The refresh history after the save: {Describe(history)}");
            }
        }

        protected async Task ThenTheTeamsWereUpdatedBeforeAnyPortfolio()
        {
            var history = await TheHistoryOnceEachOfThemHasRun(EveryTeamAndPortfolioOnNordwindJiraDc);
            var lastTeam = history.Where(entry => entry.Type == RefreshType.Team).Select(entry => entry.Id).DefaultIfEmpty(int.MaxValue).Max();
            var firstPortfolio = history.Where(entry => entry.Type == RefreshType.Portfolio).Select(entry => entry.Id).DefaultIfEmpty(int.MinValue).Min();

            Assert.That(lastTeam, Is.LessThan(firstPortfolio),
                "A Portfolio's forecast in the same round reads its Teams' data, so every Team goes first. "
                + $"The refresh history after the save: {Describe(history)}");
        }

        /// <summary>
        /// Once the queue is quiet, no Team or Portfolio has been updated since the save and none is
        /// waiting. The Task Manager is read too, so an update parked behind a held one is not missed.
        /// </summary>
        protected async Task ThenNoTeamOrPortfolioIsUpdated()
        {
            var listed = await TheTeamsAndPortfoliosInTheTaskManager();
            await TheQueueGoesIdle();
            var history = TheRefreshHistorySinceTheSave();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listed, Is.Empty, $"Nothing should be waiting in the Task Manager. It listed: {Describe(listed)}");
                Assert.That(history, Is.Empty, $"Nothing should have been updated. The refresh history after the save: {Describe(history)}");
            }
        }

        protected async Task ThenTheTeamIsNotUpdated(string team)
        {
            var listed = await TheTeamsAndPortfoliosInTheTaskManager();
            await TheQueueGoesIdle();
            var history = TheRefreshHistorySinceTheSave();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listed, Does.Not.Contain(team), $"{team} should not be waiting in the Task Manager: {Describe(listed)}");
                Assert.That(history.Select(entry => entry.EntityName), Does.Not.Contain(team),
                    $"{team} should not have been updated: {Describe(history)}");
            }
        }

        protected async Task ThenTheConnectionReads(string connection, params string[] fields)
        {
            var read = await TheConnectionAsThePageReadsIt(connection);
            var names = FieldsOf(read).Select(field => field?[DisplayName]?.GetValue<string>()).ToList();

            Assert.That(names, Is.EquivalentTo(fields), $"The connection's Additional Fields as the page reads them: {read.ToJsonString()}");
        }

        protected async Task ThenTheUpdateOfFailed(string team)
        {
            var history = await TheHistoryOnceEachOfThemHasRun([team]);
            var entries = history.Where(entry => entry.EntityName == team).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entries, Has.Count.EqualTo(1), $"One update of {team} after the save: {Describe(history)}");
                Assert.That(entries.Select(entry => entry.Success), Is.All.False, $"{team}'s update should show it failed: {Describe(history)}");
            }
        }

        protected async Task ThenEachOfThemFinishesItsUpdate(params string[] owners)
        {
            var history = await TheHistoryOnceEachOfThemHasRun(owners);
            var finished = history.Where(entry => entry.Success && owners.Contains(entry.EntityName)).Select(entry => entry.EntityName);

            Assert.That(finished, Is.EquivalentTo(owners), $"Each finished its update: {Describe(history)}");
        }

        protected async Task ThenTheUpdateOfDownloadedEverything(string team)
        {
            var history = await TheHistoryOnceEachOfThemHasRun([team]);
            var ran = history.Select(entry => $"{entry.EntityName} {entry.Mode} {(entry.Success ? "finished" : "failed")}");

            Assert.That(ran, Is.EqualTo(TheOneLine($"{team} {SyncMode.Full} finished")),
                $"{team}'s next update downloads everything: {Describe(history)}");
        }

        // --- The tracker ---

        private Mock<IWorkTrackingConnector> ATrackerThatAnswersWithOneItemEach()
        {
            var connector = new Mock<IWorkTrackingConnector>();
            connector.Setup(c => c.SupportsTransitionHistory(It.IsAny<WorkTrackingSystemConnection>())).Returns(false);
            connector.Setup(c => c.SupportsIncrementalSync(It.IsAny<WorkTrackingSystemConnection>())).Returns(true);
            connector.Setup(c => c.GetPredefinedAdditionalFields(It.IsAny<WorkTrackingSystemConnection>())).Returns([]);

            connector
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns<Team, CancellationToken>(async (team, _) => await TheWorkItemOf(team));
            connector
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .Returns<Team, IReadOnlyCollection<string>, CancellationToken>(async (team, wanted, _) => [.. (await TheWorkItemOf(team)).Where(item => wanted.Contains(item.ReferenceId))]);
            connector
                .Setup(c => c.SweepWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns<Team, CancellationToken>(async (team, _) => await StampsFor(team));

            connector
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .Returns<Portfolio, CancellationToken>(async (portfolio, _) => await TheFeatureOf(portfolio));
            connector
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .Returns<Portfolio, IReadOnlyCollection<string>, CancellationToken>(async (portfolio, wanted, _) => [.. (await TheFeatureOf(portfolio)).Where(feature => wanted.Contains(feature.ReferenceId))]);
            connector
                .Setup(c => c.SweepFeaturesForPortfolio(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .Returns<Portfolio, CancellationToken>(async (portfolio, _) => await StampsFor(portfolio));
            connector
                .Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns<Portfolio, IEnumerable<string>, CancellationToken>(async (_, _, _) => await NoParentFeatures());
            connector
                .Setup(c => c.SweepParentFeatures(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns<Portfolio, IEnumerable<string>, CancellationToken>(async (_, _, _) => await NoParentStamps());
            connector
                .Setup(c => c.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ReturnsAsync(new WriteBackResult());

            return connector;
        }

        // Every Team and every Portfolio holds one item that never moves on the tracker. Something stored
        // with a change stamp is what lets an update with nothing new to ask for fetch only what changed,
        // so a full download is something an update has to have a reason for.
        private static readonly DateTime TheItemLastChangedAt = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        private static string TheItemOf(string owner) => $"{owner}-1";

        private async Task<List<WorkItem>> TheWorkItemOf(Team team)
        {
            await trackerMayAnswer.Task;
            RefuseIfTold(team);
            return
            [
                new WorkItem(new WorkItemBase
                {
                    ReferenceId = TheItemOf(team.Name),
                    Name = TheItemOf(team.Name),
                    Type = "Story",
                    State = "In Progress",
                    StateCategory = StateCategories.Doing,
                    Order = "1",
                }, team)
                {
                    LastChangedRemote = TheItemLastChangedAt,
                },
            ];
        }

        private async Task<List<RemoteRecordStamp>> StampsFor(Team team)
        {
            await trackerMayAnswer.Task;
            RefuseIfTold(team);
            return [new RemoteRecordStamp(TheItemOf(team.Name), TheItemLastChangedAt)];
        }

        private async Task<List<Feature>> TheFeatureOf(Portfolio portfolio)
        {
            await trackerMayAnswer.Task;
            return
            [
                new Feature(new WorkItemBase
                {
                    ReferenceId = TheItemOf(portfolio.Name),
                    Name = TheItemOf(portfolio.Name),
                    Type = "Epic",
                    State = "In Progress",
                    StateCategory = StateCategories.Doing,
                    Order = "1",
                })
                {
                    LastChangedRemote = TheItemLastChangedAt,
                },
            ];
        }

        private async Task<List<RemoteRecordStamp>> StampsFor(Portfolio portfolio)
        {
            await trackerMayAnswer.Task;
            return [new RemoteRecordStamp(TheItemOf(portfolio.Name), TheItemLastChangedAt)];
        }

        private async Task<List<Feature>> NoParentFeatures()
        {
            await trackerMayAnswer.Task;
            return [];
        }

        private async Task<List<RemoteRecordStamp>> NoParentStamps()
        {
            await trackerMayAnswer.Task;
            return [];
        }

        private void RefuseIfTold(Team team)
        {
            if (teamsTheTrackerRefuses.Contains(team.Name))
            {
                throw new HttpRequestException("The project refuses this account.", null, HttpStatusCode.Forbidden);
            }
        }

        // --- Seeding (preconditions only, never the expected outcome) ---

        private async Task SeedConnection(string name, WorkTrackingSystems system, params (string DisplayName, string Reference)[] fields)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = name,
                WorkTrackingSystem = system,
                AuthenticationMethodKey = system == WorkTrackingSystems.Jira ? AuthenticationMethodKeys.JiraCloud : AuthenticationMethodKeys.AzureDevOpsPat,
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = JiraAddress, Value = "https://jira.nordwind.example", IsSecret = false });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = JiraUsername, Value = "priya@nordwind.example", IsSecret = false });
            connection.AdditionalFieldDefinitions.AddRange(
                fields.Select(field => new AdditionalFieldDefinition { DisplayName = field.DisplayName, Reference = field.Reference }));

            repository.Add(connection);
            await repository.Save();
            connectionIds[name] = connection.Id;
        }

        private async Task SeedTeam(string name, string connection)
        {
            using var scope = Factory.Services.CreateScope();
            var services = scope.ServiceProvider;

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = services.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionIds[connection])!,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = $"project = {name.Replace(' ', '_')}",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                ThroughputHistory = 30,
            };

            var repository = services.GetRequiredService<IRepository<Team>>();
            repository.Add(team);
            await repository.Save();
            teamIds[name] = team.Id;
        }

        private async Task SeedPortfolio(string name, string connection)
        {
            using var scope = Factory.Services.CreateScope();
            var services = scope.ServiceProvider;

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = services.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionIds[connection])!,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = $"project = {name.Replace(' ', '_')}",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            var repository = services.GetRequiredService<IRepository<Portfolio>>();
            repository.Add(portfolio);
            await repository.Save();
            portfolioIds[name] = portfolio.Id;
        }

        // --- The save, as the page sends it ---

        private async Task SaveTheConnection(string connection, Action<JsonObject> edit, string? choice)
        {
            GivenTheRefreshHistorySoFarIsSetAside();
            var read = await TheConnectionAsThePageReadsIt(connection);
            edit(read);
            await Put(connection, read, choice);
        }

        protected async Task SaveKeepingTheHistoryAlreadySetAside(string connection, FieldEdit edit)
        {
            var read = await TheConnectionAsThePageReadsIt(connection);
            ApplyAFieldEdit(read, edit);
            await Put(connection, read, choice: null);
        }

        private async Task Put(string connection, JsonObject body, string? choice)
        {
            var route = choice is null
                ? $"{ConnectionsRoute}/{connectionIds[connection]}"
                : $"{ConnectionsRoute}/{connectionIds[connection]}?startUpdates={choice}";

            using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, JsonMediaType);
            using var response = await Client.PutAsync(new Uri(route, UriKind.Relative), content);

            SaveAnswer = response.StatusCode;
            SaveAnswerBody = await response.Content.ReadAsStringAsync();
        }

        private async Task SomeoneElseRenamesTheConnection(string connection)
        {
            var theirs = await TheConnectionAsThePageReadsIt(connection);
            theirs["name"] = $"{connection} (renamed)";
            await Put(connection, theirs, choice: null);

            Assert.That(SaveAnswer, Is.EqualTo(HttpStatusCode.OK), $"The other admin's save is the one that lands first: {SaveAnswerBody}");
        }

        /// <summary>
        /// The list the connection page reads its connection from.
        /// </summary>
        protected async Task<JsonObject> TheConnectionAsThePageReadsIt(string connection)
        {
            using var response = await Client.GetAsync(new Uri(ConnectionsRoute, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var read = JsonNode.Parse(body)!.AsArray()
                .OfType<JsonObject>()
                .FirstOrDefault(candidate => candidate["id"]?.GetValue<int>() == connectionIds[connection]);

            Assert.That(read, Is.Not.Null, $"{connection} is not among the connections: {body}");
            return read!;
        }

        private static void ApplyAFieldEdit(JsonObject read, FieldEdit edit)
        {
            var fields = FieldsOf(read);

            switch (edit)
            {
                case FieldEdit.AddsAField:
                    fields.Add(new JsonObject { ["id"] = -1, [DisplayName] = FixVersions, [Reference] = FixVersionsReference, ["isPredefined"] = false });
                    break;
                case FieldEdit.RenamesAFieldsLabel:
                    TheField(fields, StoryPoints)[DisplayName] = "Estimate";
                    break;
                case FieldEdit.ChangesAFieldsReference:
                    TheField(fields, StoryPoints)[Reference] = "customfield_10028";
                    break;
                case FieldEdit.RemovesAField:
                    fields.Remove(TheField(fields, StoryPoints));
                    break;
            }
        }

        private static void ApplyAnEditThatChangesNothingFetched(JsonObject read, EditThatChangesNothingFetched edit)
        {
            switch (edit)
            {
                case EditThatChangesNothingFetched.NewApiToken:
                    OptionsOf(read).Add(new JsonObject { ["key"] = JiraWorkTrackingOptionNames.ApiToken, ["value"] = "a-new-token", ["isSecret"] = true, ["isOptional"] = false });
                    break;
                case EditThatChangesNothingFetched.NewConnectionName:
                    read["name"] = "Nordwind Jira Data Center";
                    break;
                case EditThatChangesNothingFetched.NewJiraAddress:
                    TheOption(OptionsOf(read), JiraAddress)["value"] = "https://jira2.nordwind.example";
                    break;
                case EditThatChangesNothingFetched.NewSignInMethod:
                    read["authenticationMethodKey"] = AuthenticationMethodKeys.JiraDataCenter;
                    break;
                case EditThatChangesNothingFetched.NewWriteBackMapping:
                    MappingsOf(read).Add(AWriteBackMappingOnto(TheField(FieldsOf(read), StoryPoints)["id"]!.GetValue<int>()));
                    break;
                case EditThatChangesNothingFetched.SameFieldsInAnotherOrder:
                    var asRead = NamesOf(FieldsOf(read));
                    var reversed = FieldsOf(read).Select(field => field!.DeepClone()).Reverse().ToList();
                    read[AdditionalFieldDefinitions] = new JsonArray([.. reversed]);
                    Assert.That(NamesOf(FieldsOf(read)), Is.Not.EqualTo(asRead),
                        "The connection needs two fields or more, or putting them in another order changes nothing");
                    break;
            }
        }

        /// <summary>
        /// A new write-back mapping as the page adds it. Without a field to write into it is unfinished.
        /// </summary>
        private static JsonObject AWriteBackMappingOnto(int? additionalFieldDefinitionId) => new()
        {
            ["id"] = 0,
            ["valueSource"] = nameof(WriteBackValueSource.WorkItemAgeCycleTime),
            ["appliesTo"] = nameof(WriteBackAppliesTo.Team),
            ["additionalFieldDefinitionId"] = additionalFieldDefinitionId,
        };

        private static JsonArray FieldsOf(JsonObject read) => read[AdditionalFieldDefinitions]!.AsArray();

        private static List<string?> NamesOf(JsonArray fields) => [.. fields.Select(field => field?[DisplayName]?.GetValue<string>())];

        private static JsonArray OptionsOf(JsonObject read) => read["options"]!.AsArray();

        private static JsonArray MappingsOf(JsonObject read) => read["writeBackMappingDefinitions"]!.AsArray();

        private static JsonObject TheField(JsonArray fields, string displayName)
            => fields.OfType<JsonObject>().First(field => field[DisplayName]?.GetValue<string>() == displayName);

        private static JsonObject TheOption(JsonArray options, string key)
            => options.OfType<JsonObject>().First(option => option["key"]?.GetValue<string>() == key);

        // --- Reading the Task Manager and the refresh history ---

        private async Task<List<string>> TheTeamsAndPortfoliosInTheTaskManager()
        {
            using var response = await Client.GetAsync(new Uri(TaskListRoute, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var rows = JsonDocument.Parse(body);
            return [.. rows.RootElement.EnumerateArray()
                .Where(row => row.GetProperty("updateType").GetString() is nameof(UpdateType.Team) or nameof(UpdateType.Features))
                .Select(row => row.GetProperty("name").GetString() ?? "(a row with no name)")];
        }

        private List<RefreshLog> TheRefreshHistory()
        {
            using var scope = Factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetRequiredService<IRefreshLogService>()
                .GetRefreshLogs()
                .Where(entry => entry.Type is RefreshType.Team or RefreshType.Portfolio)
                .OrderBy(entry => entry.Id)];
        }

        private List<RefreshLog> TheRefreshHistorySinceTheSave()
            => [.. TheRefreshHistory().Where(entry => entry.Id > lastHistoryEntryBeforeTheSave)];

        protected async Task<List<RefreshLog>> TheHistoryOnceEachOfThemHasRun(string[] owners)
        {
            var deadline = DateTime.UtcNow + LongEnoughForAnyUpdateToFinish;
            await TheQueueGoesIdle();
            var history = TheRefreshHistorySinceTheSave();

            while (!owners.All(owner => history.Exists(entry => entry.EntityName == owner)) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
                await TheQueueGoesIdle();
                history = TheRefreshHistorySinceTheSave();
            }

            return history;
        }

        private async Task TheUpdateOfIs(UpdateKey key, UpdateProgress progress)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow + LongEnoughForAnyUpdateToFinish;

            while (DateTime.UtcNow < deadline)
            {
                if (store.TryGet(key, out var status) && status?.Status == progress)
                {
                    return;
                }

                await Task.Delay(20);
            }

            Assert.Fail($"The update of {key.UpdateType} {key.Id} never became {progress}; the scenario cannot go on.");
        }

        protected async Task TheQueueGoesIdle()
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow + LongEnoughForAnyUpdateToFinish;

            while (store.HasActiveWork() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }
        }

        private static bool SameNames(List<string> listed, string[] expected)
            => listed.Count == expected.Length && listed.Order(StringComparer.Ordinal).SequenceEqual(expected.Order(StringComparer.Ordinal));

        private static List<string> TheOneLine(string line) => [line];

        private static string Describe(List<string> names) => names.Count == 0 ? "(nothing)" : string.Join(", ", names);

        protected static string Describe(List<RefreshLog> history)
            => history.Count == 0
                ? "(nothing)"
                : string.Join(", ", history.Select(entry => $"{entry.EntityName} {entry.Mode}{(entry.Success ? string.Empty : " failed")}"));
    }
}
